#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public sealed class HandTrackingCoordinator : MonoBehaviour
    {
        private IHandTrackingProvider? provider;
        private SimulatedHandTrackingProvider? fallbackProvider;
        private string lastStatusMessage = "Waiting for user selection.";

        public static HandTrackingCoordinator? Instance { get; private set; }

        public HandTrackingLifecycle Lifecycle { get; private set; } = HandTrackingLifecycle.WaitingForUser;
        public int LockedUserId { get; private set; } = -1;
        public float LockedUserConfidence { get; private set; }
        public Rect LockedUserViewportBounds { get; private set; }
        public HandSample LeftHand { get; private set; }
        public HandSample RightHand { get; private set; }
        public string LastStatusMessage => lastStatusMessage;

        public static void EnsureOn(GameObject host)
        {
            if (host.GetComponent<HandTrackingCoordinator>() != null)
            {
                return;
            }

            host.AddComponent<SimulatedHandTrackingProvider>();
            host.AddComponent<HandTrackingCoordinator>();
            host.AddComponent<HandTrackingDebugHUD>();
        }

        private void Awake()
        {
            Instance = this;
            RefreshProviderReference();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (provider == null)
            {
                RefreshProviderReference();
            }

            if (provider == null || !provider.TryGetFrame(out var frame))
            {
                lastStatusMessage = "No hand-tracking provider frame available.";
                return;
            }

            var previousLifecycle = Lifecycle;
            var previousStatus = lastStatusMessage;
            var previousLeftGesture = LeftHand.Gesture;
            var previousRightGesture = RightHand.Gesture;

            StepStateMachine(frame);

            if (Lifecycle != previousLifecycle)
            {
                RuntimeEventLogger.LogEvent("hand_tracking.lifecycle", $"{previousLifecycle} -> {Lifecycle}");
            }

            if (LeftHand.Gesture != previousLeftGesture)
            {
                RuntimeEventLogger.LogEvent("hand_tracking.left_hand", $"Gesture={LeftHand.Gesture} Confidence={LeftHand.Confidence:F2}");
            }

            if (RightHand.Gesture != previousRightGesture)
            {
                RuntimeEventLogger.LogEvent("hand_tracking.right_hand", $"Gesture={RightHand.Gesture} Confidence={RightHand.Confidence:F2}");
            }

            if (!string.Equals(previousStatus, lastStatusMessage, System.StringComparison.Ordinal))
            {
                RuntimeEventLogger.LogEvent("hand_tracking.status", lastStatusMessage);
            }
        }

        private void StepStateMachine(HandTrackingFrame frame)
        {
            var candidateMatchesLock = frame.HasCandidateUser && frame.CandidateUserId == LockedUserId;

            switch (Lifecycle)
            {
                case HandTrackingLifecycle.WaitingForUser:
                    LeftHand = frame.LeftHand;
                    RightHand = frame.RightHand;
                    if (frame.HasCandidateUser && frame.RightHand.Gesture == HandGestureType.ThumbsUp)
                    {
                        LockedUserId = frame.CandidateUserId;
                        LockedUserConfidence = frame.CandidateConfidence;
                        LockedUserViewportBounds = frame.CandidateViewportBounds;
                        Lifecycle = HandTrackingLifecycle.SelectedPaused;
                        lastStatusMessage = "User selected. Left open palm starts tracking.";
                    }
                    else
                    {
                        lastStatusMessage = frame.HasCandidateUser
                            ? "Candidate user detected. Use right-hand thumbs-up to select."
                            : "No user selected.";
                    }
                    break;

                case HandTrackingLifecycle.SelectedPaused:
                    if (!candidateMatchesLock)
                    {
                        Lifecycle = HandTrackingLifecycle.LostUser;
                        lastStatusMessage = "Selected user lost. Reacquiring.";
                        break;
                    }

                    LeftHand = frame.LeftHand;
                    RightHand = frame.RightHand;
                    LockedUserConfidence = frame.CandidateConfidence;
                    LockedUserViewportBounds = frame.CandidateViewportBounds;

                    if (frame.LeftHand.Gesture == HandGestureType.OpenPalm)
                    {
                        Lifecycle = HandTrackingLifecycle.Active;
                        lastStatusMessage = "Tracking active.";
                    }
                    else
                    {
                        lastStatusMessage = "Tracking paused. Left open palm resumes.";
                    }
                    break;

                case HandTrackingLifecycle.Active:
                    if (!candidateMatchesLock)
                    {
                        Lifecycle = HandTrackingLifecycle.LostUser;
                        lastStatusMessage = "Active user lost. Reacquiring.";
                        break;
                    }

                    LeftHand = frame.LeftHand;
                    RightHand = frame.RightHand;
                    LockedUserConfidence = frame.CandidateConfidence;
                    LockedUserViewportBounds = frame.CandidateViewportBounds;

                    if (frame.LeftHand.Gesture == HandGestureType.Fist)
                    {
                        Lifecycle = HandTrackingLifecycle.SelectedPaused;
                        lastStatusMessage = "Tracking paused by left-hand fist.";
                    }
                    else
                    {
                        lastStatusMessage = "Tracking active.";
                    }
                    break;

                case HandTrackingLifecycle.LostUser:
                    LeftHand = frame.LeftHand;
                    RightHand = frame.RightHand;
                    if (candidateMatchesLock)
                    {
                        LockedUserConfidence = frame.CandidateConfidence;
                        LockedUserViewportBounds = frame.CandidateViewportBounds;
                        Lifecycle = frame.LeftHand.Gesture == HandGestureType.OpenPalm
                            ? HandTrackingLifecycle.Active
                            : HandTrackingLifecycle.SelectedPaused;
                        lastStatusMessage = Lifecycle == HandTrackingLifecycle.Active
                            ? "Locked user reacquired. Tracking active."
                            : "Locked user reacquired. Tracking paused.";
                    }
                    else if (frame.HasCandidateUser && frame.RightHand.Gesture == HandGestureType.ThumbsUp)
                    {
                        LockedUserId = frame.CandidateUserId;
                        LockedUserConfidence = frame.CandidateConfidence;
                        LockedUserViewportBounds = frame.CandidateViewportBounds;
                        Lifecycle = HandTrackingLifecycle.SelectedPaused;
                        lastStatusMessage = "New user selected. Left open palm starts tracking.";
                    }
                    else
                    {
                        lastStatusMessage = "Waiting to reacquire selected user or select a new one.";
                    }
                    break;
            }
        }

        private void RefreshProviderReference()
        {
            fallbackProvider = GetComponent<SimulatedHandTrackingProvider>();
            provider = fallbackProvider;
        }
    }
}
