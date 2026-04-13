#import "VESVisionBridgeExports.h"

#import <AVFoundation/AVFoundation.h>
#import <Foundation/Foundation.h>
#import <Vision/Vision.h>

#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <string>
#include <vector>

namespace
{
constexpr float kMinimumPointConfidence = 0.15f;
constexpr float kPinchDistanceThreshold = 0.075f;
constexpr float kFingerExtendedMargin = 0.03f;
constexpr int kDefaultUserId = 1001;

char* CopyCString(const std::string& value)
{
    auto* buffer = static_cast<char*>(std::malloc(value.size() + 1));
    if (buffer == nullptr)
    {
        return nullptr;
    }

    std::memcpy(buffer, value.c_str(), value.size() + 1);
    return buffer;
}

std::string ToStdString(NSString* value)
{
    if (value == nil)
    {
        return std::string();
    }

    const char* utf8 = value.UTF8String;
    return utf8 != nullptr ? std::string(utf8) : std::string();
}

NSString* EscapeJson(NSString* value)
{
    if (value == nil)
    {
        return @"";
    }

    NSMutableString* escaped = [value mutableCopy];
    [escaped replaceOccurrencesOfString:@"\\" withString:@"\\\\" options:0 range:NSMakeRange(0, escaped.length)];
    [escaped replaceOccurrencesOfString:@"\"" withString:@"\\\"" options:0 range:NSMakeRange(0, escaped.length)];
    return escaped;
}

float Distance(CGPoint a, CGPoint b)
{
    const auto dx = static_cast<float>(a.x - b.x);
    const auto dy = static_cast<float>(a.y - b.y);
    return std::sqrt(dx * dx + dy * dy);
}

struct NativeHandResult
{
    bool tracked = false;
    float confidence = 0.0f;
    CGPoint viewportPosition = CGPointZero;
    CGRect bounds = CGRectZero;
    std::string gesture;
};

struct NativeUserResult
{
    bool tracked = false;
    int userId = 0;
    float confidence = 0.0f;
    CGRect viewportBounds = CGRectZero;
};

NSString* BuildFrameJson(const NativeUserResult& user, const NativeHandResult& leftHand, const NativeHandResult& rightHand, double timestamp)
{
    return [NSString stringWithFormat:
        @"{\"hasUser\":%@,\"userId\":%d,\"userConfidence\":%.4f,\"userViewportX\":%.4f,\"userViewportY\":%.4f,\"userViewportWidth\":%.4f,\"userViewportHeight\":%.4f,"
         "\"leftTracked\":%@,\"leftConfidence\":%.4f,\"leftViewportX\":%.4f,\"leftViewportY\":%.4f,\"leftGesture\":\"%@\","
         "\"rightTracked\":%@,\"rightConfidence\":%.4f,\"rightViewportX\":%.4f,\"rightViewportY\":%.4f,\"rightGesture\":\"%@\","
         "\"timestamp\":%.6f}",
        user.tracked ? @"true" : @"false",
        user.userId,
        user.confidence,
        user.viewportBounds.origin.x,
        user.viewportBounds.origin.y,
        user.viewportBounds.size.width,
        user.viewportBounds.size.height,
        leftHand.tracked ? @"true" : @"false",
        leftHand.confidence,
        leftHand.viewportPosition.x,
        leftHand.viewportPosition.y,
        EscapeJson([NSString stringWithUTF8String:leftHand.gesture.c_str()]),
        rightHand.tracked ? @"true" : @"false",
        rightHand.confidence,
        rightHand.viewportPosition.x,
        rightHand.viewportPosition.y,
        EscapeJson([NSString stringWithUTF8String:rightHand.gesture.c_str()]),
        timestamp];
}

bool TryGetPoint(VNHumanHandPoseObservation* observation, VNHumanHandPoseObservationJointName jointName, CGPoint& location, float& confidence)
{
    NSError* error = nil;
    VNRecognizedPoint* point = [observation recognizedPointForJointName:jointName error:&error];
    if (error != nil || point == nil || point.confidence < kMinimumPointConfidence)
    {
        return false;
    }

    location = point.location;
    confidence = point.confidence;
    return true;
}

bool IsFingerExtended(CGPoint wrist, CGPoint tip, CGPoint pip)
{
    return Distance(wrist, tip) > Distance(wrist, pip) + kFingerExtendedMargin;
}

CGRect EstimateHandBounds(VNHumanHandPoseObservation* observation, CGPoint wrist)
{
    static NSArray<VNHumanHandPoseObservationJointName>* joints = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        joints = @[
            VNHumanHandPoseObservationJointNameWrist,
            VNHumanHandPoseObservationJointNameThumbTip,
            VNHumanHandPoseObservationJointNameThumbIP,
            VNHumanHandPoseObservationJointNameIndexTip,
            VNHumanHandPoseObservationJointNameIndexPIP,
            VNHumanHandPoseObservationJointNameMiddleTip,
            VNHumanHandPoseObservationJointNameMiddlePIP,
            VNHumanHandPoseObservationJointNameRingTip,
            VNHumanHandPoseObservationJointNameRingPIP,
            VNHumanHandPoseObservationJointNameLittleTip,
            VNHumanHandPoseObservationJointNameLittlePIP
        ];
    });

    CGFloat minX = wrist.x;
    CGFloat minY = wrist.y;
    CGFloat maxX = wrist.x;
    CGFloat maxY = wrist.y;

    for (VNHumanHandPoseObservationJointName jointName in joints)
    {
        CGPoint point = CGPointZero;
        float confidence = 0.0f;
        if (!TryGetPoint(observation, jointName, point, confidence))
        {
            continue;
        }

        minX = std::min<CGFloat>(minX, point.x);
        minY = std::min<CGFloat>(minY, point.y);
        maxX = std::max<CGFloat>(maxX, point.x);
        maxY = std::max<CGFloat>(maxY, point.y);
    }

    const CGFloat padding = 0.04;
    minX = std::max<CGFloat>(0.0, minX - padding);
    minY = std::max<CGFloat>(0.0, minY - padding);
    maxX = std::min<CGFloat>(1.0, maxX + padding);
    maxY = std::min<CGFloat>(1.0, maxY + padding);
    return CGRectMake(minX, minY, std::max<CGFloat>(0.0, maxX - minX), std::max<CGFloat>(0.0, maxY - minY));
}

std::string ClassifyGesture(VNHumanHandPoseObservation* observation, CGPoint wrist)
{
    CGPoint thumbTip = CGPointZero;
    CGPoint thumbIp = CGPointZero;
    CGPoint indexTip = CGPointZero;
    CGPoint indexPip = CGPointZero;
    CGPoint middleTip = CGPointZero;
    CGPoint middlePip = CGPointZero;
    CGPoint ringTip = CGPointZero;
    CGPoint ringPip = CGPointZero;
    CGPoint littleTip = CGPointZero;
    CGPoint littlePip = CGPointZero;
    float unusedConfidence = 0.0f;

    const bool hasThumbTip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameThumbTip, thumbTip, unusedConfidence);
    const bool hasThumbIp = TryGetPoint(observation, VNHumanHandPoseObservationJointNameThumbIP, thumbIp, unusedConfidence);
    const bool hasIndexTip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameIndexTip, indexTip, unusedConfidence);
    const bool hasIndexPip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameIndexPIP, indexPip, unusedConfidence);
    const bool hasMiddleTip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameMiddleTip, middleTip, unusedConfidence);
    const bool hasMiddlePip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameMiddlePIP, middlePip, unusedConfidence);
    const bool hasRingTip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameRingTip, ringTip, unusedConfidence);
    const bool hasRingPip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameRingPIP, ringPip, unusedConfidence);
    const bool hasLittleTip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameLittleTip, littleTip, unusedConfidence);
    const bool hasLittlePip = TryGetPoint(observation, VNHumanHandPoseObservationJointNameLittlePIP, littlePip, unusedConfidence);

    if (hasThumbTip && hasIndexTip && Distance(thumbTip, indexTip) <= kPinchDistanceThreshold)
    {
        return "pinch";
    }

    const bool indexExtended = hasIndexTip && hasIndexPip && IsFingerExtended(wrist, indexTip, indexPip);
    const bool middleExtended = hasMiddleTip && hasMiddlePip && IsFingerExtended(wrist, middleTip, middlePip);
    const bool ringExtended = hasRingTip && hasRingPip && IsFingerExtended(wrist, ringTip, ringPip);
    const bool littleExtended = hasLittleTip && hasLittlePip && IsFingerExtended(wrist, littleTip, littlePip);
    const bool thumbExtended = hasThumbTip && hasThumbIp && Distance(wrist, thumbTip) > Distance(wrist, thumbIp) + kFingerExtendedMargin;

    if (indexExtended && !middleExtended && !ringExtended && !littleExtended)
    {
        return "point";
    }

    if (thumbExtended && !indexExtended && !middleExtended && !ringExtended && !littleExtended)
    {
        return "thumbsup";
    }

    if (thumbExtended && indexExtended && middleExtended && ringExtended && littleExtended)
    {
        return "openpalm";
    }

    if (!thumbExtended && !indexExtended && !middleExtended && !ringExtended && !littleExtended)
    {
        return "fist";
    }

    return "";
}
} // namespace

@interface VESVisionBridgeService : NSObject <AVCaptureVideoDataOutputSampleBufferDelegate>
+ (instancetype)sharedService;
- (BOOL)ensureStarted;
- (void)stop;
- (BOOL)isOperational;
- (NSString*)statusMessage;
- (NSString*)latestFrameJson;
@end

@implementation VESVisionBridgeService
{
    AVCaptureSession* _session;
    dispatch_queue_t _captureQueue;
    std::mutex _stateMutex;
    NSString* _statusMessage;
    NSString* _latestFrameJson;
    BOOL _operational;
    BOOL _startAttempted;
}

+ (instancetype)sharedService
{
    static VESVisionBridgeService* service = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        service = [[VESVisionBridgeService alloc] init];
    });
    return service;
}

- (instancetype)init
{
    self = [super init];
    if (self != nil)
    {
        _captureQueue = dispatch_queue_create("com.visualeducationsystem.vesvisionbridge.capture", DISPATCH_QUEUE_SERIAL);
        _statusMessage = @"macOS Vision bridge initialized. Camera session not started yet.";
        _latestFrameJson = BuildFrameJson(NativeUserResult{}, NativeHandResult{}, NativeHandResult{}, 0.0);
        _operational = NO;
        _startAttempted = NO;
    }
    return self;
}

- (BOOL)ensureStarted
{
    std::scoped_lock lock(_stateMutex);
    if (_operational)
    {
        return YES;
    }

    if (_startAttempted)
    {
        return _operational;
    }

    _startAttempted = YES;

    if (@available(macOS 10.15, *))
    {
        AVAuthorizationStatus authorizationStatus = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
        if (authorizationStatus == AVAuthorizationStatusDenied || authorizationStatus == AVAuthorizationStatusRestricted)
        {
            _statusMessage = @"Camera access denied or restricted for VESVisionBridge.";
            _operational = NO;
            return NO;
        }

        if (authorizationStatus == AVAuthorizationStatusNotDetermined)
        {
            _statusMessage = @"Camera access not determined yet. Approve camera access and reload the plugin.";
            [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
                std::scoped_lock asyncLock(self->_stateMutex);
                self->_statusMessage = granted
                    ? @"Camera access granted. Reload the plugin or restart play mode to begin native recognition."
                    : @"Camera access denied for VESVisionBridge.";
            }];
            _operational = NO;
            return NO;
        }
    }

    AVCaptureDevice* device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    if (device == nil)
    {
        _statusMessage = @"No video capture device available for VESVisionBridge.";
        _operational = NO;
        return NO;
    }

    NSError* inputError = nil;
    AVCaptureDeviceInput* input = [AVCaptureDeviceInput deviceInputWithDevice:device error:&inputError];
    if (input == nil || inputError != nil)
    {
        _statusMessage = [NSString stringWithFormat:@"Failed to create capture input: %@", inputError.localizedDescription ?: @"unknown error"];
        _operational = NO;
        return NO;
    }

    AVCaptureVideoDataOutput* output = [[AVCaptureVideoDataOutput alloc] init];
    output.alwaysDiscardsLateVideoFrames = YES;
    output.videoSettings = @{
        (__bridge NSString*)kCVPixelBufferPixelFormatTypeKey : @(kCVPixelFormatType_32BGRA)
    };
    [output setSampleBufferDelegate:self queue:_captureQueue];

    AVCaptureSession* session = [[AVCaptureSession alloc] init];
    if ([session canSetSessionPreset:AVCaptureSessionPreset640x480])
    {
        session.sessionPreset = AVCaptureSessionPreset640x480;
    }

    if (![session canAddInput:input] || ![session canAddOutput:output])
    {
        _statusMessage = @"Failed to attach capture input/output for VESVisionBridge.";
        _operational = NO;
        return NO;
    }

    [session addInput:input];
    [session addOutput:output];
    [session startRunning];

    _session = session;
    _operational = session.isRunning;
    _statusMessage = _operational
        ? @"VESVisionBridge native capture started. Vision hand recognition is active."
        : @"VESVisionBridge failed to start capture session.";
    return _operational;
}

- (BOOL)isOperational
{
    std::scoped_lock lock(_stateMutex);
    return _operational;
}

- (void)stop
{
    std::scoped_lock lock(_stateMutex);
    if (_session != nil)
    {
        [_session stopRunning];
        _session = nil;
    }

    _operational = NO;
    _startAttempted = NO;
    _statusMessage = @"VESVisionBridge native capture stopped.";
}

- (NSString*)statusMessage
{
    std::scoped_lock lock(_stateMutex);
    return _statusMessage;
}

- (NSString*)latestFrameJson
{
    std::scoped_lock lock(_stateMutex);
    return _latestFrameJson;
}

- (void)captureOutput:(AVCaptureOutput*)output didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer fromConnection:(AVCaptureConnection*)connection
{
    CVPixelBufferRef pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer);
    if (pixelBuffer == nil)
    {
        std::scoped_lock lock(_stateMutex);
        _statusMessage = @"VESVisionBridge received an empty pixel buffer.";
        return;
    }

    NSError* handError = nil;
    VNDetectHumanHandPoseRequest* handRequest = [[VNDetectHumanHandPoseRequest alloc] init];
    handRequest.maximumHandCount = 2;

    VNImageRequestHandler* handler = [[VNImageRequestHandler alloc] initWithCVPixelBuffer:pixelBuffer options:@{}];
    const BOOL handSuccess = [handler performRequests:@[handRequest] error:&handError];
    if (!handSuccess || handError != nil)
    {
        std::scoped_lock lock(_stateMutex);
        _statusMessage = [NSString stringWithFormat:@"Vision hand pose request failed: %@", handError.localizedDescription ?: @"unknown error"];
        return;
    }

    NSMutableArray<VNHumanHandPoseObservation*>* observations = [NSMutableArray arrayWithArray:handRequest.results ?: @[]];
    std::vector<NativeHandResult> detectedHands;
    detectedHands.reserve(observations.count);

    for (VNHumanHandPoseObservation* observation in observations)
    {
        CGPoint wrist = CGPointZero;
        float wristConfidence = 0.0f;
        if (!TryGetPoint(observation, VNHumanHandPoseObservationJointNameWrist, wrist, wristConfidence))
        {
            continue;
        }

        NativeHandResult hand;
        hand.tracked = true;
        hand.confidence = wristConfidence;
        hand.viewportPosition = wrist;

        hand.bounds = EstimateHandBounds(observation, wrist);
        hand.gesture = ClassifyGesture(observation, wrist);
        detectedHands.push_back(hand);
    }

    NativeHandResult leftHand;
    NativeHandResult rightHand;
    if (!detectedHands.empty())
    {
        std::sort(detectedHands.begin(), detectedHands.end(), [](const NativeHandResult& a, const NativeHandResult& b) {
            return a.viewportPosition.x < b.viewportPosition.x;
        });

        if (detectedHands.size() == 1)
        {
            if (detectedHands[0].viewportPosition.x < 0.5f)
            {
                rightHand = detectedHands[0];
            }
            else
            {
                leftHand = detectedHands[0];
            }
        }
        else
        {
            rightHand = detectedHands.front();
            leftHand = detectedHands.back();
        }
    }

    NativeUserResult user;
    if (leftHand.tracked || rightHand.tracked)
    {
        user.tracked = true;
        user.userId = kDefaultUserId;
        user.confidence = std::max(leftHand.confidence, rightHand.confidence);

        CGRect unionRect = CGRectZero;
        bool hasRect = false;
        if (leftHand.tracked)
        {
            unionRect = leftHand.bounds;
            hasRect = true;
        }

        if (rightHand.tracked)
        {
            unionRect = hasRect ? CGRectUnion(unionRect, rightHand.bounds) : rightHand.bounds;
            hasRect = true;
        }

        if (hasRect)
        {
            const CGFloat expandX = 0.18;
            const CGFloat expandY = 0.22;
            CGFloat x = std::max<CGFloat>(0.0, unionRect.origin.x - expandX);
            CGFloat y = std::max<CGFloat>(0.0, unionRect.origin.y - expandY);
            CGFloat maxX = std::min<CGFloat>(1.0, CGRectGetMaxX(unionRect) + expandX);
            CGFloat maxY = std::min<CGFloat>(1.0, CGRectGetMaxY(unionRect) + expandY);
            user.viewportBounds = CGRectMake(x, y, maxX - x, maxY - y);
        }
    }

    const double timestamp = CMTimeGetSeconds(CMSampleBufferGetPresentationTimeStamp(sampleBuffer));
    NSString* frameJson = BuildFrameJson(user, leftHand, rightHand, std::isfinite(timestamp) ? timestamp : [NSDate timeIntervalSinceReferenceDate]);

    std::scoped_lock lock(_stateMutex);
    _latestFrameJson = frameJson;
    _statusMessage = user.tracked
        ? [NSString stringWithFormat:@"VESVisionBridge active. user=%d left=%s right=%s",
            user.userId,
            leftHand.gesture.empty() ? "tracked" : leftHand.gesture.c_str(),
            rightHand.gesture.empty() ? "tracked" : rightHand.gesture.c_str()]
        : @"VESVisionBridge active. No hands detected in the current frame.";
}

@end

int VESVision_IsBackendAvailable(void)
{
    VESVisionBridgeService* service = [VESVisionBridgeService sharedService];
    return [service ensureStarted] ? 1 : 0;
}

const char* VESVision_CopyStatusMessage(void)
{
    NSString* status = [[VESVisionBridgeService sharedService] statusMessage];
    return CopyCString(ToStdString(status));
}

const char* VESVision_CopyLatestFrameJson(void)
{
    VESVisionBridgeService* service = [VESVisionBridgeService sharedService];
    [service ensureStarted];
    NSString* json = [service latestFrameJson];
    return CopyCString(ToStdString(json));
}

void VESVision_StopBackend(void)
{
    [[VESVisionBridgeService sharedService] stop];
}

void VESVision_FreeCopiedString(void* pointer)
{
    if (pointer != nullptr)
    {
        std::free(pointer);
    }
}
