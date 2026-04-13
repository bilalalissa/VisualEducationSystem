#nullable enable
using UnityEngine;
using System.Collections.Generic;

namespace VisualEducationSystem.Interaction
{
    public sealed class WebcamHandTrackingProvider : MonoBehaviour, IHandTrackingProvider
    {
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFps = 30;
        [SerializeField] private bool mirrorHandRoles = false;
        [SerializeField] private float minimumUserConfidence = 0.2f;
        [SerializeField] private float minimumHandConfidence = 0.2f;
        [SerializeField] private float positionSmoothing = 0.35f;
        [SerializeField] private float userLossGraceSeconds = 0.4f;
        [SerializeField] private float handLossGraceSeconds = 0.5f;
        [SerializeField] private int gestureStabilityFrames = 3;
        private static readonly HashSet<WebcamHandTrackingProvider> ActiveProviders = new();

        private WebCamTexture? webcamTexture;
        private IWebcamRecognitionBackend? recognitionBackend;
        private string statusSummary = "Webcam not started.";
        private string providerDisplayName = "Webcam";
        private string backendStatus = "Recognition backend not initialized.";
        private const int WebcamCandidateUserId = 1001;
        private SmoothedRecognitionState smoothedState;

        public string ProviderDisplayName => providerDisplayName;
        public string StatusSummary => $"{statusSummary} {backendStatus} Roles={(mirrorHandRoles ? "mirrored" : "native")}, smoothing on.";
        public bool IsAvailable => webcamTexture != null && webcamTexture.isPlaying;
        public Texture? DebugTexture => webcamTexture;
        public bool HasOperationalRecognition => recognitionBackend?.IsOperational == true;

        private void Awake()
        {
            recognitionBackend = CreateRecognitionBackend();
            if (recognitionBackend is MacOSVisionRecognitionBackend)
            {
                // Native bridge now emits user-facing left/right hand roles directly.
                mirrorHandRoles = false;
            }
            backendStatus = recognitionBackend.StatusSummary;
            ActiveProviders.Add(this);
        }

        private void OnEnable()
        {
            if (autoStart)
            {
                StartCamera();
            }
        }

        private void OnDisable()
        {
            StopCamera();
        }

        private void OnDestroy()
        {
            StopCamera();
            ActiveProviders.Remove(this);
        }

        private void OnApplicationQuit()
        {
            StopCamera();
        }

        private void Update()
        {
            if (webcamTexture == null || !webcamTexture.isPlaying)
            {
                return;
            }

            statusSummary = $"{providerDisplayName} active at {webcamTexture.width}x{webcamTexture.height}.";
            backendStatus = recognitionBackend?.StatusSummary ?? "Recognition backend unavailable.";
        }

        public void ShutdownProvider()
        {
            StopCamera();
        }

        public static void ShutdownAllProviders()
        {
            foreach (var provider in ActiveProviders)
            {
                if (provider != null)
                {
                    provider.StopCamera();
                }
            }

            MacOSVisionNativeBridge.TryStopBackend();
        }

        public bool TryGetFrame(out HandTrackingFrame frame)
        {
            if (webcamTexture == null || !webcamTexture.isPlaying)
            {
                frame = default;
                return false;
            }

            if (recognitionBackend != null && recognitionBackend.IsOperational && recognitionBackend.TryRecognizeFrame(webcamTexture, out var recognizedFrame))
            {
                var leftHandSample = recognizedFrame.LeftHand;
                var rightHandSample = recognizedFrame.RightHand;
                if (mirrorHandRoles)
                {
                    (leftHandSample, rightHandSample) = (rightHandSample, leftHandSample);
                }

                frame = BuildSmoothedFrame(
                    recognizedFrame.Timestamp,
                    recognizedFrame.User,
                    leftHandSample,
                    rightHandSample);
                return true;
            }

            var leftHand = new HandSample(false, 0f, new Vector2(0.3f, 0.55f), HandGestureType.None);
            var rightHand = new HandSample(false, 0f, new Vector2(0.7f, 0.55f), HandGestureType.None);
            frame = new HandTrackingFrame(
                true,
                WebcamCandidateUserId,
                0.35f,
                new Rect(0.2f, 0.08f, 0.6f, 0.84f),
                leftHand,
                rightHand,
                Time.unscaledTimeAsDouble);
            return true;
        }

        private void StartCamera()
        {
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                return;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                statusSummary = "No webcam devices detected.";
                return;
            }

            var selectedDevice = devices[0];
            providerDisplayName = $"Webcam ({selectedDevice.name})";
            webcamTexture = new WebCamTexture(selectedDevice.name, requestedWidth, requestedHeight, requestedFps);
            webcamTexture.Play();
            statusSummary = $"{providerDisplayName} starting...";
        }

        private void StopCamera()
        {
            if (webcamTexture == null)
            {
                MacOSVisionNativeBridge.TryStopBackend();
                return;
            }

            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }

            Destroy(webcamTexture);
            webcamTexture = null;
            MacOSVisionNativeBridge.TryStopBackend();
            statusSummary = "Webcam stopped.";
        }

        private static IWebcamRecognitionBackend CreateRecognitionBackend()
        {
            return new MacOSVisionRecognitionBackend();
        }

        private HandTrackingFrame BuildSmoothedFrame(
            double timestamp,
            UserRecognitionSample userSample,
            HandRecognitionSample leftHandSample,
            HandRecognitionSample rightHandSample)
        {
            UpdateUserState(timestamp, userSample);
            var leftHand = UpdateHandState(ref smoothedState.LeftHand, timestamp, leftHandSample);
            var rightHand = UpdateHandState(ref smoothedState.RightHand, timestamp, rightHandSample);

            return new HandTrackingFrame(
                smoothedState.UserTracked,
                smoothedState.UserId,
                smoothedState.UserConfidence,
                smoothedState.UserViewportBounds,
                leftHand,
                rightHand,
                timestamp);
        }

        private void UpdateUserState(double timestamp, UserRecognitionSample userSample)
        {
            if (userSample.IsTracked && userSample.Confidence >= minimumUserConfidence)
            {
                if (!smoothedState.UserTracked)
                {
                    smoothedState.UserViewportBounds = userSample.ViewportBounds;
                }
                else
                {
                    smoothedState.UserViewportBounds = SmoothRect(smoothedState.UserViewportBounds, userSample.ViewportBounds, positionSmoothing);
                }

                smoothedState.UserTracked = true;
                smoothedState.UserId = userSample.UserId;
                smoothedState.UserConfidence = Mathf.Max(smoothedState.UserConfidence, userSample.Confidence);
                smoothedState.LastUserSeenTime = timestamp;
                return;
            }

            if (smoothedState.UserTracked && timestamp - smoothedState.LastUserSeenTime <= userLossGraceSeconds)
            {
                smoothedState.UserConfidence = Mathf.Max(minimumUserConfidence * 0.5f, smoothedState.UserConfidence * 0.97f);
                return;
            }

            smoothedState.UserTracked = false;
            smoothedState.UserConfidence = 0f;
            smoothedState.UserId = 0;
            smoothedState.UserViewportBounds = default;
        }

        private HandSample UpdateHandState(ref SmoothedHandState state, double timestamp, HandRecognitionSample sample)
        {
            if (sample.IsTracked && sample.Confidence >= minimumHandConfidence)
            {
                state.Position = state.IsTracked
                    ? Vector2.Lerp(state.Position, sample.ViewportPosition, 1f - Mathf.Clamp01(positionSmoothing))
                    : sample.ViewportPosition;
                state.Confidence = Mathf.Max(state.Confidence, sample.Confidence);
                state.IsTracked = true;
                state.LastSeenTime = timestamp;
                state.DisplayGesture = UpdateGestureState(ref state, sample.Gesture);
                return new HandSample(true, state.Confidence, state.Position, state.DisplayGesture);
            }

            if (state.IsTracked && timestamp - state.LastSeenTime <= handLossGraceSeconds)
            {
                state.Confidence = Mathf.Max(minimumHandConfidence * 0.5f, state.Confidence * 0.94f);
                return new HandSample(true, state.Confidence, state.Position, state.DisplayGesture);
            }

            state = default;
            return new HandSample(false, 0f, sample.ViewportPosition, HandGestureType.None);
        }

        private HandGestureType UpdateGestureState(ref SmoothedHandState state, HandGestureType nextGesture)
        {
            if (nextGesture == state.PendingGesture)
            {
                state.PendingFrames++;
            }
            else
            {
                state.PendingGesture = nextGesture;
                state.PendingFrames = 1;
            }

            if (state.PendingFrames >= Mathf.Max(1, gestureStabilityFrames))
            {
                state.DisplayGesture = state.PendingGesture;
            }

            return state.DisplayGesture;
        }

        private static Rect SmoothRect(Rect current, Rect target, float smoothing)
        {
            var factor = 1f - Mathf.Clamp01(smoothing);
            return new Rect(
                Mathf.Lerp(current.x, target.x, factor),
                Mathf.Lerp(current.y, target.y, factor),
                Mathf.Lerp(current.width, target.width, factor),
                Mathf.Lerp(current.height, target.height, factor));
        }

        private struct SmoothedRecognitionState
        {
            public bool UserTracked;
            public int UserId;
            public float UserConfidence;
            public Rect UserViewportBounds;
            public double LastUserSeenTime;
            public SmoothedHandState LeftHand;
            public SmoothedHandState RightHand;
        }

        private struct SmoothedHandState
        {
            public bool IsTracked;
            public float Confidence;
            public Vector2 Position;
            public HandGestureType DisplayGesture;
            public HandGestureType PendingGesture;
            public int PendingFrames;
            public double LastSeenTime;
        }
    }
}
