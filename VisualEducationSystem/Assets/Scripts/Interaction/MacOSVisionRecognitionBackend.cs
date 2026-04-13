#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public sealed class MacOSVisionRecognitionBackend : IWebcamRecognitionBackend
    {
        public string BackendName => "macOS Vision";
        public string StatusSummary { get; private set; } = "Native macOS Vision hand recognition plugin is not installed yet.";
        public bool IsOperational { get; private set; }

        public MacOSVisionRecognitionBackend()
        {
            RefreshAvailability();
        }

        public bool TryRecognizeFrame(Texture sourceTexture, out WebcamRecognitionFrame frame)
        {
            if (!IsOperational)
            {
                frame = default;
                return false;
            }

            if (!MacOSVisionNativeBridge.TryGetLatestFrameJson(out var json, out var status))
            {
                StatusSummary = status;
                frame = default;
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                StatusSummary = "Native macOS Vision backend returned an empty frame payload.";
                frame = default;
                return false;
            }

            var payload = JsonUtility.FromJson<MacOSVisionFramePayload>(json);
            if (payload == null)
            {
                StatusSummary = "Native macOS Vision backend returned invalid JSON payload.";
                frame = default;
                return false;
            }

            StatusSummary = "Native macOS Vision frame received.";
            frame = new WebcamRecognitionFrame(
                new UserRecognitionSample(
                    payload.hasUser,
                    payload.userId,
                    payload.userConfidence,
                    new Rect(payload.userViewportX, payload.userViewportY, payload.userViewportWidth, payload.userViewportHeight)),
                new HandRecognitionSample(
                    payload.leftTracked,
                    payload.leftConfidence,
                    new Vector2(payload.leftViewportX, payload.leftViewportY),
                    ParseGesture(payload.leftGesture)),
                new HandRecognitionSample(
                    payload.rightTracked,
                    payload.rightConfidence,
                    new Vector2(payload.rightViewportX, payload.rightViewportY),
                    ParseGesture(payload.rightGesture)),
                payload.timestamp <= 0d ? Time.unscaledTimeAsDouble : payload.timestamp);
            return true;
        }

        private void RefreshAvailability()
        {
            if (MacOSVisionNativeBridge.TryIsBackendAvailable(out var isAvailable, out var status))
            {
                IsOperational = isAvailable;
                StatusSummary = status;
            }
            else
            {
                IsOperational = false;
                StatusSummary = status;
            }
        }

        private static HandGestureType ParseGesture(string? gesture)
        {
            if (string.IsNullOrWhiteSpace(gesture))
            {
                return HandGestureType.None;
            }

            return gesture.Trim().ToLowerInvariant() switch
            {
                "thumbsup" => HandGestureType.ThumbsUp,
                "openpalm" => HandGestureType.OpenPalm,
                "fist" => HandGestureType.Fist,
                "pinch" => HandGestureType.Pinch,
                "point" => HandGestureType.Point,
                _ => HandGestureType.None
            };
        }
    }
}
