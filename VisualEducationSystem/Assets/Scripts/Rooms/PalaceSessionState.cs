using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    public enum PalaceClueType
    {
        Note = 0,
        Image = 1,
        File = 2,
        Video = 3
    }

    public enum PalaceClueTextStyle
    {
        Normal = 0,
        Bold = 1,
        Italic = 2
    }

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

        public readonly struct ClueRecord
        {
            public ClueRecord(
                string clueId,
                string roomId,
                PalaceClueType clueType,
                string title,
                string bodyText,
                string assetPath,
                float textScale,
                PalaceClueTextStyle textStyle,
                Vector3 localPosition,
                Vector3 localEulerAngles,
                Vector3 localScale)
            {
                ClueId = clueId;
                RoomId = roomId;
                ClueType = clueType;
                Title = title;
                BodyText = bodyText;
                AssetPath = assetPath;
                TextScale = textScale;
                TextStyle = textStyle;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
                LocalScale = localScale;
            }

            public string ClueId { get; }
            public string RoomId { get; }
            public PalaceClueType ClueType { get; }
            public string Title { get; }
            public string BodyText { get; }
            public string AssetPath { get; }
            public float TextScale { get; }
            public PalaceClueTextStyle TextStyle { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 LocalEulerAngles { get; }
            public Vector3 LocalScale { get; }
        }

        public readonly struct ClueSnapshot
        {
            public ClueSnapshot(
                string roomId,
                PalaceClueType clueType,
                string title,
                string bodyText,
                string assetPath,
                float textScale,
                PalaceClueTextStyle textStyle,
                Vector3 localPosition,
                Vector3 localEulerAngles,
                Vector3 localScale)
            {
                RoomId = roomId;
                ClueType = clueType;
                Title = title;
                BodyText = bodyText;
                AssetPath = assetPath;
                TextScale = textScale;
                TextStyle = textStyle;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
                LocalScale = localScale;
            }

            public string RoomId { get; }
            public PalaceClueType ClueType { get; }
            public string Title { get; }
            public string BodyText { get; }
            public string AssetPath { get; }
            public float TextScale { get; }
            public PalaceClueTextStyle TextStyle { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 LocalEulerAngles { get; }
            public Vector3 LocalScale { get; }
        }

        private static readonly Dictionary<string, RoomSnapshot> Rooms = new();
        private static readonly Dictionary<string, ClueSnapshot> Clues = new();

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

        public static void SetClue(
            string clueId,
            string roomId,
            PalaceClueType clueType,
            string title,
            string bodyText,
            string assetPath,
            float textScale,
            PalaceClueTextStyle textStyle,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            Clues[clueId] = new ClueSnapshot(
                roomId,
                clueType,
                string.IsNullOrWhiteSpace(title) ? "Untitled Clue" : title.Trim(),
                string.IsNullOrWhiteSpace(bodyText) ? string.Empty : bodyText.Trim(),
                string.IsNullOrWhiteSpace(assetPath) ? string.Empty : assetPath.Trim(),
                Mathf.Clamp(textScale, 0.75f, 1.35f),
                textStyle,
                localPosition,
                localEulerAngles,
                localScale);
        }

        public static bool TryGetClue(string clueId, out ClueSnapshot snapshot)
        {
            return Clues.TryGetValue(clueId, out snapshot);
        }

        public static bool RemoveClue(string clueId)
        {
            return Clues.Remove(clueId);
        }

        public static IEnumerable<ClueRecord> GetAllClues()
        {
            foreach (var pair in Clues)
            {
                yield return new ClueRecord(
                    pair.Key,
                    pair.Value.RoomId,
                    pair.Value.ClueType,
                    pair.Value.Title,
                    pair.Value.BodyText,
                    pair.Value.AssetPath,
                    pair.Value.TextScale,
                    pair.Value.TextStyle,
                    pair.Value.LocalPosition,
                    pair.Value.LocalEulerAngles,
                    pair.Value.LocalScale);
            }
        }

        public static IEnumerable<ClueRecord> GetCluesForRoom(string roomId)
        {
            foreach (var pair in Clues)
            {
                if (pair.Value.RoomId != roomId)
                {
                    continue;
                }

                yield return new ClueRecord(
                    pair.Key,
                    pair.Value.RoomId,
                    pair.Value.ClueType,
                    pair.Value.Title,
                    pair.Value.BodyText,
                    pair.Value.AssetPath,
                    pair.Value.TextScale,
                    pair.Value.TextStyle,
                    pair.Value.LocalPosition,
                    pair.Value.LocalEulerAngles,
                    pair.Value.LocalScale);
            }
        }

        public static int GetClueCount(string roomId = "")
        {
            var count = 0;
            foreach (var clue in Clues)
            {
                if (string.IsNullOrWhiteSpace(roomId) || clue.Value.RoomId == roomId)
                {
                    count++;
                }
            }

            return count;
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
            Clues.Clear();
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
