using UnityEngine;

namespace VisualEducationSystem.UI
{
    public sealed class CurrentRoomHUD : MonoBehaviour
    {
        private string currentPalaceName = "Untitled Palace";
        private string currentRoomName = "Entry Hall";
        private readonly Rect palaceRect = new(20f, 20f, 360f, 48f);
        private readonly Rect roomRect = new(20f, 76f, 320f, 48f);
        private readonly Rect helpRect = new(20f, 132f, 500f, 88f);

        public void SetCurrentPalace(string palaceName)
        {
            currentPalaceName = string.IsNullOrWhiteSpace(palaceName) ? "Untitled Palace" : palaceName;
        }

        public void SetCurrentRoom(string roomName)
        {
            currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "Unknown Room" : roomName;
        }

        private void OnGUI()
        {
            GUI.Box(palaceRect, $"Current Palace: {currentPalaceName}");
            GUI.Box(roomRect, $"Current Room: {currentRoomName}");
            GUI.Box(helpRect, "Move with WASD, look with mouse, press Esc for menu.\nPress E to edit the current room's name and color.\nFrom Entry Hall, you can also rename the palace and add one new branch room.");
        }
    }
}
