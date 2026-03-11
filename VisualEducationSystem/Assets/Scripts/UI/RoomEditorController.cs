using UnityEngine;
using UnityEngine.InputSystem;
using VisualEducationSystem.Player;
using VisualEducationSystem.Rooms;
using VisualEducationSystem.Save;

namespace VisualEducationSystem.UI
{
    public sealed class RoomEditorController : MonoBehaviour
    {
        private const string EntryHallRoomId = "EntryHall";

        [SerializeField] private PlayerRoomTracker roomTracker = null!;
        [SerializeField] private SimpleFirstPersonController playerController = null!;
        [SerializeField] private PrototypePalaceBootstrap palaceBootstrap = null!;

        private bool isOpen;
        private string draftPalaceName = string.Empty;
        private string draftName = string.Empty;
        private Color draftColor = Color.white;
        private string editorStatus = string.Empty;
        private Vector2 scrollPosition;
        public static bool IsAnyEditorOpen { get; private set; }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseEditor();
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
                    palaceBootstrap.TryAddRoomFromEntryHall();
                    PalaceSaveManager.SaveCurrentState();
                    editorStatus = string.Empty;
                }

                GUI.enabled = true;
                GUILayout.Label(canAddRoom ? "Adds a new branch room from the main hall." : "No more branch slots available in the main hall.");
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
            editorStatus = string.Empty;
            playerController.SetInputEnabled(false);
        }

        private void CloseEditor()
        {
            isOpen = false;
            IsAnyEditorOpen = false;
            editorStatus = string.Empty;
            playerController.SetInputEnabled(true);
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
    }
}
