using UnityEngine;

namespace VisualEducationSystem.UI
{
    public sealed class CurrentRoomHUD : MonoBehaviour
    {
        private string currentRoomName = "Entry Hall";
        private readonly Rect labelRect = new(20f, 20f, 320f, 48f);
        private readonly Rect helpRect = new(20f, 76f, 460f, 72f);

        public void SetCurrentRoom(string roomName)
        {
            currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "Unknown Room" : roomName;
        }

        private void OnGUI()
        {
            GUI.Box(labelRect, $"Current Room: {currentRoomName}");
            GUI.Box(helpRect, "Move with WASD, look with mouse, press Esc for menu.\nPress E to edit the current room's name and color.\nWalk through the wall openings to move from Entry Hall to Room 01, Room 02, and Room 03.");
        }
    }
}
