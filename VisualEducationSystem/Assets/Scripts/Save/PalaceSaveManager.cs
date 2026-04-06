using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VisualEducationSystem.Rooms;

namespace VisualEducationSystem.Save
{
    public static class PalaceSaveManager
    {
        [Serializable]
        public readonly struct PalaceSummary
        {
            public PalaceSummary(string palaceId, string displayName)
            {
                PalaceId = palaceId;
                DisplayName = displayName;
            }

            public string PalaceId { get; }
            public string DisplayName { get; }
        }

        [Serializable]
        private sealed class PalaceSaveData
        {
            public string palaceId = string.Empty;
            public string palaceName = string.Empty;
            public bool hasWestBranchRoom;
            public List<RoomSaveData> rooms = new();
            public List<ClueSaveData> clues = new();
        }

        [Serializable]
        private sealed class PalaceIndexData
        {
            public List<PalaceIndexEntry> palaces = new();
        }

        [Serializable]
        private sealed class PalaceIndexEntry
        {
            public string palaceId = string.Empty;
            public string displayName = string.Empty;
        }

        [Serializable]
        private sealed class RoomSaveData
        {
            public string roomId = string.Empty;
            public string displayName = string.Empty;
            public string parentRoomId = string.Empty;
            public float colorR;
            public float colorG;
            public float colorB;
            public float colorA;
        }

        [Serializable]
        private sealed class ClueSaveData
        {
            public string clueId = string.Empty;
            public string roomId = string.Empty;
            public int clueType;
            public string title = string.Empty;
            public string bodyText = string.Empty;
            public string assetPath = string.Empty;
            public float textScale = 1f;
            public int textStyle;
            public float localPositionX;
            public float localPositionY;
            public float localPositionZ;
            public float localEulerX;
            public float localEulerY;
            public float localEulerZ;
            public float localScaleX = 1f;
            public float localScaleY = 1f;
            public float localScaleZ = 1f;
        }

        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Palaces");
        private static string IndexPath => Path.Combine(SaveDirectory, "palace-index.json");

        public static IReadOnlyList<PalaceSummary> GetPalaceSummaries()
        {
            EnsureSaveDirectory();
            if (!File.Exists(IndexPath))
            {
                return Array.Empty<PalaceSummary>();
            }

            var json = File.ReadAllText(IndexPath);
            var index = JsonUtility.FromJson<PalaceIndexData>(json) ?? new PalaceIndexData();
            var results = new List<PalaceSummary>(index.palaces.Count);
            foreach (var palace in index.palaces)
            {
                results.Add(new PalaceSummary(palace.palaceId, palace.displayName));
            }

            return results;
        }

        public static bool IsPalaceNameAvailable(string palaceName, string excludedPalaceId = "")
        {
            var normalizedName = NormalizeName(palaceName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            foreach (var palace in GetPalaceSummaries())
            {
                if (palace.PalaceId == excludedPalaceId)
                {
                    continue;
                }

                if (NormalizeName(palace.DisplayName) == normalizedName)
                {
                    return false;
                }
            }

            return true;
        }

        public static void CreateNewPalace(string palaceName)
        {
            EnsureSaveDirectory();
            var trimmedName = string.IsNullOrWhiteSpace(palaceName) ? "Untitled Palace" : palaceName.Trim();
            var palaceId = $"{SanitizeFileName(trimmedName)}-{Guid.NewGuid():N}".ToLowerInvariant();

            PalaceSessionState.Clear();
            PalaceSessionState.SetActivePalace(palaceId, trimmedName);
            SaveCurrentState();
            UpsertIndexEntry(palaceId, trimmedName);
        }

        public static void SaveCurrentState()
        {
            EnsureSaveDirectory();
            if (string.IsNullOrWhiteSpace(PalaceSessionState.CurrentPalaceId))
            {
                PalaceSessionState.SetActivePalace("default-palace", "Default Palace");
            }

            var data = new PalaceSaveData
            {
                palaceId = PalaceSessionState.CurrentPalaceId,
                palaceName = PalaceSessionState.CurrentPalaceName,
                hasWestBranchRoom = PalaceSessionState.HasWestBranchRoom
            };

            foreach (var room in PalaceSessionState.GetAllRooms())
            {
                data.rooms.Add(new RoomSaveData
                {
                    roomId = room.RoomId,
                    displayName = room.DisplayName,
                    parentRoomId = room.ParentRoomId,
                    colorR = room.AccentColor.r,
                    colorG = room.AccentColor.g,
                    colorB = room.AccentColor.b,
                    colorA = room.AccentColor.a
                });
            }

            foreach (var clue in PalaceSessionState.GetAllClues())
            {
                data.clues.Add(new ClueSaveData
                {
                    clueId = clue.ClueId,
                    roomId = clue.RoomId,
                    clueType = (int)clue.ClueType,
                    title = clue.Title,
                    bodyText = clue.BodyText,
                    assetPath = clue.AssetPath,
                    textScale = clue.TextScale,
                    textStyle = (int)clue.TextStyle,
                    localPositionX = clue.LocalPosition.x,
                    localPositionY = clue.LocalPosition.y,
                    localPositionZ = clue.LocalPosition.z,
                    localEulerX = clue.LocalEulerAngles.x,
                    localEulerY = clue.LocalEulerAngles.y,
                    localEulerZ = clue.LocalEulerAngles.z,
                    localScaleX = clue.LocalScale.x,
                    localScaleY = clue.LocalScale.y,
                    localScaleZ = clue.LocalScale.z
                });
            }

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPalacePath(PalaceSessionState.CurrentPalaceId), json);
            UpsertIndexEntry(PalaceSessionState.CurrentPalaceId, PalaceSessionState.CurrentPalaceName);
            Debug.Log($"Palace saved: {PalaceSessionState.CurrentPalaceName}");
        }

        public static bool LoadPalaceIntoSession(string palaceId)
        {
            var savePath = GetPalacePath(palaceId);
            if (!File.Exists(savePath))
            {
                return false;
            }

            var json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<PalaceSaveData>(json);
            if (data == null)
            {
                return false;
            }

            PalaceSessionState.Clear();
            PalaceSessionState.SetActivePalace(data.palaceId, data.palaceName);
            PalaceSessionState.HasWestBranchRoom = data.hasWestBranchRoom;

            foreach (var room in data.rooms)
            {
                var color = new Color(room.colorR, room.colorG, room.colorB, room.colorA);
                PalaceSessionState.SetRoom(room.roomId, room.displayName, color, room.parentRoomId);
            }

            foreach (var clue in data.clues)
            {
                PalaceSessionState.SetClue(
                    clue.clueId,
                    clue.roomId,
                    (PalaceClueType)clue.clueType,
                    clue.title,
                    clue.bodyText,
                    clue.assetPath,
                    clue.textScale,
                    (PalaceClueTextStyle)clue.textStyle,
                    new Vector3(clue.localPositionX, clue.localPositionY, clue.localPositionZ),
                    new Vector3(clue.localEulerX, clue.localEulerY, clue.localEulerZ),
                    new Vector3(clue.localScaleX, clue.localScaleY, clue.localScaleZ));
            }

            return true;
        }

        public static bool DeletePalace(string palaceId)
        {
            if (string.IsNullOrWhiteSpace(palaceId))
            {
                return false;
            }

            EnsureSaveDirectory();
            var deleted = false;
            var palacePath = GetPalacePath(palaceId);
            if (File.Exists(palacePath))
            {
                File.Delete(palacePath);
                deleted = true;
            }

            PalaceIndexData index;
            if (File.Exists(IndexPath))
            {
                index = JsonUtility.FromJson<PalaceIndexData>(File.ReadAllText(IndexPath)) ?? new PalaceIndexData();
            }
            else
            {
                index = new PalaceIndexData();
            }

            var removedFromIndex = index.palaces.RemoveAll(palace => palace.palaceId == palaceId) > 0;
            if (removedFromIndex)
            {
                File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
            }

            if (PalaceSessionState.CurrentPalaceId == palaceId)
            {
                PalaceSessionState.Clear();
            }

            return deleted || removedFromIndex;
        }

        public static void ClearSessionOnly()
        {
            PalaceSessionState.Clear();
        }

        public static void ClearSessionAndDeleteCurrentPalace()
        {
            if (!string.IsNullOrWhiteSpace(PalaceSessionState.CurrentPalaceId))
            {
                var path = GetPalacePath(PalaceSessionState.CurrentPalaceId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            PalaceSessionState.Clear();
        }

        private static void EnsureSaveDirectory()
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        private static string GetPalacePath(string palaceId)
        {
            return Path.Combine(SaveDirectory, $"{palaceId}.json");
        }

        private static void UpsertIndexEntry(string palaceId, string palaceName)
        {
            EnsureSaveDirectory();
            PalaceIndexData index;
            if (File.Exists(IndexPath))
            {
                index = JsonUtility.FromJson<PalaceIndexData>(File.ReadAllText(IndexPath)) ?? new PalaceIndexData();
            }
            else
            {
                index = new PalaceIndexData();
            }

            var updated = false;
            for (var i = 0; i < index.palaces.Count; i++)
            {
                if (index.palaces[i].palaceId != palaceId)
                {
                    continue;
                }

                index.palaces[i].displayName = palaceName;
                updated = true;
                break;
            }

            if (!updated)
            {
                index.palaces.Add(new PalaceIndexEntry
                {
                    palaceId = palaceId,
                    displayName = palaceName
                });
            }

            File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
        }

        private static string SanitizeFileName(string input)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(invalidChar, '-');
            }

            return input.Replace(' ', '-');
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
