#nullable enable
using UnityEngine;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Interaction
{
    public sealed class HandTrackingDebugHUD : MonoBehaviour
    {
        private GUIStyle? panelStyle;
        private GUIStyle? labelStyle;
        private GUIStyle? titleStyle;
        private Rect lastMainPanelRect;

        private void OnGUI()
        {
            var coordinator = HandTrackingCoordinator.Instance;
            if (coordinator == null)
            {
                return;
            }

            EnsureStyles();

            var showFullPanel = HudSettingsStore.IsHandTrackingPanelVisible();
            if (HudSettingsStore.IsHandTrackingIndicatorVisible() && !showFullPanel)
            {
                DrawCompactIndicator(coordinator);
            }

            if (!showFullPanel)
            {
                DrawPreviewPanel(coordinator);
                return;
            }

            var rect = new Rect(Screen.width - 376f, 16f, 360f, 282f);
            lastMainPanelRect = rect;
            GUI.Box(rect, string.Empty, panelStyle);

            var lineY = rect.y + 12f;
            const float lineHeight = 24f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 24f), "Hand Tracking", titleStyle);
            lineY += 28f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Mode: {coordinator.ActiveProviderName}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"State: {coordinator.Lifecycle}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"User: {(coordinator.LockedUserId >= 0 ? coordinator.LockedUserId.ToString() : "None")}  |  Confidence: {coordinator.LockedUserConfidence:F2}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Left: {DescribeHand(coordinator.LeftHand)}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, lineHeight), $"Right: {DescribeHand(coordinator.RightHand)}", labelStyle);
            lineY += lineHeight;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 42f), $"Status: {coordinator.LastStatusMessage}", labelStyle);
            lineY += 46f;
            GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 36f), "Controls: F7 select, F8 start, F9 pause, F10 mode switch.", labelStyle);
            lineY += 40f;
            var logPath = RuntimeEventLogger.CurrentLogFilePath;
            if (!string.IsNullOrWhiteSpace(logPath))
            {
                GUI.Label(new Rect(rect.x + 12f, lineY, rect.width - 24f, 24f), $"Log: {System.IO.Path.GetFileName(logPath)}", labelStyle);
                lineY += 26f;
            }

            DrawPreviewPanel(coordinator);
        }

        private void DrawCompactIndicator(HandTrackingCoordinator coordinator)
        {
            var rect = new Rect(Screen.width - 272f, 16f, 256f, 64f);
            GUI.Box(rect, string.Empty, panelStyle);

            var modeLabel = coordinator.ActiveProviderName.Contains("Webcam") ? "Webcam" : "Simulated";
            var userLabel = coordinator.LockedUserId >= 0 ? $"User {coordinator.LockedUserId}" : "No user";
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f), $"{modeLabel} | {coordinator.Lifecycle}", titleStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 28f, rect.width - 20f, 16f), userLabel, labelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, 16f), $"L:{coordinator.LeftHand.Gesture}  R:{coordinator.RightHand.Gesture}", labelStyle);
        }

        private void DrawPreviewPanel(HandTrackingCoordinator coordinator)
        {
            var debugTexture = coordinator.ProviderDebugTexture;
            if (debugTexture == null || !HudSettingsStore.IsHandTrackingCameraPreviewVisible())
            {
                return;
            }

            var anchorRect = lastMainPanelRect.width > 0f
                ? lastMainPanelRect
                : new Rect(Screen.width - 416f, 16f, 400f, 484f);

            var preferredX = anchorRect.x - 264f;
            var panelX = preferredX >= 16f ? preferredX : 16f;
            var panelY = anchorRect.y;
            var panelRect = new Rect(panelX, panelY, 252f, 212f);
            GUI.Box(panelRect, string.Empty, panelStyle);

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 18f), "Live Provider Preview", titleStyle);
            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 28f, panelRect.width - 20f, 30f), "Webcam feed only. Hand tracking status and instructions remain in the main panel.", labelStyle);

            var previewRect = new Rect(panelRect.x + 10f, panelRect.y + 62f, panelRect.width - 20f, 140f);
            GUI.DrawTexture(previewRect, debugTexture, ScaleMode.ScaleToFit, false);
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
                fontSize = 15
            };
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
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
