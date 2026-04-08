#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public enum HandTrackingLifecycle
    {
        WaitingForUser = 0,
        SelectedPaused = 1,
        Active = 2,
        LostUser = 3
    }

    public enum HandGestureType
    {
        None = 0,
        ThumbsUp = 1,
        OpenPalm = 2,
        Fist = 3,
        Pinch = 4,
        Point = 5
    }

    public readonly struct HandSample
    {
        public HandSample(bool isTracked, float confidence, Vector2 viewportPosition, HandGestureType gesture)
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

    public readonly struct HandTrackingFrame
    {
        public HandTrackingFrame(
            bool hasCandidateUser,
            int candidateUserId,
            float candidateConfidence,
            Rect candidateViewportBounds,
            HandSample leftHand,
            HandSample rightHand,
            double timestamp)
        {
            HasCandidateUser = hasCandidateUser;
            CandidateUserId = candidateUserId;
            CandidateConfidence = candidateConfidence;
            CandidateViewportBounds = candidateViewportBounds;
            LeftHand = leftHand;
            RightHand = rightHand;
            Timestamp = timestamp;
        }

        public bool HasCandidateUser { get; }
        public int CandidateUserId { get; }
        public float CandidateConfidence { get; }
        public Rect CandidateViewportBounds { get; }
        public HandSample LeftHand { get; }
        public HandSample RightHand { get; }
        public double Timestamp { get; }
    }
}
