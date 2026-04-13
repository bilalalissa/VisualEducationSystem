#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using VisualEducationSystem.Rooms;

namespace VisualEducationSystem.UI
{
    public sealed class CurrentRoomHUD : MonoBehaviour
    {
        private const float BoxMargin = 16f;
        private const float BoxMaxScreenWidthRatio = 0.4f;
        private const float BoxHorizontalPadding = 14f;
        private const float BoxTopPadding = 10f;
        private const float BoxBottomPadding = 5f;
        private const float BoxVerticalGap = 8f;
        private const float SlideVisibleWidth = 22f;
        private const float HideTransitionSeconds = 0.22f;

        private string currentPalaceName = "Untitled Palace";
        private string currentRoomName = "Entry Hall";
        private bool showFullMap;
        private float lastMovementTime;
        private Texture2D? whiteTexture;
        private GUIStyle? hudTextStyle;
        private GUIStyle? hudTitleStyle;
        private GUIStyle? mapLabelStyle;
        private GUIStyle? mapHintStyle;

        public void SetCurrentPalace(string palaceName)
        {
            currentPalaceName = string.IsNullOrWhiteSpace(palaceName) ? "Untitled Palace" : palaceName;
        }

        public void SetCurrentRoom(string roomName)
        {
            currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "Unknown Room" : roomName;
        }

        private void Awake()
        {
            lastMovementTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Keyboard.current != null && !RoomEditorController.IsAnyEditorOpen && Keyboard.current.mKey.wasPressedThisFrame)
            {
                showFullMap = !showFullMap;
            }

            HandleAltShortcuts();

            if (IsUserActivelyMoving())
            {
                lastMovementTime = Time.unscaledTime;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            var maxBoxWidth = Mathf.Max(220f, Screen.width * BoxMaxScreenWidthRatio);
            var hideBlend = ComputeHideBlend();
            var pointerEditMode = IsPointerEditMode();

            var palaceRect = BuildContentBoxRect(
                $"Current Palace: {currentPalaceName}",
                BoxMargin,
                BoxMargin,
                maxBoxWidth,
                false);
            palaceRect = GetPanelRect(palaceRect, HudPanelId.Palace, true, hideBlend);
            DrawHudPanel(palaceRect, $"Current Palace: {currentPalaceName}", HudPanelId.Palace, pointerEditMode);

            var roomRect = BuildContentBoxRect(
                $"Current Room: {currentRoomName}",
                BoxMargin,
                palaceRect.yMax + BoxVerticalGap,
                maxBoxWidth,
                false);
            roomRect = GetPanelRect(roomRect, HudPanelId.Room, true, hideBlend);
            DrawHudPanel(roomRect, $"Current Room: {currentRoomName}", HudPanelId.Room, pointerEditMode);

            const string helpText = "WASD + mouse. Esc for menu. E edits room.\nWalk through portals or use the editor for parent/sub-room travel.\nInk clues: start from the room editor. Right-hand pinch draws. Right-hand fist erases in erase mode.\nPress M to toggle the palace map.";
            var helpRect = BuildContentBoxRect(
                helpText,
                BoxMargin,
                roomRect.yMax + BoxVerticalGap,
                maxBoxWidth,
                true);
            helpRect = GetPanelRect(helpRect, HudPanelId.Help, true, hideBlend);
            DrawHudPanel(helpRect, helpText, HudPanelId.Help, pointerEditMode);

            var miniMapRect = BuildMiniMapRect();
            miniMapRect = GetPanelRect(miniMapRect, HudPanelId.MiniMap, false, hideBlend);
            DrawMap(miniMapRect, false, pointerEditMode);

            if (showFullMap)
            {
                DrawMap(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 130f, 380f, 260f), true, false);
            }
        }

        private void DrawMap(Rect rect, bool expanded, bool allowPanelToggle)
        {
            var bootstrap = PrototypePalaceBootstrap.Instance;
            if (bootstrap == null)
            {
                return;
            }

            var rooms = bootstrap.GetMapRoomInfos();
            if (rooms.Count == 0)
            {
                return;
            }

            var (backgroundColor, panelBorderColor, textColor) = GetThemeColors();
            DrawPanelBackground(rect, backgroundColor, panelBorderColor);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f), expanded ? "Palace Map" : "Mini Map", hudTitleStyle);

            if (allowPanelToggle)
            {
                DrawPanelModeBadge(rect, HudPanelId.MiniMap, "4");
            }

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach (var room in rooms)
            {
                minX = Mathf.Min(minX, room.Center.x);
                maxX = Mathf.Max(maxX, room.Center.x);
                minZ = Mathf.Min(minZ, room.Center.z);
                maxZ = Mathf.Max(maxZ, room.Center.z);
            }

            var roomCount = rooms.Count;
            var boundsPadding = expanded ? 8f : 5f;
            minX -= boundsPadding;
            maxX += boundsPadding;
            minZ -= boundsPadding;
            maxZ += boundsPadding;

            var width = Mathf.Max(1f, maxX - minX);
            var height = Mathf.Max(1f, maxZ - minZ);
            var mapArea = new Rect(rect.x + 10f, rect.y + 30f, rect.width - 20f, rect.height - 40f);
            var markerWidth = expanded ? Mathf.Clamp(82f - roomCount * 2.4f, 38f, 82f) : Mathf.Clamp(20f - roomCount * 0.35f, 10f, 18f);
            var markerHeight = expanded ? Mathf.Clamp(28f - roomCount * 0.45f, 16f, 28f) : Mathf.Clamp(12f - roomCount * 0.18f, 7f, 10f);
            var labelChars = expanded ? Mathf.Clamp(Mathf.RoundToInt(18f - roomCount * 0.4f), 8, 18) : 0;
            mapLabelStyle!.fontSize = expanded
                ? Mathf.Clamp(Mathf.RoundToInt(13f - roomCount * 0.22f), 8, 13)
                : 10;

            var markerCenters = new System.Collections.Generic.Dictionary<string, Vector2>(roomCount);
            var roomLookup = new System.Collections.Generic.Dictionary<string, PrototypePalaceBootstrap.MapRoomInfo>(roomCount);
            foreach (var room in rooms)
            {
                markerCenters[room.RoomId] = GetMapPoint(room.Center, minX, minZ, width, height, mapArea);
                roomLookup[room.RoomId] = room;
            }

            foreach (var room in rooms)
            {
                var parentRoomId = GetEffectiveMapParentRoomId(room, roomLookup);
                if (string.IsNullOrWhiteSpace(parentRoomId) || !markerCenters.TryGetValue(parentRoomId, out var parentPoint))
                {
                    continue;
                }

                DrawMapLine(parentPoint, markerCenters[room.RoomId], Color.Lerp(room.AccentColor, Color.white, 0.35f), expanded ? 2.25f : 1.4f);
            }

            foreach (var room in rooms)
            {
                var markerCenter = markerCenters[room.RoomId];
                var markerRect = new Rect(
                    markerCenter.x - markerWidth * 0.5f,
                    markerCenter.y - markerHeight * 0.5f,
                    markerWidth,
                    markerHeight);

                var fillColor = room.DisplayName == currentRoomName
                    ? new Color(1f, 0.91f, 0.4f, 1f)
                    : Color.Lerp(room.AccentColor, room.IsSubRoom ? Color.white : new Color(0.72f, 0.75f, 0.8f, 1f), room.IsSubRoom ? 0.42f : 0.18f);
                var markerBorderColor = room.Depth == 0
                    ? new Color(0.12f, 0.16f, 0.22f, 0.95f)
                    : Color.Lerp(new Color(0.17f, 0.2f, 0.26f, 0.95f), room.AccentColor, 0.25f);
                DrawFilledRect(markerRect, fillColor, markerBorderColor);
                if (expanded)
                {
                    mapLabelStyle.normal.textColor = GetReadableTextColor(fillColor);
                    GUI.Label(markerRect, BuildMapLabel(room.DisplayName, labelChars), mapLabelStyle);
                }
            }

            if (bootstrap.TryGetPlayerMapPosition(out var playerPosition))
            {
                var normalizedX = Mathf.Clamp01((playerPosition.x - minX) / width);
                var normalizedY = Mathf.Clamp01((playerPosition.z - minZ) / height);
                var outerSize = expanded ? 14f : 10f;
                var innerSize = expanded ? 8f : 5f;
                var outerRect = new Rect(
                    mapArea.x + normalizedX * (mapArea.width - outerSize),
                    mapArea.y + normalizedY * (mapArea.height - outerSize),
                    outerSize,
                    outerSize);
                var innerRect = new Rect(
                    outerRect.x + (outerSize - innerSize) * 0.5f,
                    outerRect.y + (outerSize - innerSize) * 0.5f,
                    innerSize,
                    innerSize);

                DrawFilledRect(outerRect, Color.white, new Color(0.1f, 0.1f, 0.14f, 1f));
                DrawFilledRect(innerRect, new Color(1f, 0.15f, 0.85f, 1f), new Color(0.55f, 0.08f, 0.4f, 1f));
            }

            if (!expanded && allowPanelToggle && IsPointerEditMode())
            {
                GUI.Label(new Rect(rect.x + 8f, rect.yMax - 20f, rect.width - 16f, 16f), "Alt+Click to pin", mapHintStyle);
            }
        }

        private static Vector2 GetMapPoint(Vector3 worldPosition, float minX, float minZ, float width, float height, Rect mapArea)
        {
            var normalizedX = Mathf.Clamp01((worldPosition.x - minX) / width);
            var normalizedY = Mathf.Clamp01((worldPosition.z - minZ) / height);
            return new Vector2(
                mapArea.x + normalizedX * mapArea.width,
                mapArea.y + normalizedY * mapArea.height);
        }

        private static string GetEffectiveMapParentRoomId(
            PrototypePalaceBootstrap.MapRoomInfo room,
            System.Collections.Generic.IReadOnlyDictionary<string, PrototypePalaceBootstrap.MapRoomInfo> roomLookup)
        {
            if (!string.IsNullOrWhiteSpace(room.ParentRoomId))
            {
                return room.ParentRoomId;
            }

            return room.RoomId != "EntryHall" && roomLookup.ContainsKey("EntryHall")
                ? "EntryHall"
                : string.Empty;
        }

        private static Color GetReadableTextColor(Color backgroundColor)
        {
            var luminance = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
            return luminance < 0.56f
                ? new Color(0.97f, 0.98f, 1f, 1f)
                : new Color(0.08f, 0.1f, 0.14f, 1f);
        }

        private void DrawHudPanel(Rect rect, string text, HudPanelId panelId, bool allowPanelToggle)
        {
            var (backgroundColor, borderColor, _) = GetThemeColors();
            DrawPanelBackground(rect, backgroundColor, borderColor);

            var textRect = new Rect(
                rect.x + BoxHorizontalPadding,
                rect.y + BoxTopPadding - 1f,
                rect.width - BoxHorizontalPadding * 2f,
                rect.height - BoxTopPadding - BoxBottomPadding + 2f);
            GUI.Label(textRect, text, hudTextStyle);

            if (!allowPanelToggle)
            {
                return;
            }

            var shortcutLabel = panelId switch
            {
                HudPanelId.Palace => "1",
                HudPanelId.Room => "2",
                HudPanelId.Help => "3",
                _ => "4"
            };
            DrawPanelModeBadge(rect, panelId, shortcutLabel);
        }

        private void DrawPanelModeBadge(Rect rect, HudPanelId panelId, string shortcutLabel)
        {
            var badgeRect = new Rect(rect.xMax - 72f, rect.y + 6f, 62f, 16f);
            var isAutoHide = HudSettingsStore.IsPanelAutoHideEnabled(panelId);
            var fillColor = isAutoHide ? new Color(0.78f, 0.9f, 1f, 0.95f) : new Color(0.93f, 0.82f, 0.56f, 0.95f);
            DrawFilledRect(badgeRect, fillColor, new Color(0.25f, 0.28f, 0.34f, 0.95f));
            mapLabelStyle!.fontSize = 10;
            GUI.Label(badgeRect, $"{shortcutLabel}:{(isAutoHide ? "AUTO" : "PIN")}", mapLabelStyle);
        }

        private float ComputeHideBlend()
        {
            if (showFullMap || IsPointerEditMode() || IsPeekMode())
            {
                return 0f;
            }

            var hideDelay = HudSettingsStore.GetAutoHideDelaySeconds();
            if (hideDelay <= 0f)
            {
                return 0f;
            }

            var revealElapsed = Time.unscaledTime - lastMovementTime - hideDelay;
            if (revealElapsed <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01(revealElapsed / HideTransitionSeconds);
        }

        private Rect GetPanelRect(Rect baseRect, HudPanelId panelId, bool leftAnchored, float hideBlend)
        {
            if (!HudSettingsStore.IsPanelAutoHideEnabled(panelId))
            {
                return baseRect;
            }

            if (leftAnchored)
            {
                var hiddenX = -(baseRect.width - SlideVisibleWidth);
                baseRect.x = Mathf.Lerp(baseRect.x, hiddenX, hideBlend);
            }
            else
            {
                var hiddenX = Screen.width - SlideVisibleWidth;
                baseRect.x = Mathf.Lerp(baseRect.x, hiddenX, hideBlend);
            }

            return baseRect;
        }

        private static string BuildMapLabel(string displayName, int maxCharacters)
        {
            if (maxCharacters <= 0 || displayName.Length <= maxCharacters)
            {
                return displayName;
            }

            if (maxCharacters <= 3)
            {
                return displayName.Substring(0, maxCharacters);
            }

            return displayName.Substring(0, maxCharacters - 2) + "..";
        }

        private void EnsureStyles()
        {
            if (hudTextStyle == null)
            {
                hudTextStyle = new GUIStyle(GUI.skin.label);
                hudTextStyle.alignment = TextAnchor.UpperLeft;
                hudTextStyle.wordWrap = true;
                hudTextStyle.clipping = TextClipping.Clip;
                hudTextStyle.fontSize = 14;
            }

            if (hudTitleStyle == null)
            {
                hudTitleStyle = new GUIStyle(GUI.skin.label);
                hudTitleStyle.alignment = TextAnchor.UpperCenter;
                hudTitleStyle.fontStyle = FontStyle.Bold;
                hudTitleStyle.fontSize = 14;
            }

            if (mapLabelStyle == null)
            {
                mapLabelStyle = new GUIStyle(GUI.skin.label);
                mapLabelStyle.alignment = TextAnchor.MiddleCenter;
                mapLabelStyle.fontSize = 11;
                mapLabelStyle.fontStyle = FontStyle.Bold;
            }

            if (mapHintStyle == null)
            {
                mapHintStyle = new GUIStyle(GUI.skin.label);
                mapHintStyle.alignment = TextAnchor.UpperLeft;
                mapHintStyle.fontSize = 10;
                mapHintStyle.fontStyle = FontStyle.Bold;
            }

            var (_, _, textColor) = GetThemeColors();
            hudTextStyle.normal.textColor = textColor;
            hudTitleStyle.normal.textColor = textColor;
            mapLabelStyle.normal.textColor = new Color(0.08f, 0.1f, 0.14f, 1f);
            mapHintStyle.normal.textColor = textColor;
        }

        private Rect BuildContentBoxRect(string text, float x, float y, float maxWidth, bool multiline)
        {
            var targetContentWidth = multiline
                ? Mathf.Min(maxWidth - BoxHorizontalPadding * 2f, MeasureMaxLineWidth(text))
                : hudTextStyle!.CalcSize(new GUIContent(text)).x;
            targetContentWidth = Mathf.Max(160f, targetContentWidth);

            var width = Mathf.Min(maxWidth, targetContentWidth + BoxHorizontalPadding * 2f);
            var contentWidth = width - BoxHorizontalPadding * 2f;
            var contentHeight = multiline
                ? hudTextStyle!.CalcHeight(new GUIContent(text), contentWidth)
                : hudTextStyle!.CalcSize(new GUIContent(text)).y;
            var height = Mathf.Ceil(contentHeight + BoxTopPadding + BoxBottomPadding);
            return new Rect(x, y, width, height);
        }

        private float MeasureMaxLineWidth(string text)
        {
            var lines = text.Split('\n');
            var maxLineWidth = 0f;
            foreach (var line in lines)
            {
                maxLineWidth = Mathf.Max(maxLineWidth, hudTextStyle!.CalcSize(new GUIContent(line)).x);
            }

            return maxLineWidth;
        }

        private bool IsUserActivelyMoving()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed)
                {
                    return true;
                }

                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    return true;
                }
            }

            if (Mouse.current == null)
            {
                return false;
            }

            if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
            {
                return true;
            }

            return Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
        }

        private bool IsPeekMode()
        {
            return Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        }

        private bool IsPointerEditMode()
        {
            return Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
        }

        private void HandleAltShortcuts()
        {
            if (Keyboard.current == null || !IsPointerEditMode())
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                HudSettingsStore.TogglePanelAutoHide(HudPanelId.Palace);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                HudSettingsStore.TogglePanelAutoHide(HudPanelId.Room);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                HudSettingsStore.TogglePanelAutoHide(HudPanelId.Help);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
            {
                HudSettingsStore.TogglePanelAutoHide(HudPanelId.MiniMap);
            }
            else if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                HudSettingsStore.ToggleAllPanelsAutoHide();
            }
        }

        private void DrawPanelBackground(Rect rect, Color fillColor, Color borderColor)
        {
            DrawFilledRect(rect, fillColor, borderColor);
        }

        private void DrawFilledRect(Rect rect, Color fillColor, Color borderColor)
        {
            var previousColor = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(rect, GetWhiteTexture());

            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), GetWhiteTexture());
            GUI.color = previousColor;
        }

        private void DrawMapLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            var delta = end - start;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, length, thickness), GetWhiteTexture());
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private (Color backgroundColor, Color borderColor, Color textColor) GetThemeColors()
        {
            return HudSettingsStore.GetThemeMode() switch
            {
                HudThemeMode.Dark => (
                    new Color(0.08f, 0.1f, 0.14f, 0.14f),
                    new Color(0.25f, 0.29f, 0.36f, 0.26f),
                    new Color(0.95f, 0.97f, 1f, 1f)),
                HudThemeMode.HighContrast => (
                    new Color(0.97f, 0.98f, 1f, 0.3f),
                    new Color(0.08f, 0.1f, 0.14f, 0.52f),
                    new Color(0.05f, 0.06f, 0.08f, 1f)),
                _ => (
                    new Color(0.94f, 0.96f, 0.99f, 0.14f),
                    new Color(0.2f, 0.24f, 0.3f, 0.22f),
                    new Color(0.08f, 0.1f, 0.14f, 1f))
            };
        }

        private Rect BuildMiniMapRect()
        {
            const float width = 160f;
            const float height = 122f;
            return HudSettingsStore.GetMiniMapAnchorMode() == MiniMapAnchorMode.BottomRight
                ? new Rect(Screen.width - width - 16f, Screen.height - height - 16f, width, height)
                : new Rect(Screen.width - width - 16f, 16f, width, height);
        }

        private Texture2D GetWhiteTexture()
        {
            if (whiteTexture != null)
            {
                return whiteTexture;
            }

            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            return whiteTexture;
        }
    }
}
