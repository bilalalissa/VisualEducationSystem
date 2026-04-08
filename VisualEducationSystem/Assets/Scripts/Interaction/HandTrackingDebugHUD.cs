#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public sealed class HandTrackingDebugHUD : MonoBehaviour
    {
        private GUIStyle? panelStyle;
        private GUIStyle? labelStyle;
        private GUIStyle? titleStyle;

        private void OnGUI()
        {
            var coordinator = HandTrackingCoordinator.Instance;
            if (coordinator == null)
            {
                return;
            }

            EnsureStyles();

            var rect = new Rect(Screen.width - 376f, 16f, 360f, 286f);
            GUI.Box(rect, string.Empty, panelStyle);

            var lineY = rect.y + 12f;
            const float lineHeight = 20f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 22f), "Hand Tracking Simulation", titleStyle);
            lineY += 24f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Hand Tracking: {coordinator.Lifecycle}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Locked User: {(coordinator.LockedUserId >= 0 ? coordinator.LockedUserId.ToString() : "None")}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Confidence: {coordinator.LockedUserConfidence:F2}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Left Hand: {DescribeHand(coordinator.LeftHand)}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Right Hand: {DescribeHand(coordinator.RightHand)}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 34f), $"Status: {coordinator.LastStatusMessage}", labelStyle);
            lineY += 42f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 18f), "User select:", labelStyle);
            lineY += 16f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "F7 = right-hand thumbs-up locks current user", labelStyle);
            lineY += 18f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "F6 = show / hide simulated user", labelStyle);
            lineY += 24f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 18f), "Left hand controls:", labelStyle);
            lineY += 16f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "F8 = open palm (start / resume)", labelStyle);
            lineY += 18f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "F9 = fist (pause)", labelStyle);
            lineY += 24f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 18f), "Right hand actions:", labelStyle);
            lineY += 16f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "P = pinch", labelStyle);
            lineY += 18f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "O = point", labelStyle);
            lineY += 18f;
            GUI.Label(new Rect(rect.x + 22f, lineY, rect.width - 34f, 18f), "I/J/K/L = move simulated right hand", labelStyle);
            lineY += 22f;
            var logPath = RuntimeEventLogger.CurrentLogFilePath;
            if (!string.IsNullOrWhiteSpace(logPath))
            {
                GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 34f), $"Session log: {System.IO.Path.GetFileName(logPath)}", labelStyle);
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null && labelStyle != null && titleStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.1f, 0.88f));
            texture.Apply();
            panelStyle.normal.background = texture;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 12
            };
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;
        }

        private static string DescribeHand(HandSample hand)
        {
            return hand.IsTracked ? $"{hand.Gesture} ({hand.Confidence:F2})" : "Not tracked";
        }
    }
}
