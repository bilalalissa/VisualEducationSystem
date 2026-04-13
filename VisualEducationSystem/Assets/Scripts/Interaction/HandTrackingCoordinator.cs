#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualEducationSystem.Interaction
{
    public sealed class HandTrackingCoordinator : MonoBehaviour
    {
        private IHandTrackingProvider? provider;
        private SimulatedHandTrackingProvider? fallbackProvider;
        private WebcamHandTrackingProvider? webcamProvider;
        private string lastStatusMessage = "Waiting for user selection.";
        private string activeProviderName = "None";
        private string providerStatusSummary = "No provider bound.";
        private string recognitionStatusSummary = "Recognition backend unavailable.";
        private HandGestureType previousLeftGesture = HandGestureType.None;
        private HandGestureType previousRightGesture = HandGestureType.None;

        public static HandTrackingCoordinator? Instance { get; private set; }

        public HandTrackingLifecycle Lifecycle { get; private set; } = HandTrackingLifecycle.WaitingForUser;
        public int LockedUserId { get; private set; } = -1;
        public float LockedUserConfidence { get; private set; }
        public Rect LockedUserViewportBounds { get; private set; }
        public HandSample LeftHand { get; private set; }
        public HandSample RightHand { get; private set; }
        public string LastStatusMessage => lastStatusMessage;
        public string ActiveProviderName => activeProviderName;
        public string ProviderStatusSummary => providerStatusSummary;
        public string RecognitionStatusSummary => recognitionStatusSummary;
        public Texture? ProviderDebugTexture => provider?.DebugTexture;

        public static void EnsureOn(GameObject host)
        {
            if (host.GetComponent<HandTrackingCoordinator>() != null)
            {
                return;
            }

            host.AddComponent<WebcamHandTrackingProvider>();
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

            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                ToggleProviderPreference();
            }

            if (provider == null || !provider.TryGetFrame(out var frame))
            {
                activeProviderName = provider?.ProviderDisplayName ?? "None";
                providerStatusSummary = provider?.StatusSummary ?? "No provider available.";
                recognitionStatusSummary = webcamProvider != null
                    ? (webcamProvider.HasOperationalRecognition ? "Recognition backend operational." : "Recognition backend not installed.")
                    : "Recognition backend unavailable.";
                lastStatusMessage = "No hand-tracking provider frame available.";
                return;
            }

            activeProviderName = provider.ProviderDisplayName;
            providerStatusSummary = provider.StatusSummary;
            recognitionStatusSummary = webcamProvider != null
                ? (webcamProvider.HasOperationalRecognition ? "Recognition backend operational." : "Recognition backend not installed.")
                : "Recognition backend unavailable.";

            var previousLifecycle = Lifecycle;
            var previousStatus = lastStatusMessage;
            var priorLeftGesture = LeftHand.Gesture;
            var priorRightGesture = RightHand.Gesture;

            StepStateMachine(frame);

            if (Lifecycle != previousLifecycle)
            {
                RuntimeEventLogger.LogEvent("hand_tracking.lifecycle", $"{previousLifecycle} -> {Lifecycle}");
            }

            if (LeftHand.Gesture != priorLeftGesture)
            {
                RuntimeEventLogger.LogEvent("hand_tracking.left_hand", $"Gesture={LeftHand.Gesture} Confidence={LeftHand.Confidence:F2}");
            }

            if (RightHand.Gesture != priorRightGesture)
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
            var leftOpenPalmTriggered = frame.LeftHand.Gesture == HandGestureType.OpenPalm && previousLeftGesture != HandGestureType.OpenPalm;
            var leftFistTriggered = frame.LeftHand.Gesture == HandGestureType.Fist && previousLeftGesture != HandGestureType.Fist;
            var rightThumbsUpTriggered = frame.RightHand.Gesture == HandGestureType.ThumbsUp && previousRightGesture != HandGestureType.ThumbsUp;

            switch (Lifecycle)
            {
                case HandTrackingLifecycle.WaitingForUser:
                    LeftHand = frame.LeftHand;
                    RightHand = frame.RightHand;
                    if (frame.HasCandidateUser && rightThumbsUpTriggered)
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

                    if (leftOpenPalmTriggered)
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

                    if (leftFistTriggered)
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
                        Lifecycle = leftOpenPalmTriggered
                            ? HandTrackingLifecycle.Active
                            : HandTrackingLifecycle.SelectedPaused;
                        lastStatusMessage = Lifecycle == HandTrackingLifecycle.Active
                            ? "Locked user reacquired. Tracking active."
                            : "Locked user reacquired. Tracking paused.";
                    }
                    else if (frame.HasCandidateUser && rightThumbsUpTriggered)
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

            previousLeftGesture = frame.LeftHand.Gesture;
            previousRightGesture = frame.RightHand.Gesture;
        }

        private void RefreshProviderReference()
        {
            webcamProvider = GetComponent<WebcamHandTrackingProvider>();
            fallbackProvider = GetComponent<SimulatedHandTrackingProvider>();
            provider = webcamProvider != null && webcamProvider.IsAvailable
                ? webcamProvider
                : fallbackProvider;
            activeProviderName = provider?.ProviderDisplayName ?? "None";
            providerStatusSummary = provider?.StatusSummary ?? "No provider available.";
            recognitionStatusSummary = webcamProvider != null
                ? (webcamProvider.HasOperationalRecognition ? "Recognition backend operational." : "Recognition backend not installed.")
                : "Recognition backend unavailable.";
        }

        private void ToggleProviderPreference()
        {
            webcamProvider = GetComponent<WebcamHandTrackingProvider>();
            fallbackProvider = GetComponent<SimulatedHandTrackingProvider>();
            if (provider == webcamProvider || webcamProvider == null || !webcamProvider.IsAvailable)
            {
                provider = fallbackProvider;
            }
            else
            {
                provider = webcamProvider;
            }

            activeProviderName = provider?.ProviderDisplayName ?? "None";
            providerStatusSummary = provider?.StatusSummary ?? "No provider available.";
            lastStatusMessage = $"Switched hand-tracking provider to {activeProviderName}.";
            RuntimeEventLogger.LogEvent("hand_tracking.provider", lastStatusMessage);
        }
    }
}
