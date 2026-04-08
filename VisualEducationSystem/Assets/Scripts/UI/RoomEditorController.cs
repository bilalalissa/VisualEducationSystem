#nullable enable
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using VisualEducationSystem.Interaction;
using VisualEducationSystem.Player;
using VisualEducationSystem.Rooms;
using VisualEducationSystem.Save;

namespace VisualEducationSystem.UI
{
    public sealed class RoomEditorController : MonoBehaviour
    {
        private const string EntryHallRoomId = "EntryHall";
        private const float ClueMoveStep = 0.6f;
        private const float ClueHeightStep = 0.25f;
        private const float ClueScaleStep = 0.15f;

        [SerializeField] private PlayerRoomTracker roomTracker = null!;
        [SerializeField] private SimpleFirstPersonController playerController = null!;
        [SerializeField] private PrototypePalaceBootstrap palaceBootstrap = null!;

        private bool isOpen;
        private string draftPalaceName = string.Empty;
        private string draftName = string.Empty;
        private Color draftColor = Color.white;
        private string selectedClueId = string.Empty;
        private PalaceClueType selectedClueType = PalaceClueType.Note;
        private string draftClueTitle = string.Empty;
        private string draftClueBody = string.Empty;
        private string draftClueAssetPath = string.Empty;
        private float draftClueTextScale = 1f;
        private PalaceClueTextStyle draftClueTextStyle = PalaceClueTextStyle.Normal;
        private bool isPreviewOpen;
        private Texture2D? previewTexture;
        private string previewTexturePath = string.Empty;
        private float previewImageZoom = 1f;
        private Vector2 previewImagePan = Vector2.zero;
        private string editorStatus = string.Empty;
        private Vector2 scrollPosition;
        public static bool IsAnyEditorOpen { get; private set; }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (isOpen && isPreviewOpen && Mouse.current != null)
            {
                HandlePreviewScrollInputFromDevices();
                HandlePreviewHorizontalPanKeys();
            }

            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPreviewOpen)
                {
                    isPreviewOpen = false;
                    return;
                }

                CloseEditor();
                return;
            }

            if (isOpen)
            {
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ToggleEditor();
            }
        }

        private void OnGUI()
        {
            if (!isOpen || roomTracker.CurrentRoom == null)
            {
                return;
            }

            if (isPreviewOpen)
            {
                DrawCluePreviewOverlay();
                return;
            }

            var panelRect = new Rect(20f, 160f, Mathf.Min(420f, Screen.width - 40f), Mathf.Min(380f, Screen.height - 180f));
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            GUILayout.Label($"Editing: {roomTracker.CurrentRoom.RoomDisplayName}");
            GUILayout.Space(10f);

            if (roomTracker.CurrentRoom.RoomId == EntryHallRoomId)
            {
                GUILayout.Label("Palace Name");
                draftPalaceName = GUILayout.TextField(draftPalaceName, 40);
                GUILayout.Space(10f);
            }

            GUILayout.Label("Room Name");
            draftName = GUILayout.TextField(draftName, 32);

            GUILayout.Space(10f);
            GUILayout.Label($"Red: {draftColor.r:F2}");
            draftColor.r = GUILayout.HorizontalSlider(draftColor.r, 0f, 1f);
            GUILayout.Label($"Green: {draftColor.g:F2}");
            draftColor.g = GUILayout.HorizontalSlider(draftColor.g, 0f, 1f);
            GUILayout.Label($"Blue: {draftColor.b:F2}");
            draftColor.b = GUILayout.HorizontalSlider(draftColor.b, 0f, 1f);

            GUILayout.Space(12f);

            if (GUILayout.Button("Apply Changes", GUILayout.Height(34f)))
            {
                ApplyAndSaveCurrentDraft();
            }

            GUILayout.Space(8f);

            if (roomTracker.CurrentRoom.RoomId == EntryHallRoomId)
            {
                var canAddRoom = palaceBootstrap != null && palaceBootstrap.CanAddRoomFromEntryHall;
                GUI.enabled = canAddRoom;

                if (GUILayout.Button("Add Room From Entry Hall", GUILayout.Height(34f)))
                {
                    palaceBootstrap!.TryAddRoomFromEntryHall();
                    PalaceSaveManager.SaveCurrentState();
                    editorStatus = string.Empty;
                }

                GUI.enabled = true;
                GUILayout.Label(canAddRoom ? "Adds a new branch room from the main hall." : "No more branch slots available in the main hall.");
                GUILayout.Space(8f);
            }

            if (roomTracker.CurrentRoom.RoomId != EntryHallRoomId && palaceBootstrap != null)
            {
                var canAddSubRoom = palaceBootstrap.CanAddSubRoom(roomTracker.CurrentRoom);
                var childRooms = palaceBootstrap.GetChildRooms(roomTracker.CurrentRoom);
                var clues = palaceBootstrap.GetCluesForRoom(roomTracker.CurrentRoom);
                var hasSubRoom = childRooms.Count > 0;

                GUI.enabled = canAddSubRoom;
                if (GUILayout.Button("Add Sub-Room", GUILayout.Height(34f)))
                {
                    palaceBootstrap.TryAddSubRoom(roomTracker.CurrentRoom);
                    PalaceSaveManager.SaveCurrentState();
                    editorStatus = "Sub-room created.";
                }

                GUI.enabled = true;

                foreach (var childRoom in childRooms)
                {
                    if (GUILayout.Button($"Enter {childRoom.RoomDisplayName}", GUILayout.Height(34f)))
                    {
                        palaceBootstrap.TryTeleportToRoom(childRoom.RoomId);
                        CloseEditor();
                        return;
                    }
                }

                if (palaceBootstrap.CanNavigateToParent(roomTracker.CurrentRoom) && GUILayout.Button("Return To Parent Room", GUILayout.Height(34f)))
                {
                    palaceBootstrap.NavigateToParentRoom(roomTracker.CurrentRoom);
                    CloseEditor();
                    return;
                }

                GUILayout.Label(
                    canAddSubRoom
                        ? "Create another nested study room from this branch."
                        : hasSubRoom
                            ? "This branch has reached its current sub-room limit."
                            : "Sub-room creation is unavailable here.");
                GUILayout.Space(8f);

                GUILayout.Label("Room Clues");
                if (GUILayout.Button("New Note Clue", GUILayout.Height(30f)))
                {
                    selectedClueId = string.Empty;
                    selectedClueType = PalaceClueType.Note;
                    draftClueTitle = string.Empty;
                    draftClueBody = string.Empty;
                    draftClueAssetPath = string.Empty;
                    draftClueTextScale = 1f;
                    draftClueTextStyle = PalaceClueTextStyle.Normal;
                    editorStatus = "Creating note clue.";
                    RuntimeEventLogger.LogEvent("room_editor.clue", "Started creating a new note clue.");
                }

                if (GUILayout.Button("New Image Clue", GUILayout.Height(30f)))
                {
                    selectedClueId = string.Empty;
                    selectedClueType = PalaceClueType.Image;
                    draftClueTitle = string.Empty;
                    draftClueBody = string.Empty;
                    draftClueAssetPath = string.Empty;
                    draftClueTextScale = 1f;
                    draftClueTextStyle = PalaceClueTextStyle.Normal;
                    editorStatus = "Creating image clue.";
                    RuntimeEventLogger.LogEvent("room_editor.clue", "Started creating a new image clue.");
                }

                foreach (var clue in clues)
                {
                    if (GUILayout.Button($"Edit {clue.ClueType}: {clue.Title}", GUILayout.Height(28f)))
                    {
                        selectedClueId = clue.ClueId;
                        selectedClueType = clue.ClueType;
                        draftClueTitle = clue.Title;
                        draftClueBody = clue.BodyText;
                        draftClueAssetPath = clue.AssetPath;
                        draftClueTextScale = clue.TextScale;
                        draftClueTextStyle = clue.TextStyle;
                        editorStatus = $"Editing clue: {clue.Title}";
                        RuntimeEventLogger.LogEvent("room_editor.clue", $"Editing clue {clue.ClueId} ({clue.ClueType})");
                    }

                    if (GUILayout.Button($"Delete {clue.ClueType}: {clue.Title}", GUILayout.Height(26f)))
                    {
                        palaceBootstrap.DeleteClue(clue.ClueId);
                        PalaceSaveManager.SaveCurrentState();
                        if (selectedClueId == clue.ClueId)
                        {
                            selectedClueId = string.Empty;
                            selectedClueType = PalaceClueType.Note;
                            draftClueTitle = string.Empty;
                            draftClueBody = string.Empty;
                            draftClueAssetPath = string.Empty;
                            draftClueTextScale = 1f;
                            draftClueTextStyle = PalaceClueTextStyle.Normal;
                        }

                        GUIUtility.keyboardControl = 0;
                        selectedClueId = string.Empty;
                        editorStatus = "Clue deleted.";
                        RuntimeEventLogger.LogEvent("room_editor.clue", $"Deleted clue {clue.ClueId} ({clue.ClueType})");
                        break;
                    }
                }

                GUILayout.Space(6f);
                GUILayout.Label(selectedClueType == PalaceClueType.Image
                    ? (selectedClueId == string.Empty ? "Image Title" : "Edit Image Title")
                    : (selectedClueId == string.Empty ? "Note Title" : "Edit Note Title"));
                draftClueTitle = GUILayout.TextField(draftClueTitle, 40);

                if (selectedClueType == PalaceClueType.Image)
                {
                    GUILayout.Label("Image File Path");
                    draftClueAssetPath = GUILayout.TextArea(draftClueAssetPath, GUILayout.MinHeight(54f));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Browse Local Image", GUILayout.Height(28f)))
                    {
                        TryBrowseForImagePath();
                    }

                    if (GUILayout.Button("Clear Path", GUILayout.Height(28f)))
                    {
                        draftClueAssetPath = string.Empty;
                        RuntimeEventLogger.LogEvent("room_editor.clue", "Cleared image clue path.");
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("Note Body");
                    draftClueBody = GUILayout.TextArea(draftClueBody, GUILayout.MinHeight(72f));

                    GUILayout.Space(6f);
                    GUILayout.Label($"Text Size: {GetTextScaleLabel(draftClueTextScale)}");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Small", GUILayout.Height(28f)))
                    {
                        draftClueTextScale = 0.85f;
                    }

                    if (GUILayout.Button("Medium", GUILayout.Height(28f)))
                    {
                        draftClueTextScale = 1f;
                    }

                    if (GUILayout.Button("Large", GUILayout.Height(28f)))
                    {
                        draftClueTextScale = 1.2f;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label($"Font Style: {draftClueTextStyle}");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Normal", GUILayout.Height(28f)))
                    {
                        draftClueTextStyle = PalaceClueTextStyle.Normal;
                    }

                    if (GUILayout.Button("Bold", GUILayout.Height(28f)))
                    {
                        draftClueTextStyle = PalaceClueTextStyle.Bold;
                    }

                    if (GUILayout.Button("Italic", GUILayout.Height(28f)))
                    {
                        draftClueTextStyle = PalaceClueTextStyle.Italic;
                    }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button(selectedClueId == string.Empty
                        ? (selectedClueType == PalaceClueType.Image ? "Save New Image Clue" : "Save New Note Clue")
                        : "Save Selected Clue", GUILayout.Height(32f)))
                {
                    if (string.IsNullOrWhiteSpace(draftClueTitle))
                    {
                        editorStatus = "Enter a clue title.";
                    }
                    else if (selectedClueType == PalaceClueType.Image && string.IsNullOrWhiteSpace(draftClueAssetPath))
                    {
                        editorStatus = "Enter an image file path.";
                    }
                    else
                    {
                        string savedClueId;
                        if (selectedClueType == PalaceClueType.Image)
                        {
                            savedClueId = palaceBootstrap.SaveImageClue(roomTracker.CurrentRoom, selectedClueId, draftClueTitle, draftClueAssetPath);
                        }
                        else
                        {
                            savedClueId = palaceBootstrap.SaveNoteClue(roomTracker.CurrentRoom, selectedClueId, draftClueTitle, draftClueBody, draftClueTextScale, draftClueTextStyle);
                        }

                        PalaceSaveManager.SaveCurrentState();
                        var wasNewClue = string.IsNullOrWhiteSpace(selectedClueId);
                        selectedClueId = savedClueId;
                        editorStatus = wasNewClue
                            ? (selectedClueType == PalaceClueType.Image ? "Image clue created." : "Note clue created.")
                            : "Clue updated.";
                        RuntimeEventLogger.LogEvent(
                            "room_editor.clue",
                            $"{(wasNewClue ? "Saved new" : "Updated")} {selectedClueType} clue {savedClueId} in room {roomTracker.CurrentRoom.RoomId}.");
                    }
                }

                GUI.enabled = CanPreviewDraftClue();
                if (GUILayout.Button(selectedClueType == PalaceClueType.Image ? "Preview Image Clue" : "Preview Note Clue", GUILayout.Height(30f)))
                {
                    previewImageZoom = 1f;
                    previewImagePan = Vector2.zero;
                    isPreviewOpen = true;
                    RuntimeEventLogger.LogEvent("room_editor.preview", $"Opened preview for {(selectedClueType == PalaceClueType.Image ? "image" : "note")} clue draft.");
                }
                GUI.enabled = true;

                if (!string.IsNullOrWhiteSpace(selectedClueId))
                {
                    GUILayout.Space(8f);
                    var availableWalls = palaceBootstrap.GetAvailableClueWalls(roomTracker.CurrentRoom);
                    if (availableWalls.Count > 0)
                    {
                        GUILayout.Label("Place Selected Clue On");
                        for (var i = 0; i < availableWalls.Count; i += 2)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(availableWalls[i].Label, GUILayout.Height(28f)))
                            {
                                MoveSelectedClueToWall(availableWalls[i].Id);
                            }

                            if (i + 1 < availableWalls.Count && GUILayout.Button(availableWalls[i + 1].Label, GUILayout.Height(28f)))
                            {
                                MoveSelectedClueToWall(availableWalls[i + 1].Id);
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Space(6f);
                    }
                    else
                    {
                        GUILayout.Label("No wall slots available in this room for clue placement.");
                        GUILayout.Space(6f);
                    }

                    GUILayout.Label("Move Selected Clue");

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Left", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(-ClueMoveStep, 0f, 0f));
                    }

                    if (GUILayout.Button("Right", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(ClueMoveStep, 0f, 0f));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Forward", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(0f, 0f, ClueMoveStep));
                    }

                    if (GUILayout.Button("Back", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(0f, 0f, -ClueMoveStep));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Up", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(0f, ClueHeightStep, 0f));
                    }

                    if (GUILayout.Button("Down", GUILayout.Height(28f)))
                    {
                        NudgeSelectedClue(new Vector3(0f, -ClueHeightStep, 0f));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Scale Down", GUILayout.Height(28f)))
                    {
                        ScaleSelectedClue(-ClueScaleStep);
                    }

                    if (GUILayout.Button("Scale Up", GUILayout.Height(28f)))
                    {
                        ScaleSelectedClue(ClueScaleStep);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(8f);
            }

            if (!string.IsNullOrWhiteSpace(editorStatus))
            {
                GUILayout.Label(editorStatus, GUI.skin.box, GUILayout.Height(44f));
                GUILayout.Space(8f);
            }

            if (GUILayout.Button("Close Editor", GUILayout.Height(30f)))
            {
                CloseEditor();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            ApplyDraftPreview();

            if (isPreviewOpen)
            {
                DrawCluePreviewOverlay();
            }
        }

        private void ToggleEditor()
        {
            var currentRoom = roomTracker.CurrentRoom;
            if (currentRoom == null)
            {
                return;
            }

            if (isOpen)
            {
                CloseEditor();
                return;
            }

            isOpen = true;
            IsAnyEditorOpen = true;
            draftPalaceName = PalaceSessionState.CurrentPalaceName;
            draftName = currentRoom.RoomDisplayName;
            draftColor = currentRoom.AccentColor;
            selectedClueId = string.Empty;
            selectedClueType = PalaceClueType.Note;
            draftClueTitle = string.Empty;
            draftClueBody = string.Empty;
            draftClueAssetPath = string.Empty;
            draftClueTextScale = 1f;
            draftClueTextStyle = PalaceClueTextStyle.Normal;
            isPreviewOpen = false;
            editorStatus = string.Empty;
            playerController.SetInputEnabled(false);
            RuntimeEventLogger.LogEvent("room_editor", $"Opened room editor for {currentRoom.RoomId} ({currentRoom.RoomDisplayName}).");
        }

        private void CloseEditor()
        {
            isOpen = false;
            IsAnyEditorOpen = false;
            editorStatus = string.Empty;
            selectedClueId = string.Empty;
            selectedClueType = PalaceClueType.Note;
            draftClueTitle = string.Empty;
            draftClueBody = string.Empty;
            draftClueAssetPath = string.Empty;
            draftClueTextScale = 1f;
            draftClueTextStyle = PalaceClueTextStyle.Normal;
            isPreviewOpen = false;
            playerController.SetInputEnabled(true);
            RuntimeEventLogger.LogEvent("room_editor", "Closed room editor.");
        }

        private static string GetTextScaleLabel(float textScale)
        {
            if (textScale <= 0.9f)
            {
                return "Small";
            }

            if (textScale >= 1.1f)
            {
                return "Large";
            }

            return "Medium";
        }

        private void TryBrowseForImagePath()
        {
#if UNITY_EDITOR
            var selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Select Clue Image", string.Empty, "png,jpg,jpeg,bmp,gif,tga");
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                draftClueAssetPath = selectedPath;
                editorStatus = "Image selected.";
                RuntimeEventLogger.LogEvent("room_editor.clue", $"Selected local image path: {selectedPath}");
            }
#else
            editorStatus = "Local file browsing is available in the Unity Editor. Use the path field outside the editor.";
            RuntimeEventLogger.LogEvent("room_editor.clue", "Image browse requested outside Unity Editor.");
#endif
        }

        private bool CanPreviewDraftClue()
        {
            if (string.IsNullOrWhiteSpace(draftClueTitle))
            {
                return false;
            }

            return selectedClueType == PalaceClueType.Image
                ? !string.IsNullOrWhiteSpace(draftClueAssetPath)
                : !string.IsNullOrWhiteSpace(draftClueBody);
        }

        private void DrawCluePreviewOverlay()
        {
            var backgroundColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = backgroundColor;

            var panelWidth = Mathf.Min(Screen.width - 120f, 960f);
            var panelHeight = Mathf.Min(Screen.height - 120f, 700f);
            var panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
            GUI.Box(panelRect, string.Empty);
            DrawSolidRect(new Rect(panelRect.x + 12f, panelRect.y + 12f, panelRect.width - 24f, panelRect.height - 24f), new Color(0.95f, 0.96f, 0.97f, 0.99f));

            var contentRect = new Rect(panelRect.x + 24f, panelRect.y + 24f, panelRect.width - 48f, panelRect.height - 48f);
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.08f, 0.1f, 0.12f);
            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 36f), draftClueTitle, titleStyle);

            if (selectedClueType == PalaceClueType.Image)
            {
                DrawImagePreview(contentRect);
            }
            else
            {
                DrawNotePreview(contentRect);
            }

            if (GUI.Button(new Rect(panelRect.x + panelRect.width - 150f, panelRect.y + panelRect.height - 54f, 126f, 32f), "Close Preview"))
            {
                isPreviewOpen = false;
            }
        }

        private void DrawImagePreview(Rect contentRect)
        {
            var controlsRect = new Rect(contentRect.x, contentRect.y + 44f, contentRect.width, 32f);
            if (GUI.Button(new Rect(controlsRect.x, controlsRect.y, 88f, controlsRect.height), "Zoom Out"))
            {
                previewImageZoom = Mathf.Clamp(previewImageZoom - 0.2f, 0.4f, 3f);
            }

            if (GUI.Button(new Rect(controlsRect.x + 96f, controlsRect.y, 88f, controlsRect.height), "Reset"))
            {
                previewImageZoom = 1f;
                previewImagePan = Vector2.zero;
            }

            if (GUI.Button(new Rect(controlsRect.x + 192f, controlsRect.y, 88f, controlsRect.height), "Zoom In"))
            {
                previewImageZoom = Mathf.Clamp(previewImageZoom + 0.35f, 0.4f, 8f);
            }

            var zoomStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            zoomStyle.normal.textColor = new Color(0.14f, 0.16f, 0.2f);
            GUI.Label(new Rect(controlsRect.x + 290f, controlsRect.y, contentRect.width - 290f, controlsRect.height), "Scroll: up/down pan. A/D or Left/Right: horizontal pan.", zoomStyle);

            var imageCanvasRect = new Rect(contentRect.x, contentRect.y + 88f, contentRect.width, contentRect.height - 168f);
            DrawSolidRect(imageCanvasRect, new Color(0.14f, 0.16f, 0.18f, 1f));

            var texture = GetPreviewTexture(draftClueAssetPath);
            if (texture != null)
            {
                var fitScale = Mathf.Min(imageCanvasRect.width / texture.width, imageCanvasRect.height / texture.height);
                var drawWidth = texture.width * fitScale * previewImageZoom;
                var drawHeight = texture.height * fitScale * previewImageZoom;

                GUI.BeginGroup(imageCanvasRect);
                var imageRect = new Rect(
                    (imageCanvasRect.width - drawWidth) * 0.5f + previewImagePan.x,
                    (imageCanvasRect.height - drawHeight) * 0.5f + previewImagePan.y,
                    drawWidth,
                    drawHeight);
                GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
                GUI.EndGroup();
            }
            else
            {
                var fallbackStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 18
                };
                fallbackStyle.normal.textColor = Color.white;
                GUI.Label(imageCanvasRect, "Image preview unavailable for this path.", fallbackStyle);
            }

            var pathRect = new Rect(contentRect.x, contentRect.yMax - 64f, contentRect.width, 52f);
            DrawSolidRect(pathRect, new Color(0.84f, 0.87f, 0.9f, 1f));
            var pathStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            pathStyle.normal.textColor = new Color(0.1f, 0.12f, 0.14f);
            GUI.Label(new Rect(pathRect.x + 10f, pathRect.y + 8f, pathRect.width - 20f, pathRect.height - 16f), draftClueAssetPath, pathStyle);
        }

        private void DrawNotePreview(Rect contentRect)
        {
            var bodyRect = new Rect(contentRect.x, contentRect.y + 44f, contentRect.width, contentRect.height - 96f);
            DrawSolidRect(bodyRect, new Color(0.96f, 0.95f, 0.88f, 1f));

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = Mathf.RoundToInt(22f * draftClueTextScale)
            };
            bodyStyle.normal.textColor = new Color(0.08f, 0.09f, 0.1f);
            bodyStyle.fontStyle = draftClueTextStyle switch
            {
                PalaceClueTextStyle.Bold => FontStyle.Bold,
                PalaceClueTextStyle.Italic => FontStyle.Italic,
                _ => FontStyle.Normal
            };

            GUI.Label(new Rect(bodyRect.x + 18f, bodyRect.y + 18f, bodyRect.width - 36f, bodyRect.height - 36f), draftClueBody, bodyStyle);
        }

        private void HandlePreviewScrollInputFromDevices()
        {
            var scrollDelta = Mouse.current!.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) < 0.01f)
            {
                return;
            }

            var step = Mathf.Sign(scrollDelta) * 40f;
            var shiftPressed = Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            if (shiftPressed)
            {
                previewImagePan.x -= step;
            }
            else
            {
                previewImagePan.y += step;
            }
        }

        private void HandlePreviewHorizontalPanKeys()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            const float horizontalStep = 28f;
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                previewImagePan.x -= horizontalStep;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                previewImagePan.x += horizontalStep;
            }
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private Texture2D? GetPreviewTexture(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
            {
                return null;
            }

            if (previewTexture != null && previewTexturePath == assetPath)
            {
                return previewTexture;
            }

            var imageBytes = File.ReadAllBytes(assetPath);
            if (imageBytes.Length == 0)
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(imageBytes))
            {
                Destroy(texture);
                return null;
            }

            previewTexture = texture;
            previewTexturePath = assetPath;
            return previewTexture;
        }

        private void ApplyAndSaveCurrentDraft()
        {
            var currentRoom = roomTracker.CurrentRoom;
            if (currentRoom == null)
            {
                return;
            }

            if (currentRoom.RoomId == EntryHallRoomId)
            {
                if (!PalaceSaveManager.IsPalaceNameAvailable(draftPalaceName, PalaceSessionState.CurrentPalaceId))
                {
                    editorStatus = "Choose a unique palace name.";
                    return;
                }

                PalaceSessionState.SetCurrentPalaceName(draftPalaceName);
            }

            if (!PalaceSessionState.IsRoomDisplayNameAvailable(draftName, currentRoom.RoomId))
            {
                editorStatus = "Choose a unique room name.";
                return;
            }

            currentRoom.SetDisplayName(draftName);
            currentRoom.ApplyAccentColor(draftColor);
            roomTracker.SetCurrentRoom(currentRoom, currentRoom.RoomDisplayName);
            roomTracker.RefreshHud();
            PalaceSaveManager.SaveCurrentState();
            editorStatus = "Changes saved.";
            RuntimeEventLogger.LogEvent("room_editor.room", $"Saved room changes for {currentRoom.RoomId} with display name \"{draftName}\".");
        }

        private void ApplyDraftPreview()
        {
            var currentRoom = roomTracker.CurrentRoom;
            if (currentRoom == null)
            {
                return;
            }

            currentRoom.ApplyAccentColor(draftColor);
            roomTracker.SetCurrentRoom(currentRoom, currentRoom.RoomDisplayName);
            roomTracker.RefreshHud();
        }

        private void NudgeSelectedClue(Vector3 localDelta)
        {
            if (string.IsNullOrWhiteSpace(selectedClueId) || roomTracker.CurrentRoom == null)
            {
                return;
            }

            if (palaceBootstrap.TryNudgeClue(roomTracker.CurrentRoom, selectedClueId, localDelta))
            {
                PalaceSaveManager.SaveCurrentState();
                editorStatus = "Clue moved.";
                RuntimeEventLogger.LogEvent("room_editor.clue", $"Nudged clue {selectedClueId} by {localDelta}.");
            }
        }

        private void MoveSelectedClueToWall(string wallId)
        {
            if (string.IsNullOrWhiteSpace(selectedClueId) || roomTracker.CurrentRoom == null)
            {
                return;
            }

            if (palaceBootstrap.TryMoveClueToWall(roomTracker.CurrentRoom, selectedClueId, wallId))
            {
                PalaceSaveManager.SaveCurrentState();
                editorStatus = "Clue moved to wall.";
                RuntimeEventLogger.LogEvent("room_editor.clue", $"Moved clue {selectedClueId} to wall {wallId}.");
            }
        }

        private void ScaleSelectedClue(float scaleDelta)
        {
            if (string.IsNullOrWhiteSpace(selectedClueId))
            {
                return;
            }

            if (palaceBootstrap.TryScaleClue(selectedClueId, scaleDelta))
            {
                PalaceSaveManager.SaveCurrentState();
                editorStatus = scaleDelta > 0f ? "Clue enlarged." : "Clue reduced.";
                RuntimeEventLogger.LogEvent("room_editor.clue", $"Scaled clue {selectedClueId} by delta {scaleDelta:F2}.");
            }
        }
    }
}
