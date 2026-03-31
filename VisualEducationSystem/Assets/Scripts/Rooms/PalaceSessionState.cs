using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public static class PalaceSessionState
    {
        public readonly struct RoomRecord
        {
            public RoomRecord(string roomId, string displayName, Color accentColor, string parentRoomId)
            {
                RoomId = roomId;
                DisplayName = displayName;
                AccentColor = accentColor;
                ParentRoomId = parentRoomId;
            }

            public string RoomId { get; }
            public string DisplayName { get; }
            public Color AccentColor { get; }
            public string ParentRoomId { get; }
            public bool IsSubRoom => !string.IsNullOrWhiteSpace(ParentRoomId);
        }

        public readonly struct RoomSnapshot
        {
            public RoomSnapshot(string displayName, Color accentColor, string parentRoomId)
            {
                DisplayName = displayName;
                AccentColor = accentColor;
                ParentRoomId = parentRoomId;
            }

            public string DisplayName { get; }
            public Color AccentColor { get; }
            public string ParentRoomId { get; }
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

        public static void SetRoom(string roomId, string displayName, Color accentColor, string parentRoomId = "")
        {
            Rooms[roomId] = new RoomSnapshot(displayName, accentColor, parentRoomId);
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
                yield return new RoomRecord(pair.Key, pair.Value.DisplayName, pair.Value.AccentColor, pair.Value.ParentRoomId);
            }
        }

        public static bool HasChildRoom(string parentRoomId)
        {
            if (string.IsNullOrWhiteSpace(parentRoomId))
            {
                return false;
            }

            foreach (var room in Rooms)
            {
                if (room.Value.ParentRoomId == parentRoomId)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetChildRoomCount(string parentRoomId)
        {
            if (string.IsNullOrWhiteSpace(parentRoomId))
            {
                return 0;
            }

            var count = 0;
            foreach (var room in Rooms)
            {
                if (room.Value.ParentRoomId == parentRoomId)
                {
                    count++;
                }
            }

            return count;
        }

        public static List<string> GetChildRoomIds(string parentRoomId)
        {
            var childRoomIds = new List<string>();
            if (string.IsNullOrWhiteSpace(parentRoomId))
            {
                return childRoomIds;
            }

            foreach (var room in Rooms)
            {
                if (room.Value.ParentRoomId == parentRoomId)
                {
                    childRoomIds.Add(room.Key);
                }
            }

            childRoomIds.Sort(System.StringComparer.Ordinal);
            return childRoomIds;
        }

        public static bool TryGetFirstChildRoomId(string parentRoomId, out string childRoomId)
        {
            var childRoomIds = GetChildRoomIds(parentRoomId);
            if (childRoomIds.Count > 0)
            {
                childRoomId = childRoomIds[0];
                return true;
            }

            childRoomId = string.Empty;
            return false;
        }

        public static int GetRoomDepth(string roomId)
        {
            var depth = 0;
            var currentRoomId = roomId;

            while (TryGetRoom(currentRoomId, out var snapshot) && !string.IsNullOrWhiteSpace(snapshot.ParentRoomId))
            {
                depth++;
                currentRoomId = snapshot.ParentRoomId;
            }

            return depth;
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
