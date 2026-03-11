using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public static class PalaceSessionState
    {
        public readonly struct RoomRecord
        {
            public RoomRecord(string roomId, string displayName, Color accentColor)
            {
                RoomId = roomId;
                DisplayName = displayName;
                AccentColor = accentColor;
            }

            public string RoomId { get; }
            public string DisplayName { get; }
            public Color AccentColor { get; }
        }

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

        public static string CurrentPalaceId { get; private set; } = string.Empty;
        public static string CurrentPalaceName { get; private set; } = "Untitled Palace";
        public static bool HasWestBranchRoom { get; set; }

        public static void SetActivePalace(string palaceId, string palaceName)
        {
            CurrentPalaceId = palaceId;
            CurrentPalaceName = palaceName;
        }

        public static void SetCurrentPalaceName(string palaceName)
        {
            CurrentPalaceName = string.IsNullOrWhiteSpace(palaceName) ? "Untitled Palace" : palaceName.Trim();
        }

        public static void SetRoom(string roomId, string displayName, Color accentColor)
        {
            Rooms[roomId] = new RoomSnapshot(displayName, accentColor);
        }

        public static bool TryGetRoom(string roomId, out RoomSnapshot snapshot)
        {
            return Rooms.TryGetValue(roomId, out snapshot);
        }

        public static bool IsRoomDisplayNameAvailable(string displayName, string excludedRoomId = "")
        {
            var normalizedName = NormalizeName(displayName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            foreach (var room in Rooms)
            {
                if (room.Key == excludedRoomId)
                {
                    continue;
                }

                if (NormalizeName(room.Value.DisplayName) == normalizedName)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<RoomRecord> GetAllRooms()
        {
            foreach (var pair in Rooms)
            {
                yield return new RoomRecord(pair.Key, pair.Value.DisplayName, pair.Value.AccentColor);
            }
        }

        public static void Clear()
        {
            Rooms.Clear();
            CurrentPalaceId = string.Empty;
            CurrentPalaceName = "Untitled Palace";
            HasWestBranchRoom = false;
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
