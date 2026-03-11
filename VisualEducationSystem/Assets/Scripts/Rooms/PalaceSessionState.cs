using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public static class PalaceSessionState
    {
        public readonly struct RoomSnapshot
        {
            public RoomSnapshot(string displayName, Color accentColor)
            {
                DisplayName = displayName;
                AccentColor = accentColor;
            }

            public string DisplayName { get; }
            public Color AccentColor { get; }
        }

        private static readonly Dictionary<string, RoomSnapshot> Rooms = new();

        public static bool HasWestBranchRoom { get; set; }

        public static void SetRoom(string roomId, string displayName, Color accentColor)
        {
            Rooms[roomId] = new RoomSnapshot(displayName, accentColor);
        }

        public static bool TryGetRoom(string roomId, out RoomSnapshot snapshot)
        {
            return Rooms.TryGetValue(roomId, out snapshot);
        }
    }
}
