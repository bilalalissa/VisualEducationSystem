#nullable enable
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using VisualEducationSystem.Interaction;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Rooms
{
    public sealed class PrototypePalaceBootstrap : MonoBehaviour
    {
        public readonly struct MapRoomInfo
        {
            public MapRoomInfo(string roomId, string displayName, string parentRoomId, Vector3 center, Color accentColor, int depth)
            {
                RoomId = roomId;
                DisplayName = displayName;
                ParentRoomId = parentRoomId;
                Center = center;
                AccentColor = accentColor;
                Depth = depth;
            }

            public string RoomId { get; }
            public string DisplayName { get; }
            public string ParentRoomId { get; }
            public Vector3 Center { get; }
            public Color AccentColor { get; }
            public int Depth { get; }
            public bool IsSubRoom => !string.IsNullOrWhiteSpace(ParentRoomId);
        }

        public readonly struct ClueWallOption
        {
            public ClueWallOption(string id, string label)
            {
                Id = id;
                Label = label;
            }

            public string Id { get; }
            public string Label { get; }
        }

        private const string EntryHallRoomId = "EntryHall";
        private const int MaxChildBranchesPerRoom = 3;
        private const float BranchSpacing = 14f;
        private const float CluePivotHeightOffset = 0.98f;
        private const float ClueWallInset = 0.72f;
        private static readonly Vector3[] CardinalDirections = { Vector3.right, Vector3.forward, Vector3.back, Vector3.left };
        public static PrototypePalaceBootstrap? Instance { get; private set; }
        [SerializeField] private Transform player = null!;
        private PlayerRoomTracker? playerRoomTracker;
        private VisualEducationSystem.Player.SimpleFirstPersonController? playerController;
        private int nextDynamicRoomIndex = 4;
        private bool hasWestBranchRoom;
        private readonly System.Collections.Generic.Dictionary<string, RoomInstance> roomInstances = new();
        private readonly System.Collections.Generic.Dictionary<string, Transform> clueVisualRoots = new();
        public bool CanAddRoomFromEntryHall => !hasWestBranchRoom;

        private enum ClueWall
        {
            North,
            South,
            East,
            West
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            playerRoomTracker = player != null ? player.GetComponent<PlayerRoomTracker>() : null;
            playerController = player != null ? player.GetComponent<VisualEducationSystem.Player.SimpleFirstPersonController>() : null;
            EnsureLight();
            BuildGreybox();
        }

        private void BuildGreybox()
        {
            var entryHall = BuildRoom("EntryHall", "Entry Hall", new Vector3(0f, 1.5f, 0f), new Vector3(12f, 3f, 12f), new Color(0.18f, 0.3f, 0.62f), string.Empty, true, true, true, true);
            var room01 = BuildRoom("Room01Box", "Room 01", new Vector3(14f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.78f, 0.36f, 0.24f), string.Empty, true, false, false, false);
            var room02 = BuildRoom("Room02Box", "Room 02", new Vector3(0f, 1.5f, 14f), new Vector3(8f, 3f, 8f), new Color(0.18f, 0.58f, 0.3f), string.Empty, false, false, false, true);
            var room03 = BuildRoom("Room03Box", "Room 03", new Vector3(0f, 1.5f, -14f), new Vector3(8f, 3f, 8f), new Color(0.6f, 0.24f, 0.64f), string.Empty, false, false, true, false);

            BuildConnector("LinkEntryTo01", new Vector3(8f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            BuildConnector("LinkEntryTo02", new Vector3(0f, 0.1f, 8f), new Vector3(4f, 0.2f, 4f));
            BuildConnector("LinkEntryTo03", new Vector3(0f, 0.1f, -8f), new Vector3(4f, 0.2f, 4f));
            var room01Sign = BuildEntranceLabel("LabelRoom01", "Room 01", new Vector3(4.65f, 1.65f, 0f), Quaternion.Euler(0f, 90f, 0f), new Color(0.78f, 0.36f, 0.24f), IconShape.Circle, true);
            var room02Sign = BuildEntranceLabel("LabelRoom02", "Room 02", new Vector3(0f, 1.65f, 4.65f), Quaternion.identity, new Color(0.18f, 0.58f, 0.3f), IconShape.Triangle, false);
            var room03Sign = BuildEntranceLabel("LabelRoom03", "Room 03", new Vector3(0f, 1.65f, -4.65f), Quaternion.Euler(0f, 180f, 0f), new Color(0.6f, 0.24f, 0.64f), IconShape.Diamond, false);
            room01.AttachEntranceSign(room01Sign.textMesh, room01Sign.plateRenderer);
            room02.AttachEntranceSign(room02Sign.textMesh, room02Sign.plateRenderer);
            room03.AttachEntranceSign(room03Sign.textMesh, room03Sign.plateRenderer);
            BuildRoomFeature(room01);
            BuildRoomFeature(room02);
            BuildRoomFeature(room03);
            BuildEntryHallLandmark();

            hasWestBranchRoom = false;
            if (PalaceSessionState.HasWestBranchRoom)
            {
                TryAddRoomFromEntryHall();
            }

            BuildSavedSubRooms();
            BuildSavedClues();

            SpawnPlayerAtEntryHall();

            playerRoomTracker?.SetCurrentRoom(entryHall, entryHall.RoomDisplayName);
        }

        private void BuildEntryHallLandmark()
        {
            const string landmarkName = "EntryHallLandmark";
            var existing = transform.Find(landmarkName);
            if (existing != null)
            {
                return;
            }

            var root = new GameObject(landmarkName).transform;
            root.SetParent(transform);
            root.position = new Vector3(0f, 0f, 0f);
            root.localScale = new Vector3(0.52f, 0.52f, 0.52f);

            var hash = string.IsNullOrWhiteSpace(PalaceSessionState.CurrentPalaceId)
                ? 0
                : Mathf.Abs(PalaceSessionState.CurrentPalaceId.GetHashCode());

            var palette = new[]
            {
                new Color(0.87f, 0.36f, 0.23f),
                new Color(0.22f, 0.62f, 0.36f),
                new Color(0.22f, 0.48f, 0.82f),
                new Color(0.70f, 0.32f, 0.72f),
                new Color(0.82f, 0.70f, 0.24f),
                new Color(0.20f, 0.74f, 0.74f)
            };

            var primaryColor = palette[hash % palette.Length];
            var secondaryColor = palette[(hash / 7 + 2) % palette.Length];
            var fountainColor = Color.Lerp(primaryColor, Color.white, 0.45f);
            var highlightColor = Color.Lerp(secondaryColor, Color.white, 0.65f);
            var landmarkFamily = hash % 4;
            switch (landmarkFamily)
            {
                case 0:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkBase", new Vector3(0f, 0.12f, 0f), new Vector3(3f, 0.24f, 3f), primaryColor * 0.42f);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkBasinOuter", new Vector3(0f, 0.34f, 0f), new Vector3(2.1f, 0.22f, 2.1f), secondaryColor * 0.9f);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkBasinInner", new Vector3(0f, 0.43f, 0f), new Vector3(1.55f, 0.06f, 1.55f), fountainColor);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkPedestal", new Vector3(0f, 1.02f, 0f), new Vector3(0.52f, 1.1f, 0.52f), primaryColor * 0.9f);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkSpire", new Vector3(0f, 2.15f, 0f), new Vector3(0.16f, 1.15f, 0.16f), highlightColor);
                    CreatePrimitive(root, PrimitiveType.Sphere, "LandmarkCrown", new Vector3(0f, 3.2f, 0f), new Vector3(0.95f, 0.95f, 0.95f), highlightColor);
                    break;
                case 1:
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkBase", new Vector3(0f, 0.15f, 0f), new Vector3(3.1f, 0.3f, 3.1f), secondaryColor * 0.45f);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkCore", new Vector3(0f, 0.85f, 0f), new Vector3(1.2f, 1.2f, 1.2f), primaryColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkTop", new Vector3(0f, 1.95f, 0f), new Vector3(0.8f, 0.8f, 0.8f), highlightColor);
                    CreatePrimitive(root, PrimitiveType.Capsule, "LandmarkNorth", new Vector3(0f, 1.1f, 1.15f), new Vector3(0.28f, 1.25f, 0.28f), fountainColor);
                    CreatePrimitive(root, PrimitiveType.Capsule, "LandmarkSouth", new Vector3(0f, 1.1f, -1.15f), new Vector3(0.28f, 1.25f, 0.28f), fountainColor);
                    CreatePrimitive(root, PrimitiveType.Capsule, "LandmarkEast", new Vector3(1.15f, 1.1f, 0f), new Vector3(0.28f, 1.25f, 0.28f), fountainColor);
                    CreatePrimitive(root, PrimitiveType.Capsule, "LandmarkWest", new Vector3(-1.15f, 1.1f, 0f), new Vector3(0.28f, 1.25f, 0.28f), fountainColor);
                    break;
                case 2:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkRingBase", new Vector3(0f, 0.12f, 0f), new Vector3(3.1f, 0.24f, 3.1f), primaryColor * 0.38f);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkPillarCenter", new Vector3(0f, 1.45f, 0f), new Vector3(0.35f, 1.65f, 0.35f), highlightColor);
                    for (var i = 0; i < 3; i++)
                    {
                        var angle = 120f * i - 30f;
                        var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.35f;
                        CreatePrimitive(root, PrimitiveType.Cylinder, $"LandmarkTriPillar_{i}", new Vector3(offset.x, 1f, offset.z), new Vector3(0.28f, 1.1f, 0.28f), secondaryColor);
                        CreatePrimitive(root, PrimitiveType.Sphere, $"LandmarkTriNode_{i}", new Vector3(offset.x, 2.2f, offset.z), new Vector3(0.55f, 0.55f, 0.55f), primaryColor);
                    }
                    CreatePrimitive(root, PrimitiveType.Capsule, "LandmarkCap", new Vector3(0f, 3.05f, 0f), new Vector3(0.7f, 0.95f, 0.7f), fountainColor);
                    break;
                default:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkBase", new Vector3(0f, 0.12f, 0f), new Vector3(2.9f, 0.24f, 2.9f), primaryColor * 0.35f);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkArchLeft", new Vector3(-0.95f, 1.2f, 0f), new Vector3(0.35f, 2.1f, 0.6f), secondaryColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkArchRight", new Vector3(0.95f, 1.2f, 0f), new Vector3(0.35f, 2.1f, 0.6f), secondaryColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkArchTop", new Vector3(0f, 2.2f, 0f), new Vector3(2.2f, 0.35f, 0.6f), secondaryColor);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "LandmarkCenterColumn", new Vector3(0f, 1.05f, 0f), new Vector3(0.32f, 1.15f, 0.32f), primaryColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkGem", new Vector3(0f, 1.45f, 0f), new Vector3(0.72f, 0.72f, 0.72f), highlightColor).transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkBenchNorth", new Vector3(0f, 0.45f, 1.15f), new Vector3(1.35f, 0.2f, 0.3f), fountainColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "LandmarkBenchSouth", new Vector3(0f, 0.45f, -1.15f), new Vector3(1.35f, 0.2f, 0.3f), fountainColor);
                    break;
            }

            var palaceNameAnchor = new GameObject("PalaceNameAnchor").transform;
            palaceNameAnchor.SetParent(root);
            palaceNameAnchor.localPosition = new Vector3(0f, 4.2f, 0f);
            palaceNameAnchor.gameObject.AddComponent<FaceCameraYawOnly>();

            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = "PalaceNamePlaque";
            plaque.transform.SetParent(palaceNameAnchor);
            plaque.transform.localPosition = Vector3.zero;
            plaque.transform.localScale = new Vector3(2.2f, 0.55f, 0.08f);
            plaque.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.06f, 0.08f, 0.14f));
            Destroy(plaque.GetComponent<Collider>());

            var plaqueTextObject = new GameObject("PalaceNameText");
            plaqueTextObject.transform.SetParent(palaceNameAnchor);
            plaqueTextObject.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            plaqueTextObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var plaqueText = plaqueTextObject.AddComponent<TextMesh>();
            plaqueText.text = PalaceSessionState.CurrentPalaceName;
            plaqueText.fontSize = 34;
            plaqueText.characterSize = 0.07f;
            plaqueText.anchor = TextAnchor.MiddleCenter;
            plaqueText.alignment = TextAlignment.Center;
            plaqueText.color = Color.white;
        }

        public void TryAddRoomFromEntryHall()
        {
            if (!CanAddRoomFromEntryHall)
            {
                return;
            }

            var roomName = $"Room {nextDynamicRoomIndex:00}";
            while (!PalaceSessionState.IsRoomDisplayNameAvailable(roomName))
            {
                nextDynamicRoomIndex++;
                roomName = $"Room {nextDynamicRoomIndex:00}";
            }

            var dynamicRoom = BuildRoom("Room04Box", roomName, new Vector3(-14f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.76f, 0.66f, 0.24f), string.Empty, false, true, false, false);
            BuildConnector("LinkEntryTo04", new Vector3(-8f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            var room04Sign = BuildEntranceLabel("LabelRoom04", roomName, new Vector3(-4.65f, 1.65f, 0f), Quaternion.Euler(0f, -90f, 0f), new Color(0.76f, 0.66f, 0.24f), IconShape.Square, true);
            dynamicRoom.AttachEntranceSign(room04Sign.textMesh, room04Sign.plateRenderer);
            BuildRoomFeature(dynamicRoom);
            hasWestBranchRoom = true;
            PalaceSessionState.HasWestBranchRoom = true;
            nextDynamicRoomIndex++;
        }

        public bool CanAddSubRoom(RoomInstance? room)
        {
            return room != null
                && room.RoomId != EntryHallRoomId
                && HasAvailableChildSlot(room);
        }

        public bool HasSubRoom(RoomInstance? room)
        {
            return room != null && PalaceSessionState.HasChildRoom(room.RoomId);
        }

        public bool CanNavigateToParent(RoomInstance? room)
        {
            return room != null && room.IsSubRoom && roomInstances.ContainsKey(room.ParentRoomId);
        }

        public void TryAddSubRoom(RoomInstance? parentRoom)
        {
            if (!CanAddSubRoom(parentRoom))
            {
                return;
            }

            var parent = parentRoom!;
            var roomId = BuildUniqueSubRoomId(parent.RoomId);
            var roomName = BuildUniqueSubRoomName(parent.RoomDisplayName);

            var roomColor = Color.Lerp(parent.AccentColor, Color.white, 0.18f);
            BuildSubRoom(parent, roomId, roomName, roomColor);
        }

        public void NavigateToChildRoom(RoomInstance? parentRoom)
        {
            if (parentRoom == null)
            {
                return;
            }

            if (!PalaceSessionState.TryGetFirstChildRoomId(parentRoom.RoomId, out var childRoomId))
            {
                return;
            }

            if (!roomInstances.TryGetValue(childRoomId, out var childRoom))
            {
                return;
            }

            TeleportPlayerToRoom(childRoom);
        }

        public System.Collections.Generic.IReadOnlyList<RoomInstance> GetChildRooms(RoomInstance? parentRoom)
        {
            var childRooms = new System.Collections.Generic.List<RoomInstance>();
            if (parentRoom == null)
            {
                return childRooms;
            }

            foreach (var childRoomId in PalaceSessionState.GetChildRoomIds(parentRoom.RoomId))
            {
                if (roomInstances.TryGetValue(childRoomId, out var childRoom))
                {
                    childRooms.Add(childRoom);
                }
            }

            return childRooms;
        }

        public System.Collections.Generic.IReadOnlyList<PalaceSessionState.ClueRecord> GetCluesForRoom(RoomInstance? room)
        {
            var clues = new System.Collections.Generic.List<PalaceSessionState.ClueRecord>();
            if (room == null)
            {
                return clues;
            }

            foreach (var clue in PalaceSessionState.GetCluesForRoom(room.RoomId))
            {
                clues.Add(clue);
            }

            clues.Sort((left, right) => string.CompareOrdinal(left.ClueId, right.ClueId));
            return clues;
        }

        public string SaveNoteClue(RoomInstance? room, string clueId, string title, string bodyText, float textScale = 1f, PalaceClueTextStyle textStyle = PalaceClueTextStyle.Normal)
        {
            return SaveClue(room, clueId, PalaceClueType.Note, title, bodyText, string.Empty, Color.white, textScale, textStyle);
        }

        public string SaveImageClue(RoomInstance? room, string clueId, string title, string assetPath)
        {
            return SaveClue(room, clueId, PalaceClueType.Image, title, string.Empty, assetPath, Color.white, 1f, PalaceClueTextStyle.Normal);
        }

        public string SaveInkClue(RoomInstance? room, string clueId, string title, string serializedInk, Color inkColor, float inkThickness)
        {
            return SaveClue(room, clueId, PalaceClueType.Ink, title, serializedInk, string.Empty, inkColor, inkThickness, PalaceClueTextStyle.Normal);
        }

        private string SaveClue(RoomInstance? room, string clueId, PalaceClueType clueType, string title, string bodyText, string assetPath, Color tintColor, float textScale, PalaceClueTextStyle textStyle)
        {
            if (room == null)
            {
                return string.Empty;
            }

            var resolvedClueId = string.IsNullOrWhiteSpace(clueId) ? BuildUniqueClueId(room.RoomId) : clueId;
            var localPosition = GetDefaultClueLocalPosition(room, resolvedClueId);
            var localEulerAngles = Vector3.zero;
            var localScale = new Vector3(1f, 1f, 1f);
            var resolvedTextScale = textScale;
            var resolvedTextStyle = textStyle;

            if (PalaceSessionState.TryGetClue(resolvedClueId, out var existingClue))
            {
                localPosition = NormalizeClueLocalPosition(room, resolvedClueId, existingClue.LocalPosition);
                localEulerAngles = existingClue.LocalEulerAngles;
                localScale = existingClue.LocalScale;
                resolvedTextScale = clueType is PalaceClueType.Note or PalaceClueType.Ink ? textScale : existingClue.TextScale;
                resolvedTextStyle = clueType == PalaceClueType.Note ? textStyle : existingClue.TextStyle;
                if (clueType != PalaceClueType.Ink)
                {
                    tintColor = existingClue.TintColor;
                }
            }

            PalaceSessionState.SetClue(
                resolvedClueId,
                room.RoomId,
                clueType,
                title,
                bodyText,
                assetPath,
                tintColor,
                resolvedTextScale,
                resolvedTextStyle,
                localPosition,
                localEulerAngles,
                localScale);

            RefreshClueVisual(resolvedClueId);
            return resolvedClueId;
        }

        public void DeleteClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId))
            {
                return;
            }

            PalaceSessionState.RemoveClue(clueId);
            RemoveClueVisual(clueId);
        }

        public bool TryNudgeClue(RoomInstance? room, string clueId, Vector3 localDelta)
        {
            if (room == null || string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            var nextLocalPosition = ClampClueLocalPosition(room, clue.LocalPosition + localDelta);
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                clue.BodyText,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                nextLocalPosition,
                Vector3.zero,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool TryRotateClue(string clueId, float yawDeltaDegrees)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            var nextEulerAngles = new Vector3(0f, clue.LocalEulerAngles.y + yawDeltaDegrees, 0f);
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                clue.BodyText,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                clue.LocalPosition,
                nextEulerAngles,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool TryScaleClue(string clueId, float scaleDelta)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            var nextScaleValue = Mathf.Clamp(clue.LocalScale.x + scaleDelta, 0.65f, 2.1f);
            var nextScale = new Vector3(nextScaleValue, nextScaleValue, nextScaleValue);
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                clue.BodyText,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                clue.LocalPosition,
                clue.LocalEulerAngles,
                nextScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public System.Collections.Generic.IReadOnlyList<ClueWallOption> GetAvailableClueWalls(RoomInstance? room)
        {
            var walls = new System.Collections.Generic.List<ClueWallOption>(4);
            if (room == null)
            {
                return walls;
            }

            if (!room.OpenNorth)
            {
                walls.Add(new ClueWallOption("front", "Front Wall"));
            }

            if (!room.OpenWest)
            {
                walls.Add(new ClueWallOption("left", "Left Wall"));
            }

            if (!room.OpenEast)
            {
                walls.Add(new ClueWallOption("right", "Right Wall"));
            }

            if (!room.OpenSouth)
            {
                walls.Add(new ClueWallOption("back", "Back Wall"));
            }

            return walls;
        }

        public bool TryMoveClueToWall(RoomInstance? room, string clueId, string wallId)
        {
            if (room == null || string.IsNullOrWhiteSpace(clueId) || string.IsNullOrWhiteSpace(wallId) || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            if (!TryParseClueWall(wallId, out var targetWall) || !IsWallAvailable(room, targetWall))
            {
                return false;
            }

            var nextLocalPosition = MoveClueToWall(room, clue.LocalPosition, targetWall);
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                clue.BodyText,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                nextLocalPosition,
                Vector3.zero,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool TryMoveClueToWorldPoint(RoomInstance? room, string clueId, Vector3 worldPoint)
        {
            if (room == null || string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            var localPosition = worldPoint - room.LayoutCenter - new Vector3(0f, CluePivotHeightOffset, 0f);
            var nextLocalPosition = SnapClueToWall(room, ClampClueLocalPosition(room, localPosition));
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                clue.BodyText,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                nextLocalPosition,
                Vector3.zero,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool TryAppendInkPoint(RoomInstance? room, string clueId, Vector3 worldPoint, bool beginStroke)
        {
            if (room == null || string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue) || clue.ClueType != PalaceClueType.Ink)
            {
                return false;
            }

            var anchoredPosition = NormalizeClueLocalPosition(room, clueId, clue.LocalPosition);
            var anchoredWall = GetNearestClueWall(room, anchoredPosition);
            var clueRotation = GetClueWallRotation(anchoredWall);
            var clueCenter = room.LayoutCenter + anchoredPosition + new Vector3(0f, CluePivotHeightOffset, 0f);
            var rootLocalPoint = Quaternion.Inverse(clueRotation) * (worldPoint - clueCenter);
            var halfWidth = 0.76f * Mathf.Max(0.01f, clue.LocalScale.x);
            var halfHeight = 0.5f * Mathf.Max(0.01f, clue.LocalScale.y);
            var normalizedPoint = new Vector2(
                Mathf.Clamp(rootLocalPoint.x / halfWidth, -1f, 1f),
                Mathf.Clamp((rootLocalPoint.y + 0.03f) / halfHeight, -1f, 1f));

            var strokes = InkStrokeSerialization.Deserialize(clue.BodyText);
            if (beginStroke || strokes.Count == 0)
            {
                strokes.Add(new System.Collections.Generic.List<Vector2>());
            }

            var activeStroke = strokes[strokes.Count - 1];
            if (activeStroke.Count > 0 && Vector2.Distance(activeStroke[activeStroke.Count - 1], normalizedPoint) < 0.01f)
            {
                return false;
            }

            activeStroke.Add(normalizedPoint);
            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                InkStrokeSerialization.Serialize(strokes),
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                clue.LocalPosition,
                clue.LocalEulerAngles,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool TryEraseInkAtPoint(RoomInstance? room, string clueId, Vector3 worldPoint)
        {
            if (room == null || string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue) || clue.ClueType != PalaceClueType.Ink)
            {
                return false;
            }

            var anchoredPosition = NormalizeClueLocalPosition(room, clueId, clue.LocalPosition);
            var anchoredWall = GetNearestClueWall(room, anchoredPosition);
            var clueRotation = GetClueWallRotation(anchoredWall);
            var clueCenter = room.LayoutCenter + anchoredPosition + new Vector3(0f, CluePivotHeightOffset, 0f);
            var rootLocalPoint = Quaternion.Inverse(clueRotation) * (worldPoint - clueCenter);
            var halfWidth = 0.76f * Mathf.Max(0.01f, clue.LocalScale.x);
            var halfHeight = 0.5f * Mathf.Max(0.01f, clue.LocalScale.y);
            var normalizedPoint = new Vector2(
                Mathf.Clamp(rootLocalPoint.x / halfWidth, -1f, 1f),
                Mathf.Clamp((rootLocalPoint.y + 0.03f) / halfHeight, -1f, 1f));

            var strokes = InkStrokeSerialization.Deserialize(clue.BodyText);
            var changed = false;
            const float eraseRadius = 0.12f;
            for (var strokeIndex = strokes.Count - 1; strokeIndex >= 0; strokeIndex--)
            {
                var stroke = strokes[strokeIndex];
                for (var pointIndex = stroke.Count - 1; pointIndex >= 0; pointIndex--)
                {
                    if (Vector2.Distance(stroke[pointIndex], normalizedPoint) <= eraseRadius)
                    {
                        stroke.RemoveAt(pointIndex);
                        changed = true;
                    }
                }

                if (stroke.Count < 2)
                {
                    strokes.RemoveAt(strokeIndex);
                    changed = true;
                }
            }

            if (!changed)
            {
                return false;
            }

            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                InkStrokeSerialization.Serialize(strokes),
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                clue.LocalPosition,
                clue.LocalEulerAngles,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public bool ClearInkClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !PalaceSessionState.TryGetClue(clueId, out var clue) || clue.ClueType != PalaceClueType.Ink)
            {
                return false;
            }

            PalaceSessionState.SetClue(
                clueId,
                clue.RoomId,
                clue.ClueType,
                clue.Title,
                string.Empty,
                clue.AssetPath,
                clue.TintColor,
                clue.TextScale,
                clue.TextStyle,
                clue.LocalPosition,
                clue.LocalEulerAngles,
                clue.LocalScale);
            RefreshClueVisual(clueId);
            return true;
        }

        public void NavigateToParentRoom(RoomInstance? childRoom)
        {
            if (childRoom == null || string.IsNullOrWhiteSpace(childRoom.ParentRoomId))
            {
                return;
            }

            if (!roomInstances.TryGetValue(childRoom.ParentRoomId, out var parentRoom))
            {
                return;
            }

            TeleportPlayerToRoom(parentRoom);
        }

        public bool TryTeleportToRoom(string roomId)
        {
            if (!roomInstances.TryGetValue(roomId, out var room))
            {
                return false;
            }

            TeleportPlayerToRoom(room);
            return true;
        }

        public System.Collections.Generic.IReadOnlyList<MapRoomInfo> GetMapRoomInfos()
        {
            var results = new System.Collections.Generic.List<MapRoomInfo>(roomInstances.Count);
            foreach (var room in roomInstances.Values)
            {
                results.Add(new MapRoomInfo(
                    room.RoomId,
                    room.RoomDisplayName,
                    room.ParentRoomId,
                    room.LayoutCenter,
                    room.AccentColor,
                    PalaceSessionState.GetRoomDepth(room.RoomId)));
            }

            return results;
        }

        public bool TryGetPlayerMapPosition(out Vector3 worldPosition)
        {
            if (player != null)
            {
                worldPosition = player.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        private void SpawnPlayerAtEntryHall()
        {
            if (playerController != null)
            {
                playerController.TeleportTo(new Vector3(-2f, 1.1f, 0f), Quaternion.Euler(0f, 90f, 0f));
                return;
            }

            if (player != null)
            {
                player.SetPositionAndRotation(new Vector3(-2f, 1.1f, 0f), Quaternion.Euler(0f, 90f, 0f));
            }
        }

        private RoomInstance BuildRoom(
            string name,
            string displayName,
            Vector3 center,
            Vector3 size,
            Color color,
            string parentRoomId,
            bool openWest,
            bool openEast,
            bool openNorth,
            bool openSouth)
        {
            if (transform.Find(name) != null)
            {
                var existingRoom = transform.Find(name)!.GetComponent<RoomInstance>();
                roomInstances[name] = existingRoom;
                return existingRoom;
            }

            var roomRoot = new GameObject(name).transform;
            roomRoot.SetParent(transform);
            var roomRenderers = new System.Collections.Generic.List<Renderer>();

            roomRenderers.Add(CreateCube(roomRoot, "Floor", center + new Vector3(0f, -1.5f, 0f), new Vector3(size.x + 2.4f, 0.2f, size.z + 2.4f), color * 0.8f));
            roomRenderers.Add(CreateCube(roomRoot, "Ceiling", center + new Vector3(0f, 1.5f, 0f), new Vector3(size.x, 0.2f, size.z), color * 0.65f));
            roomRenderers.AddRange(CreateFrontBackWall(roomRoot, "WallNorth", center, size, color, true, openNorth));
            roomRenderers.AddRange(CreateFrontBackWall(roomRoot, "WallSouth", center, size, color, false, openSouth));
            roomRenderers.AddRange(CreateSideWall(roomRoot, "WallEast", center, size, color, true, openEast));
            roomRenderers.AddRange(CreateSideWall(roomRoot, "WallWest", center, size, color, false, openWest));

            var zone = roomRoot.gameObject.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = center;
            zone.size = new Vector3(size.x - 0.4f, size.y - 0.2f, size.z - 0.4f);

            var roomInstance = roomRoot.gameObject.AddComponent<RoomInstance>();
            var initialName = displayName;
            var initialColor = color;
            var initialParentRoomId = parentRoomId;
            if (PalaceSessionState.TryGetRoom(name, out var snapshot))
            {
                initialName = snapshot.DisplayName;
                initialColor = snapshot.AccentColor;
                initialParentRoomId = snapshot.ParentRoomId;
            }
            roomInstance.Initialize(
                name,
                initialName,
                initialColor,
                initialParentRoomId,
                center,
                size,
                center + new Vector3(0f, -0.4f, 0f),
                openWest,
                openEast,
                openNorth,
                openSouth,
                roomRenderers);

            var roomZone = roomRoot.gameObject.AddComponent<RoomZone>();
            roomZone.BindRoom(roomInstance);
            roomZone.SetDisplayName(initialName);
            roomInstances[name] = roomInstance;
            return roomInstance;
        }

        private void BuildSavedSubRooms()
        {
            var subRooms = new System.Collections.Generic.List<PalaceSessionState.RoomRecord>();
            foreach (var room in PalaceSessionState.GetAllRooms())
            {
                if (!string.IsNullOrWhiteSpace(room.ParentRoomId))
                {
                    subRooms.Add(room);
                }
            }

            subRooms.Sort((left, right) =>
            {
                var depthCompare = PalaceSessionState.GetRoomDepth(left.RoomId).CompareTo(PalaceSessionState.GetRoomDepth(right.RoomId));
                return depthCompare != 0 ? depthCompare : string.CompareOrdinal(left.RoomId, right.RoomId);
            });

            foreach (var room in subRooms)
            {
                if (!roomInstances.TryGetValue(room.ParentRoomId, out var parentRoom))
                {
                    continue;
                }

                BuildSubRoom(parentRoom, room.RoomId, room.DisplayName, room.AccentColor);
            }
        }

        private void BuildSavedClues()
        {
            foreach (var clue in PalaceSessionState.GetAllClues())
            {
                RefreshClueVisual(clue.ClueId);
            }
        }

        private RoomInstance BuildSubRoom(RoomInstance parentRoom, string roomId, string displayName, Color color)
        {
            var placement = GetChildPlacement(parentRoom, roomId);
            var outwardDirection = placement.direction;
            var center = placement.center;
            var size = new Vector3(8f, 3f, 8f);
            var openWest = outwardDirection == Vector3.right;
            var openEast = outwardDirection == Vector3.left;
            var openNorth = outwardDirection == Vector3.back;
            var openSouth = outwardDirection == Vector3.forward;
            var subRoom = BuildRoom(roomId, displayName, center, size, color, parentRoom.RoomId, openWest, openEast, openNorth, openSouth);
            EnsureRoomOpening(parentRoom, outwardDirection);
            BuildConnectorBetweenRooms(parentRoom, subRoom, outwardDirection);
            BuildSubRoomEntranceLabel(parentRoom, subRoom, outwardDirection);
            BuildRoomFeature(subRoom);
            return subRoom;
        }

        private static Vector3 GetOutwardDirection(RoomInstance room)
        {
            if (!string.IsNullOrWhiteSpace(room.ParentRoomId) && Instance != null && Instance.roomInstances.TryGetValue(room.ParentRoomId, out var parentRoom))
            {
                return QuantizeDirection(room.LayoutCenter - parentRoom.LayoutCenter);
            }

            return QuantizeDirection(room.LayoutCenter);
        }

        private static Vector3 GetOppositeDirection(Vector3 direction)
        {
            return new Vector3(-direction.x, 0f, -direction.z);
        }

        private static bool SameDirection(Vector3 left, Vector3 right)
        {
            return Vector3.Dot(left, right) > 0.99f;
        }

        private static string BuildUniqueSubRoomId(string parentRoomId)
        {
            var suffix = 1;
            var roomId = $"{parentRoomId}__SubRoom{suffix:00}";
            while (PalaceSessionState.TryGetRoom(roomId, out _))
            {
                suffix++;
                roomId = $"{parentRoomId}__SubRoom{suffix:00}";
            }

            return roomId;
        }

        private static System.Collections.Generic.List<Vector3> GetAvailableChildDirections(RoomInstance room)
        {
            var blockedDirection = GetOppositeDirection(GetOutwardDirection(room));
            var directions = new System.Collections.Generic.List<Vector3>(MaxChildBranchesPerRoom);
            foreach (var direction in CardinalDirections)
            {
                if (!SameDirection(direction, blockedDirection))
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        private bool HasAvailableChildSlot(RoomInstance parentRoom)
        {
            var availableDirections = GetAvailableChildDirections(parentRoom);
            var parentGrid = WorldToGrid(parentRoom.LayoutCenter);
            foreach (var direction in availableDirections)
            {
                var candidateGrid = parentGrid + DirectionToGridOffset(direction);
                if (!IsGridOccupied(candidateGrid))
                {
                    return true;
                }
            }

            return false;
        }

        private (Vector3 direction, Vector3 center) GetChildPlacement(RoomInstance parentRoom, string childRoomId)
        {
            var availableDirections = GetAvailableChildDirections(parentRoom);
            var childRoomIds = PalaceSessionState.GetChildRoomIds(parentRoom.RoomId);
            if (!childRoomIds.Contains(childRoomId))
            {
                childRoomIds.Add(childRoomId);
                childRoomIds.Sort(System.StringComparer.Ordinal);
            }

            var childIndex = childRoomIds.IndexOf(childRoomId);
            if (childIndex < 0)
            {
                childIndex = 0;
            }

            var parentGrid = WorldToGrid(parentRoom.LayoutCenter);
            for (var offset = 0; offset < availableDirections.Count; offset++)
            {
                var direction = availableDirections[(childIndex + offset) % availableDirections.Count];
                var candidateGrid = parentGrid + DirectionToGridOffset(direction);
                if (!IsGridOccupied(candidateGrid))
                {
                    return (direction, GridToWorld(candidateGrid, parentRoom.LayoutCenter.y));
                }
            }

            var fallbackDirection = availableDirections[Mathf.Clamp(childIndex, 0, availableDirections.Count - 1)];
            var fallbackGrid = parentGrid + DirectionToGridOffset(fallbackDirection);
            return (fallbackDirection, GridToWorld(fallbackGrid, parentRoom.LayoutCenter.y));
        }

        private static Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / BranchSpacing),
                Mathf.RoundToInt(worldPosition.z / BranchSpacing));
        }

        private static Vector3 GridToWorld(Vector2Int gridPosition, float y)
        {
            return new Vector3(gridPosition.x * BranchSpacing, y, gridPosition.y * BranchSpacing);
        }

        private static Vector2Int DirectionToGridOffset(Vector3 direction)
        {
            return new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.z));
        }

        private bool IsGridOccupied(Vector2Int gridPosition)
        {
            foreach (var room in roomInstances.Values)
            {
                if (WorldToGrid(room.LayoutCenter) == gridPosition)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 QuantizeDirection(Vector3 rawDirection)
        {
            rawDirection.y = 0f;
            if (Mathf.Abs(rawDirection.x) >= Mathf.Abs(rawDirection.z))
            {
                return rawDirection.x >= 0f ? Vector3.right : Vector3.left;
            }

            return rawDirection.z >= 0f ? Vector3.forward : Vector3.back;
        }

        private void TeleportPlayerToRoom(RoomInstance room)
        {
            var spawnPosition = room.SpawnPoint;
            var spawnRotation = Quaternion.Euler(0f, 90f, 0f);

            if (playerController != null)
            {
                playerController.TeleportTo(spawnPosition, spawnRotation);
            }
            else if (player != null)
            {
                player.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            playerRoomTracker?.SetCurrentRoom(room, room.RoomDisplayName);
        }

        private static string BuildUniqueSubRoomName(string parentDisplayName)
        {
            var candidateNames = new[]
            {
                $"{parentDisplayName} Sub-Room",
                $"{parentDisplayName} Study Room",
                $"{parentDisplayName} Recall Room",
                $"{parentDisplayName} Review Room"
            };

            foreach (var candidate in candidateNames)
            {
                if (PalaceSessionState.IsRoomDisplayNameAvailable(candidate))
                {
                    return candidate;
                }
            }

            var suffix = 2;
            var fallback = $"{parentDisplayName} Sub-Room {suffix}";
            while (!PalaceSessionState.IsRoomDisplayNameAvailable(fallback))
            {
                suffix++;
                fallback = $"{parentDisplayName} Sub-Room {suffix}";
            }

            return fallback;
        }

        private string BuildUniqueClueId(string roomId)
        {
            var suffix = 1;
            var clueId = $"{roomId}__Clue{suffix:00}";
            while (PalaceSessionState.TryGetClue(clueId, out _))
            {
                suffix++;
                clueId = $"{roomId}__Clue{suffix:00}";
            }

            return clueId;
        }

        private Vector3 GetDefaultClueLocalPosition(RoomInstance room, string clueId)
        {
            var roomClues = GetCluesForRoom(room);
            var newClueIndex = roomClues.Count;
            for (var i = 0; i < roomClues.Count; i++)
            {
                if (roomClues[i].ClueId == clueId)
                {
                    newClueIndex = i;
                    break;
                }
            }

            var column = newClueIndex % 3;
            var row = newClueIndex / 3;
            return new Vector3(-2f + column * 2f, -1.02f, room.RoomSize.z * 0.5f - 1.9f - row * 1.6f);
        }

        private Vector3 NormalizeClueLocalPosition(RoomInstance room, string clueId, Vector3 localPosition)
        {
            // Early Stage 2 notes used room-center-relative Y values that mounted notes too high.
            if (localPosition.y > -0.35f)
            {
                return GetDefaultClueLocalPosition(room, clueId);
            }

            return SnapClueToWall(room, ClampClueLocalPosition(room, localPosition));
        }

        private Vector3 ClampClueLocalPosition(RoomInstance room, Vector3 localPosition)
        {
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            return new Vector3(
                Mathf.Clamp(localPosition.x, -halfWidth, halfWidth),
                Mathf.Clamp(localPosition.y, -1.15f, 0.3f),
                Mathf.Clamp(localPosition.z, -halfDepth, halfDepth));
        }

        private Vector3 SnapClueToWall(RoomInstance room, Vector3 localPosition)
        {
            var wall = GetNearestClueWall(room, localPosition);
            if (!IsWallAvailable(room, wall))
            {
                foreach (var candidateWall in GetPreferredWallOrder(wall))
                {
                    if (IsWallAvailable(room, candidateWall))
                    {
                        wall = candidateWall;
                        break;
                    }
                }
            }

            return MoveClueToWall(room, localPosition, wall);
        }

        private ClueWall GetNearestClueWall(RoomInstance room, Vector3 localPosition)
        {
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;

            var distanceToEast = Mathf.Abs(halfWidth - localPosition.x);
            var distanceToWest = Mathf.Abs(-halfWidth - localPosition.x);
            var distanceToNorth = Mathf.Abs(halfDepth - localPosition.z);
            var distanceToSouth = Mathf.Abs(-halfDepth - localPosition.z);

            var closestWall = ClueWall.North;
            var closestDistance = distanceToNorth;

            if (distanceToSouth < closestDistance)
            {
                closestWall = ClueWall.South;
                closestDistance = distanceToSouth;
            }

            if (distanceToEast < closestDistance)
            {
                closestWall = ClueWall.East;
                closestDistance = distanceToEast;
            }

            if (distanceToWest < closestDistance)
            {
                closestWall = ClueWall.West;
            }

            return closestWall;
        }

        private static Quaternion GetClueWallRotation(ClueWall wall)
        {
            return wall switch
            {
                ClueWall.North => Quaternion.identity,
                ClueWall.South => Quaternion.Euler(0f, 180f, 0f),
                ClueWall.East => Quaternion.Euler(0f, 90f, 0f),
                _ => Quaternion.Euler(0f, -90f, 0f)
            };
        }

        private static bool TryParseClueWall(string wallId, out ClueWall wall)
        {
            switch (wallId.Trim().ToLowerInvariant())
            {
                case "front":
                case "north":
                    wall = ClueWall.North;
                    return true;
                case "back":
                case "south":
                    wall = ClueWall.South;
                    return true;
                case "right":
                case "east":
                    wall = ClueWall.East;
                    return true;
                case "left":
                case "west":
                    wall = ClueWall.West;
                    return true;
                default:
                    wall = ClueWall.North;
                    return false;
            }
        }

        private static System.Collections.Generic.IEnumerable<ClueWall> GetPreferredWallOrder(ClueWall startingWall)
        {
            yield return startingWall;
            yield return ClueWall.North;
            yield return ClueWall.West;
            yield return ClueWall.East;
            yield return ClueWall.South;
        }

        private static bool IsWallAvailable(RoomInstance room, ClueWall wall)
        {
            return wall switch
            {
                ClueWall.North => !room.OpenNorth,
                ClueWall.South => !room.OpenSouth,
                ClueWall.East => !room.OpenEast,
                _ => !room.OpenWest
            };
        }

        private static float GetClueWallTangent(ClueWall wall, Vector3 localPosition)
        {
            return wall == ClueWall.North || wall == ClueWall.South ? localPosition.x : localPosition.z;
        }

        private Vector3 MoveClueToWall(RoomInstance room, Vector3 localPosition, ClueWall targetWall)
        {
            var clampedPosition = ClampClueLocalPosition(room, localPosition);
            var currentWall = GetNearestClueWall(room, clampedPosition);
            var tangent = GetClueWallTangent(currentWall, clampedPosition);
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            var clampedY = Mathf.Clamp(clampedPosition.y, -1.15f, 0.3f);

            return targetWall switch
            {
                ClueWall.North => new Vector3(Mathf.Clamp(tangent, -halfWidth, halfWidth), clampedY, halfDepth - ClueWallInset),
                ClueWall.South => new Vector3(Mathf.Clamp(tangent, -halfWidth, halfWidth), clampedY, -halfDepth + ClueWallInset),
                ClueWall.East => new Vector3(halfWidth - ClueWallInset, clampedY, Mathf.Clamp(tangent, -halfDepth, halfDepth)),
                _ => new Vector3(-halfWidth + ClueWallInset, clampedY, Mathf.Clamp(tangent, -halfDepth, halfDepth))
            };
        }

        private void RefreshClueVisual(string clueId)
        {
            RemoveClueVisual(clueId);

            if (!PalaceSessionState.TryGetClue(clueId, out var clue) || !roomInstances.TryGetValue(clue.RoomId, out var room))
            {
                return;
            }

            var clueRoot = new GameObject($"Clue_{clueId}").transform;
            clueRoot.SetParent(room.transform);
            var anchoredPosition = NormalizeClueLocalPosition(room, clueId, clue.LocalPosition);
            var anchoredWall = GetNearestClueWall(room, anchoredPosition);
            clueRoot.position = room.LayoutCenter + anchoredPosition + new Vector3(0f, CluePivotHeightOffset, 0f);
            clueRoot.rotation = GetClueWallRotation(anchoredWall);
            clueRoot.localScale = Vector3.one;

            var visualPivot = new GameObject("VisualPivot").transform;
            visualPivot.SetParent(clueRoot);
            visualPivot.localPosition = Vector3.zero;
            // Wall placement now owns clue facing. Ignore older per-clue rotation so
            // notes/images mount consistently on every wall across rooms and sub-rooms.
            visualPivot.localRotation = Quaternion.identity;
            visualPivot.localScale = clue.LocalScale;

            var baseColor = Color.Lerp(room.AccentColor, Color.white, 0.55f);
            switch (clue.ClueType)
            {
                case PalaceClueType.Note:
                    BuildNoteClueVisual(visualPivot, clue, baseColor);
                    break;
                case PalaceClueType.Image:
                    BuildImageClueVisual(visualPivot, clue, baseColor);
                    break;
                case PalaceClueType.Ink:
                    BuildInkClueVisual(visualPivot, clue);
                    break;
                default:
                    BuildGenericClueVisual(visualPivot, clue, baseColor);
                    break;
            }

            var interactionCollider = clueRoot.gameObject.AddComponent<BoxCollider>();
            interactionCollider.center = Vector3.zero;
            interactionCollider.size = new Vector3(2.2f, 1.7f, 0.28f);
            var interactionTarget = clueRoot.gameObject.AddComponent<ClueInteractionTarget>();
            interactionTarget.Initialize(clueId, clue.RoomId);

            clueVisualRoots[clueId] = clueRoot;
        }

        private void RemoveClueVisual(string clueId)
        {
            if (!clueVisualRoots.TryGetValue(clueId, out var clueRoot))
            {
                foreach (var room in roomInstances.Values)
                {
                    var candidate = room.transform.Find($"Clue_{clueId}");
                    if (candidate != null)
                    {
                        clueRoot = candidate;
                        break;
                    }
                }

                if (clueRoot == null)
                {
                    return;
                }
            }

            clueVisualRoots.Remove(clueId);
            Destroy(clueRoot.gameObject);
        }

        private void BuildNoteClueVisual(Transform root, PalaceSessionState.ClueSnapshot clue, Color baseColor)
        {
            var fontStyle = GetUnityFontStyle(clue.TextStyle);
            var textScale = clue.TextScale;
            var wrappedTitle = WrapClueText(clue.Title, 12, 2);
            var wrappedBody = WrapClueText(clue.BodyText, 19, 4);
            var titleLineCount = CountWrappedLines(wrappedTitle);
            var faceColor = new Color(1f, 0.98f, 0.86f, 1f);
            var frameColor = new Color(0.34f, 0.36f, 0.34f, 1f);

            var noteAssembly = new GameObject("NoteAssembly").transform;
            noteAssembly.SetParent(root);
            noteAssembly.localPosition = Vector3.zero;
            noteAssembly.localRotation = Quaternion.identity;
            noteAssembly.localScale = Vector3.one;

            var noteBacker = CreatePrimitive(noteAssembly, PrimitiveType.Cube, "NoteBacker", new Vector3(0f, 0f, 0.08f), new Vector3(1.76f, 1.32f, 0.03f), frameColor);
            var noteBoardCore = CreatePrimitive(noteAssembly, PrimitiveType.Cube, "NoteBoardCore", Vector3.zero, new Vector3(1.66f, 1.22f, 0.08f), frameColor);
            var noteFace = CreatePrimitive(noteAssembly, PrimitiveType.Cube, "NoteFace", new Vector3(0f, 0f, -0.035f), new Vector3(1.54f, 1.1f, 0.02f), faceColor);
            noteBacker.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(frameColor);
            noteBoardCore.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(frameColor);
            noteFace.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(faceColor);

            var notePin = CreatePrimitive(noteAssembly, PrimitiveType.Sphere, "NotePin", new Vector3(0f, 0.6f, -0.09f), new Vector3(0.13f, 0.13f, 0.13f), new Color(0.84f, 0.12f, 0.12f));
            notePin.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(new Color(0.84f, 0.12f, 0.12f));

            var titleY = titleLineCount > 1 ? 0.24f : 0.28f;
            BuildClueText(noteAssembly, "NoteTitle", wrappedTitle, new Vector3(0f, titleY, -0.1f), Mathf.RoundToInt(28f * textScale), 0.04f * textScale, new Color(0.02f, 0.02f, 0.03f), TextAnchor.MiddleCenter, fontStyle);
            BuildClueText(noteAssembly, "NoteBody", wrappedBody, new Vector3(0f, -0.22f, -0.1f), Mathf.RoundToInt(18f * textScale), 0.028f * textScale, new Color(0.04f, 0.04f, 0.05f), TextAnchor.MiddleCenter, fontStyle);
        }

        private void BuildGenericClueVisual(Transform root, PalaceSessionState.ClueSnapshot clue, Color baseColor)
        {
            CreatePrimitive(root, PrimitiveType.Cube, "CluePanel", new Vector3(0f, -0.18f, 0f), new Vector3(1.7f, 1f, 0.08f), baseColor);
            BuildClueText(root, "ClueTitle", clue.Title, new Vector3(0f, -0.05f, 0.08f), 34, 0.05f, new Color(0.08f, 0.1f, 0.14f), TextAnchor.MiddleCenter, FontStyle.Normal);
            BuildClueText(root, "ClueType", clue.ClueType.ToString(), new Vector3(0f, -0.4f, 0.08f), 26, 0.045f, new Color(0.14f, 0.16f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private void BuildImageClueVisual(Transform root, PalaceSessionState.ClueSnapshot clue, Color baseColor)
        {
            var imageFrameColor = new Color(0.3f, 0.32f, 0.34f, 1f);
            var imageMatColor = new Color(0.96f, 0.96f, 0.93f, 1f);
            var imageFrameBack = CreatePrimitive(root, PrimitiveType.Cube, "ImageFrameBack", new Vector3(0f, 0f, 0.08f), new Vector3(1.94f, 1.46f, 0.04f), imageFrameColor);
            var imageFrameCore = CreatePrimitive(root, PrimitiveType.Cube, "ImageFrameCore", new Vector3(0f, 0f, 0f), new Vector3(1.84f, 1.36f, 0.08f), imageFrameColor);
            var imageMat = CreatePrimitive(root, PrimitiveType.Cube, "ImageMat", new Vector3(0f, 0f, -0.03f), new Vector3(1.62f, 1.14f, 0.02f), imageMatColor);
            var framePin = CreatePrimitive(root, PrimitiveType.Sphere, "FramePin", new Vector3(0f, 0.62f, -0.08f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.65f, 0.16f, 0.16f));
            imageFrameBack.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(imageFrameColor);
            imageFrameCore.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(imageFrameColor);
            imageMat.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(imageMatColor);
            framePin.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(new Color(0.65f, 0.16f, 0.16f));

            var imagePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            imagePlane.name = "ImagePlane";
            imagePlane.transform.SetParent(root);
            imagePlane.transform.localPosition = new Vector3(0f, -0.02f, -0.055f);
            imagePlane.transform.localRotation = Quaternion.identity;
            imagePlane.transform.localScale = new Vector3(1.42f, 0.92f, 1f);

            if (!TryApplyImageTexture(imagePlane.GetComponent<Renderer>(), clue.AssetPath))
            {
                imagePlane.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.Lerp(baseColor, Color.white, 0.35f));
                BuildClueText(root, "ImageFallbackTitle", WrapClueText(clue.Title, 12, 2), new Vector3(0f, 0.2f, -0.08f), 28, 0.042f, new Color(0.08f, 0.1f, 0.14f), TextAnchor.MiddleCenter, FontStyle.Normal);
                BuildClueText(root, "ImageFallbackBody", WrapClueText(Path.GetFileName(clue.AssetPath), 18, 3), new Vector3(0f, -0.18f, -0.08f), 20, 0.032f, new Color(0.15f, 0.17f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Normal);
            }
            else
            {
                BuildClueText(root, "ImageCaption", WrapClueText(clue.Title, 14, 2), new Vector3(0f, -0.68f, -0.09f), 22, 0.034f, new Color(0.04f, 0.05f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Normal);
            }

            Destroy(imagePlane.GetComponent<Collider>());
        }

        private void BuildInkClueVisual(Transform root, PalaceSessionState.ClueSnapshot clue)
        {
            var frameColor = new Color(0.3f, 0.32f, 0.34f, 1f);
            var faceColor = new Color(0.97f, 0.97f, 0.94f, 1f);
            var inkAssembly = new GameObject("InkAssembly").transform;
            inkAssembly.SetParent(root, false);
            inkAssembly.localPosition = Vector3.zero;
            inkAssembly.localRotation = Quaternion.identity;
            inkAssembly.localScale = Vector3.one;

            var inkBack = CreatePrimitive(inkAssembly, PrimitiveType.Cube, "InkBacker", new Vector3(0f, 0f, 0.09f), new Vector3(1.84f, 1.42f, 0.03f), frameColor);
            var inkCore = CreatePrimitive(inkAssembly, PrimitiveType.Cube, "InkBoardCore", new Vector3(0f, 0f, 0.02f), new Vector3(1.74f, 1.32f, 0.06f), frameColor);
            var inkFace = CreatePrimitive(inkAssembly, PrimitiveType.Cube, "InkFace", new Vector3(0f, 0f, -0.035f), new Vector3(1.56f, 1.12f, 0.016f), faceColor);
            var inkPin = CreatePrimitive(inkAssembly, PrimitiveType.Sphere, "InkPin", new Vector3(0f, 0.62f, -0.07f), new Vector3(0.1f, 0.1f, 0.1f), new Color(0.82f, 0.13f, 0.13f));
            inkBack.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(frameColor);
            inkCore.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(frameColor);
            inkFace.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(faceColor);
            inkPin.GetComponent<Renderer>().sharedMaterial = CreateUnlitMaterial(new Color(0.82f, 0.13f, 0.13f));

            BuildClueText(inkAssembly, "InkTitle", WrapClueText(clue.Title, 14, 2), new Vector3(0f, 0.45f, -0.085f), 22, 0.034f, new Color(0.06f, 0.07f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            if (string.IsNullOrWhiteSpace(clue.BodyText))
            {
                BuildClueText(inkAssembly, "InkHint", "Pinch to draw", new Vector3(0f, 0.02f, -0.086f), 20, 0.03f, new Color(0.4f, 0.42f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Italic);
            }
            BuildInkStrokeLines(inkAssembly, clue);
        }

        private static void BuildInkStrokeLines(Transform root, PalaceSessionState.ClueSnapshot clue)
        {
            var strokes = InkStrokeSerialization.Deserialize(clue.BodyText);
            if (strokes.Count == 0)
            {
                return;
            }

            var strokeColor = clue.TintColor.a <= 0f ? Color.black : clue.TintColor;
            var strokeWidth = Mathf.Clamp(0.032f * clue.TextScale, 0.022f, 0.065f);
            const float halfWidth = 0.76f;
            const float halfHeight = 0.5f;

            for (var strokeIndex = 0; strokeIndex < strokes.Count; strokeIndex++)
            {
                var stroke = strokes[strokeIndex];
                if (stroke.Count < 2)
                {
                    continue;
                }

                var strokeObject = new GameObject($"InkStroke_{strokeIndex}");
                strokeObject.transform.SetParent(root, false);
                var line = strokeObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = false;
                line.positionCount = stroke.Count;
                line.startWidth = strokeWidth;
                line.endWidth = strokeWidth;
                line.numCapVertices = 6;
                line.numCornerVertices = 6;
                line.alignment = LineAlignment.TransformZ;
                line.textureMode = LineTextureMode.Stretch;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.generateLightingData = false;
                line.sortingOrder = 10;
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                var material = new Material(shader);
                material.color = strokeColor;
                material.renderQueue = (int)RenderQueue.Transparent + 20;
                line.sharedMaterial = material;

                for (var pointIndex = 0; pointIndex < stroke.Count; pointIndex++)
                {
                    var point = stroke[pointIndex];
                    line.SetPosition(pointIndex, new Vector3(point.x * halfWidth, point.y * halfHeight - 0.03f, -0.095f));
                }
            }
        }

        private static void BuildClueText(Transform parent, string name, string content, Vector3 localPosition, int fontSize, float characterSize, Color color, TextAnchor anchor, FontStyle fontStyle)
        {
            var textObject = new GameObject(name).transform;
            textObject.SetParent(parent);
            textObject.localPosition = localPosition;
            textObject.localRotation = Quaternion.identity;

            var textMesh = textObject.gameObject.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.anchor = anchor;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = fontStyle;
            textMesh.color = color;
            ConfigureLabelTextRenderer(textObject.GetComponent<MeshRenderer>(), textMesh);
        }

        private static FontStyle GetUnityFontStyle(PalaceClueTextStyle textStyle)
        {
            return textStyle switch
            {
                PalaceClueTextStyle.Bold => FontStyle.Bold,
                PalaceClueTextStyle.Italic => FontStyle.Italic,
                _ => FontStyle.Normal
            };
        }

        private static bool TryApplyImageTexture(Renderer? renderer, string assetPath)
        {
            if (renderer == null || string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
            {
                return false;
            }

            var imageBytes = File.ReadAllBytes(assetPath);
            if (imageBytes.Length == 0)
            {
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(imageBytes))
            {
                Object.Destroy(texture);
                return false;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var shader = Shader.Find("Unlit/Texture")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                Object.Destroy(texture);
                return false;
            }

            var material = new Material(shader);
            if (material.HasProperty("_MainTex"))
            {
                material.mainTexture = texture;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return true;
        }

        private static string WrapClueText(string value, int maxLineLength, int maxLines)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Empty note";
            }

            var normalized = value.Trim().Replace('\n', ' ');
            var words = normalized.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var lines = new System.Collections.Generic.List<string>(maxLines);
            var currentLine = string.Empty;

            foreach (var word in words)
            {
                var remainingWord = word;
                while (remainingWord.Length > 0)
                {
                    var allowedLength = maxLineLength - (string.IsNullOrEmpty(currentLine) ? 0 : 1);
                    if (allowedLength <= 0)
                    {
                        lines.Add(currentLine);
                        if (lines.Count >= maxLines)
                        {
                            return AppendEllipsis(lines, maxLineLength);
                        }

                        currentLine = string.Empty;
                        allowedLength = maxLineLength;
                    }

                    if (remainingWord.Length <= allowedLength)
                    {
                        currentLine = string.IsNullOrEmpty(currentLine)
                            ? remainingWord
                            : currentLine + " " + remainingWord;
                        remainingWord = string.Empty;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        if (lines.Count >= maxLines)
                        {
                            return AppendEllipsis(lines, maxLineLength);
                        }

                        currentLine = string.Empty;
                        continue;
                    }

                    lines.Add(remainingWord.Substring(0, maxLineLength));
                    if (lines.Count >= maxLines)
                    {
                        return AppendEllipsis(lines, maxLineLength);
                    }

                    remainingWord = remainingWord.Substring(maxLineLength);
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return string.Join("\n", lines);
        }

        private static string AppendEllipsis(System.Collections.Generic.List<string> lines, int maxLineLength)
        {
            if (lines.Count == 0)
            {
                return string.Empty;
            }

            var lastLine = lines[lines.Count - 1];
            lines[lines.Count - 1] = lastLine.Length >= maxLineLength - 2
                ? lastLine.Substring(0, maxLineLength - 2) + ".."
                : lastLine + "..";
            return string.Join("\n", lines);
        }

        private static int CountWrappedLines(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 1;
            }

            var count = 1;
            foreach (var character in value)
            {
                if (character == '\n')
                {
                    count++;
                }
            }

            return count;
        }

        private void BuildRoomFeature(RoomInstance room)
        {
            var featureName = "RoomFeature";
            if (room.transform.Find(featureName) != null)
            {
                return;
            }

            var root = new GameObject(featureName).transform;
            root.SetParent(room.transform);
            root.position = room.LayoutCenter + new Vector3(0f, -1.25f, 0f);

            var hash = Mathf.Abs(room.RoomId.GetHashCode());
            var baseColor = Color.Lerp(room.AccentColor, Color.white, 0.2f);
            var accentColor = Color.Lerp(room.AccentColor, Color.black, 0.18f);

            switch (hash % 4)
            {
                case 0:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.4f, 1.2f), accentColor);
                    CreatePrimitive(root, PrimitiveType.Sphere, "Orb", new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.7f, 0.7f), baseColor);
                    break;
                case 1:
                    CreatePrimitive(root, PrimitiveType.Cube, "Table", new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.22f, 1.1f), accentColor);
                    CreatePrimitive(root, PrimitiveType.Capsule, "Marker", new Vector3(0f, 0.88f, 0f), new Vector3(0.24f, 0.7f, 0.24f), baseColor);
                    break;
                case 2:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "Plinth", new Vector3(0f, 0.18f, 0f), new Vector3(1.4f, 0.36f, 1.4f), accentColor);
                    CreatePrimitive(root, PrimitiveType.Cube, "Gem", new Vector3(0f, 0.82f, 0f), new Vector3(0.65f, 0.65f, 0.65f), baseColor).transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
                    break;
                default:
                    CreatePrimitive(root, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.14f, 0f), new Vector3(1.1f, 0.28f, 1.1f), accentColor);
                    CreatePrimitive(root, PrimitiveType.Cylinder, "Column", new Vector3(0f, 0.7f, 0f), new Vector3(0.22f, 0.82f, 0.22f), baseColor);
                    CreatePrimitive(root, PrimitiveType.Sphere, "Topper", new Vector3(0f, 1.45f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Color.Lerp(baseColor, Color.white, 0.25f));
                    break;
            }
        }

        private static System.Collections.Generic.IEnumerable<Renderer> CreateFrontBackWall(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Color color,
            bool northSide,
            bool hasOpening)
        {
            float wallZ = northSide ? center.z + size.z / 2f : center.z - size.z / 2f;

            if (!hasOpening)
            {
                return new[] { CreateCube(parent, name, new Vector3(center.x, center.y, wallZ), new Vector3(size.x, size.y, 0.2f), color) };
            }

            float sideX = size.x * 0.18f;
            float sideOffset = size.x * 0.41f;
            return new[]
            {
                CreateCube(parent, $"{name}_East", new Vector3(center.x + sideOffset, center.y, wallZ), new Vector3(sideX, size.y, 0.2f), color),
                CreateCube(parent, $"{name}_West", new Vector3(center.x - sideOffset, center.y, wallZ), new Vector3(sideX, size.y, 0.2f), color),
                CreateCube(parent, $"{name}_FrameEast", new Vector3(center.x + 1.65f, center.y, wallZ + (northSide ? -0.15f : 0.15f)), new Vector3(0.18f, size.y, 0.2f), Color.white),
                CreateCube(parent, $"{name}_FrameWest", new Vector3(center.x - 1.65f, center.y, wallZ + (northSide ? -0.15f : 0.15f)), new Vector3(0.18f, size.y, 0.2f), Color.white)
            };
        }

        private enum IconShape
        {
            Circle,
            Triangle,
            Diamond,
            Square
        }

        private (TextMesh textMesh, Renderer plateRenderer) BuildEntranceLabel(
            string name,
            string labelText,
            Vector3 position,
            Quaternion rotation,
            Color accentColor,
            IconShape iconShape,
            bool flattenForSideEntrance)
        {
            if (transform.Find(name) != null)
            {
                var existing = transform.Find(name)!;
                return (existing.GetComponentInChildren<TextMesh>()!, existing.Find("BackPlate")!.GetComponent<Renderer>());
            }

            var labelRoot = new GameObject(name).transform;
            labelRoot.SetParent(transform);
            labelRoot.position = position;
            labelRoot.rotation = rotation;

            var backPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backPlate.name = "BackPlate";
            backPlate.transform.SetParent(labelRoot);
            backPlate.transform.localPosition = Vector3.zero;
            backPlate.transform.localScale = new Vector3(1.7f, 0.55f, 0.08f);
            if (flattenForSideEntrance)
            {
                backPlate.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            backPlate.GetComponent<Renderer>().sharedMaterial = CreateMaterial(accentColor);
            Destroy(backPlate.GetComponent<Collider>());

            var maskPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            maskPlate.name = "RearMask";
            maskPlate.transform.SetParent(labelRoot);
            maskPlate.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            maskPlate.transform.localScale = new Vector3(1.72f, 0.57f, 0.03f);
            if (flattenForSideEntrance)
            {
                maskPlate.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            maskPlate.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.04f, 0.05f, 0.09f));
            Destroy(maskPlate.GetComponent<Collider>());

            var textObject = new GameObject("LabelText");
            textObject.transform.SetParent(labelRoot);
            textObject.transform.localPosition = new Vector3(0.15f, 0f, -0.05f);
            textObject.transform.localRotation = Quaternion.identity;

            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = labelText;
            textMesh.fontSize = 40;
            textMesh.characterSize = 0.065f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            BuildIcon(labelRoot, accentColor, iconShape);
            ConfigureLabelTextRenderer(textObject.GetComponent<MeshRenderer>(), textMesh);
            return (textMesh, backPlate.GetComponent<Renderer>());
        }

        private static void ConfigureLabelTextRenderer(MeshRenderer renderer, TextMesh textMesh)
        {
            if (renderer == null || textMesh.font == null)
            {
                return;
            }

            var shader = Shader.Find("Unlit/Transparent");
            var material = shader != null
                ? new Material(shader)
                : new Material(textMesh.font.material);
            material.mainTexture = textMesh.font.material.mainTexture;
            material.color = Color.white;
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_ZTest"))
            {
                material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)CullMode.Back);
            }

            renderer.sharedMaterial = material;
            renderer.sortingOrder = 0;
        }

        private static void BuildIcon(Transform parent, Color accentColor, IconShape iconShape)
        {
            PrimitiveType primitiveType = PrimitiveType.Sphere;
            Vector3 scale = new(0.22f, 0.22f, 0.08f);
            Vector3 rotation = Vector3.zero;

            switch (iconShape)
            {
                case IconShape.Circle:
                    primitiveType = PrimitiveType.Sphere;
                    scale = new Vector3(0.22f, 0.22f, 0.08f);
                    break;
                case IconShape.Triangle:
                    primitiveType = PrimitiveType.Capsule;
                    scale = new Vector3(0.14f, 0.28f, 0.08f);
                    rotation = new Vector3(0f, 0f, 90f);
                    break;
                case IconShape.Diamond:
                    primitiveType = PrimitiveType.Cube;
                    scale = new Vector3(0.2f, 0.2f, 0.08f);
                    rotation = new Vector3(0f, 0f, 45f);
                    break;
                case IconShape.Square:
                    primitiveType = PrimitiveType.Cube;
                    scale = new Vector3(0.2f, 0.2f, 0.08f);
                    break;
            }

            var icon = GameObject.CreatePrimitive(primitiveType);
            icon.name = "Icon";
            icon.transform.SetParent(parent);
            icon.transform.localPosition = new Vector3(-0.5f, 0f, -0.05f);
            icon.transform.localEulerAngles = rotation;
            icon.transform.localScale = scale;
            icon.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.white);
            Destroy(icon.GetComponent<Collider>());
        }

        private static System.Collections.Generic.IEnumerable<Renderer> CreateSideWall(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Color color,
            bool eastSide,
            bool hasOpening)
        {
            float wallX = eastSide ? center.x + size.x / 2f : center.x - size.x / 2f;

            if (!hasOpening)
            {
                return new[] { CreateCube(parent, name, new Vector3(wallX, center.y, center.z), new Vector3(0.2f, size.y, size.z), color) };
            }

            float sideZ = size.z * 0.18f;
            float sideOffset = size.z * 0.41f;
            return new[]
            {
                CreateCube(parent, $"{name}_North", new Vector3(wallX, center.y, center.z + sideOffset), new Vector3(0.2f, size.y, sideZ), color),
                CreateCube(parent, $"{name}_South", new Vector3(wallX, center.y, center.z - sideOffset), new Vector3(0.2f, size.y, sideZ), color),
                CreateCube(parent, $"{name}_FrameNorth", new Vector3(wallX + (eastSide ? -0.15f : 0.15f), center.y, center.z + 1.65f), new Vector3(0.2f, size.y, 0.18f), Color.white),
                CreateCube(parent, $"{name}_FrameSouth", new Vector3(wallX + (eastSide ? -0.15f : 0.15f), center.y, center.z - 1.65f), new Vector3(0.2f, size.y, 0.18f), Color.white)
            };
        }

        private void BuildConnector(string name, Vector3 center, Vector3 size)
        {
            if (transform.Find(name) != null)
            {
                return;
            }

            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = name;
            bridge.transform.SetParent(transform);
            bridge.transform.position = center;
            bridge.transform.localScale = size;

            var renderer = bridge.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(0.24f, 0.24f, 0.27f));

            var isHorizontal = Mathf.Abs(center.x) > Mathf.Abs(center.z);
            var bridgeScale = isHorizontal
                ? new Vector3(size.x + 0.6f, size.y, size.z + 2.6f)
                : new Vector3(size.x + 2.6f, size.y, size.z + 0.6f);
            bridge.transform.localScale = bridgeScale;

            var corridorHalfSpan = isHorizontal ? bridgeScale.z / 2f - 0.12f : bridgeScale.x / 2f - 0.12f;
            var wallLength = isHorizontal ? bridgeScale.x - 0.2f : bridgeScale.z - 0.2f;
            var wallScale = isHorizontal
                ? new Vector3(wallLength, 2.15f, 0.18f)
                : new Vector3(0.18f, 2.15f, wallLength);

            var wallY = 1.05f;
            var firstWallPosition = center + (isHorizontal ? new Vector3(0f, wallY, corridorHalfSpan) : new Vector3(corridorHalfSpan, wallY, 0f));
            var secondWallPosition = center + (isHorizontal ? new Vector3(0f, wallY, -corridorHalfSpan) : new Vector3(-corridorHalfSpan, wallY, 0f));
            CreateCube(transform, $"{name}_RailA", firstWallPosition, wallScale, new Color(0.76f, 0.76f, 0.8f));
            CreateCube(transform, $"{name}_RailB", secondWallPosition, wallScale, new Color(0.76f, 0.76f, 0.8f));
        }

        private void EnsureRoomOpening(RoomInstance room, Vector3 outwardDirection)
        {
            var roomRoot = room.transform;
            var renderers = new System.Collections.Generic.List<Renderer>();
            var openWest = room.OpenWest;
            var openEast = room.OpenEast;
            var openNorth = room.OpenNorth;
            var openSouth = room.OpenSouth;

            if (outwardDirection == Vector3.right || outwardDirection == Vector3.left)
            {
                var openingEast = outwardDirection == Vector3.right;
                RemoveChildrenByPrefix(roomRoot, openingEast ? "WallEast" : "WallWest");
                renderers.AddRange(CreateSideWall(roomRoot, openingEast ? "WallEast" : "WallWest", room.LayoutCenter, room.RoomSize, room.AccentColor, openingEast, true));
                if (openingEast)
                {
                    openEast = true;
                }
                else
                {
                    openWest = true;
                }
            }
            else
            {
                var openingNorth = outwardDirection == Vector3.forward;
                RemoveChildrenByPrefix(roomRoot, openingNorth ? "WallNorth" : "WallSouth");
                renderers.AddRange(CreateFrontBackWall(roomRoot, openingNorth ? "WallNorth" : "WallSouth", room.LayoutCenter, room.RoomSize, room.AccentColor, openingNorth, true));
                if (openingNorth)
                {
                    openNorth = true;
                }
                else
                {
                    openSouth = true;
                }
            }

            room.SetWallOpenings(openWest, openEast, openNorth, openSouth);
            room.RegisterRenderers(renderers);
        }

        private void BuildConnectorBetweenRooms(RoomInstance parentRoom, RoomInstance childRoom, Vector3 outwardDirection)
        {
            var connectorName = $"SubConnector_{parentRoom.RoomId}_To_{childRoom.RoomId}";
            if (transform.Find(connectorName) != null)
            {
                return;
            }

            var midpoint = (parentRoom.LayoutCenter + childRoom.LayoutCenter) * 0.5f;
            var isHorizontal = Mathf.Abs(outwardDirection.x) > 0.5f;
            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = connectorName;
            bridge.transform.SetParent(transform);
            bridge.transform.position = midpoint + new Vector3(0f, -1.4f, 0f);
            var bridgeScale = isHorizontal
                ? new Vector3(6.6f, 0.14f, 5f)
                : new Vector3(5f, 0.14f, 6.6f);
            bridge.transform.localScale = bridgeScale;
            bridge.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.3f, 0.3f, 0.34f));

            var corridorHalfSpan = isHorizontal ? bridgeScale.z / 2f - 0.12f : bridgeScale.x / 2f - 0.12f;
            var wallLength = isHorizontal ? bridgeScale.x - 0.2f : bridgeScale.z - 0.2f;
            var wallY = -0.35f;
            var leftWallPosition = midpoint + (isHorizontal ? new Vector3(0f, wallY, corridorHalfSpan) : new Vector3(corridorHalfSpan, wallY, 0f));
            var rightWallPosition = midpoint + (isHorizontal ? new Vector3(0f, wallY, -corridorHalfSpan) : new Vector3(-corridorHalfSpan, wallY, 0f));
            var wallScale = isHorizontal ? new Vector3(wallLength, 1.9f, 0.16f) : new Vector3(0.16f, 1.9f, wallLength);
            CreateCube(transform, $"{connectorName}_WallA", leftWallPosition, wallScale, new Color(0.84f, 0.84f, 0.88f));
            CreateCube(transform, $"{connectorName}_WallB", rightWallPosition, wallScale, new Color(0.84f, 0.84f, 0.88f));
        }

        private void BuildSubRoomEntranceLabel(RoomInstance parentRoom, RoomInstance childRoom, Vector3 outwardDirection)
        {
            var labelName = $"SubConnectorLabel_{parentRoom.RoomId}_To_{childRoom.RoomId}";
            var isHorizontal = Mathf.Abs(outwardDirection.x) > 0.5f;
            var labelRotation = isHorizontal
                ? Quaternion.Euler(0f, outwardDirection.x > 0f ? 90f : -90f, 0f)
                : Quaternion.Euler(0f, outwardDirection.z > 0f ? 0f : 180f, 0f);
            var labelPosition = parentRoom.LayoutCenter
                + outwardDirection * (isHorizontal ? parentRoom.RoomSize.x * 0.5f + 1f : parentRoom.RoomSize.z * 0.5f + 1f)
                + new Vector3(0f, 0.15f, 0f);

            var label = BuildEntranceLabel(labelName, childRoom.RoomDisplayName, labelPosition, labelRotation, childRoom.AccentColor, IconShape.Square, isHorizontal);
            childRoom.AttachEntranceSign(label.textMesh, label.plateRenderer);
        }

        private static void RemoveChildrenByPrefix(Transform parent, string prefix)
        {
            var targets = new System.Collections.Generic.List<GameObject>();
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    targets.Add(child.gameObject);
                }
            }

            foreach (var target in targets)
            {
                Object.Destroy(target);
            }
        }

        private static GameObject CreatePrimitive(Transform parent, PrimitiveType primitiveType, string name, Vector3 position, Vector3 scale, Color color)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
            Destroy(primitive.GetComponent<Collider>());
            return primitive;
        }

        private static Renderer CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(color);
            return renderer;
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return CreateMaterial(color);
            }

            var material = new Material(shader);
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }
    }
}
