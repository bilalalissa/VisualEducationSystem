#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public sealed class RoomInstance : MonoBehaviour
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string roomDisplayName = "Room";
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] private string parentRoomId = string.Empty;
        [SerializeField] private Vector3 layoutCenter = Vector3.zero;
        [SerializeField] private Vector3 roomSize = Vector3.zero;
        [SerializeField] private Vector3 spawnPoint = Vector3.zero;
        [SerializeField] private bool openWest;
        [SerializeField] private bool openEast;
        [SerializeField] private bool openNorth;
        [SerializeField] private bool openSouth;

        private readonly List<Renderer> roomRenderers = new();
        private TextMesh? entranceText;
        private Renderer? entrancePlateRenderer;

        public string RoomId => roomId;
        public string RoomDisplayName => roomDisplayName;
        public Color AccentColor => accentColor;
        public string ParentRoomId => parentRoomId;
        public bool IsSubRoom => !string.IsNullOrWhiteSpace(parentRoomId);
        public Vector3 LayoutCenter => layoutCenter;
        public Vector3 RoomSize => roomSize;
        public Vector3 SpawnPoint => spawnPoint;
        public bool OpenWest => openWest;
        public bool OpenEast => openEast;
        public bool OpenNorth => openNorth;
        public bool OpenSouth => openSouth;

        public void Initialize(
            string id,
            string displayName,
            Color color,
            string parentId,
            Vector3 roomCenter,
            Vector3 size,
            Vector3 roomSpawnPoint,
            bool westOpen,
            bool eastOpen,
            bool northOpen,
            bool southOpen,
            IEnumerable<Renderer> renderers)
        {
            roomId = id;
            roomDisplayName = displayName;
            accentColor = color;
            parentRoomId = parentId;
            layoutCenter = roomCenter;
            roomSize = size;
            spawnPoint = roomSpawnPoint;
            openWest = westOpen;
            openEast = eastOpen;
            openNorth = northOpen;
            openSouth = southOpen;
            roomRenderers.Clear();
            roomRenderers.AddRange(renderers);
            ApplyAccentColor(color);
            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor, parentRoomId);
        }

        public void SetWallOpenings(bool westOpen, bool eastOpen, bool northOpen, bool southOpen)
        {
            openWest = westOpen;
            openEast = eastOpen;
            openNorth = northOpen;
            openSouth = southOpen;
        }

        public void AttachEntranceSign(TextMesh labelText, Renderer plateRenderer)
        {
            entranceText = labelText;
            entrancePlateRenderer = plateRenderer;
            entranceText.text = roomDisplayName;
            entrancePlateRenderer.material.color = accentColor;
        }

        public void RegisterRenderers(IEnumerable<Renderer> renderers)
        {
            roomRenderers.AddRange(renderers);
            ApplyAccentColor(accentColor);
        }

        public void SetDisplayName(string displayName)
        {
            roomDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Room" : displayName.Trim();

            if (entranceText != null)
            {
                entranceText.text = roomDisplayName;
            }

            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor, parentRoomId);
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

            PalaceSessionState.SetRoom(roomId, roomDisplayName, accentColor, parentRoomId);
        }
    }
}
