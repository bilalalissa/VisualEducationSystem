#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public readonly struct HandRecognitionSample
    {
        public HandRecognitionSample(bool isTracked, float confidence, Vector2 viewportPosition, HandGestureType gesture)
        {
            IsTracked = isTracked;
            Confidence = confidence;
            ViewportPosition = viewportPosition;
            Gesture = gesture;
        }

        public bool IsTracked { get; }
        public float Confidence { get; }
        public Vector2 ViewportPosition { get; }
        public HandGestureType Gesture { get; }
    }

    public readonly struct UserRecognitionSample
    {
        public UserRecognitionSample(bool isTracked, int userId, float confidence, Rect viewportBounds)
        {
            IsTracked = isTracked;
            UserId = userId;
            Confidence = confidence;
            ViewportBounds = viewportBounds;
        }

        public bool IsTracked { get; }
        public int UserId { get; }
        public float Confidence { get; }
        public Rect ViewportBounds { get; }
    }

    public readonly struct WebcamRecognitionFrame
    {
        public WebcamRecognitionFrame(UserRecognitionSample user, HandRecognitionSample leftHand, HandRecognitionSample rightHand, double timestamp)
        {
            User = user;
            LeftHand = leftHand;
            RightHand = rightHand;
            Timestamp = timestamp;
        }

        public UserRecognitionSample User { get; }
        public HandRecognitionSample LeftHand { get; }
        public HandRecognitionSample RightHand { get; }
        public double Timestamp { get; }
    }
}
