using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public sealed class RoomInstance : MonoBehaviour
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string roomDisplayName = "Room";
        [SerializeField] private Color accentColor = Color.white;

        private readonly List<Renderer> roomRenderers = new();
        private TextMesh? entranceText;
        private Renderer? entrancePlateRenderer;

        public string RoomId => roomId;
        public string RoomDisplayName => roomDisplayName;
        public Color AccentColor => accentColor;

        public void Initialize(string id, string displayName, Color color, IEnumerable<Renderer> renderers)
        {
            roomId = id;
            roomDisplayName = displayName;
            accentColor = color;
            roomRenderers.Clear();
            roomRenderers.AddRange(renderers);
            ApplyAccentColor(color);
            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor);
        }

        public void AttachEntranceSign(TextMesh labelText, Renderer plateRenderer)
        {
            entranceText = labelText;
            entrancePlateRenderer = plateRenderer;
            entranceText.text = roomDisplayName;
            entrancePlateRenderer.material.color = accentColor;
        }

        public void SetDisplayName(string displayName)
        {
            roomDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Room" : displayName.Trim();

            if (entranceText != null)
            {
                entranceText.text = roomDisplayName;
            }

            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor);
        }

        public void ApplyAccentColor(Color color)
        {
            accentColor = color;

            foreach (var renderer in roomRenderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = accentColor;
                }
            }

            if (entrancePlateRenderer != null)
            {
                entrancePlateRenderer.material.color = accentColor;
            }

            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor);
        }
    }
}
