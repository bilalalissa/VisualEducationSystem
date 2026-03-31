#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
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

        private const string EntryHallRoomId = "EntryHall";
        private const int MaxChildBranchesPerRoom = 3;
        private const float BranchSpacing = 14f;
        private static readonly Vector3[] CardinalDirections = { Vector3.right, Vector3.forward, Vector3.back, Vector3.left };
        public static PrototypePalaceBootstrap? Instance { get; private set; }
        [SerializeField] private Transform player = null!;
        private PlayerRoomTracker? playerRoomTracker;
        private VisualEducationSystem.Player.SimpleFirstPersonController? playerController;
        private int nextDynamicRoomIndex = 4;
        private bool hasWestBranchRoom;
        private readonly System.Collections.Generic.Dictionary<string, RoomInstance> roomInstances = new();
        public bool CanAddRoomFromEntryHall => !hasWestBranchRoom;

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
            roomInstance.Initialize(name, initialName, initialColor, initialParentRoomId, center, size, center + new Vector3(0f, -0.4f, 0f), roomRenderers);

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

            if (outwardDirection == Vector3.right || outwardDirection == Vector3.left)
            {
                RemoveChildrenByPrefix(roomRoot, outwardDirection == Vector3.right ? "WallEast" : "WallWest");
                renderers.AddRange(CreateSideWall(roomRoot, outwardDirection == Vector3.right ? "WallEast" : "WallWest", room.LayoutCenter, room.RoomSize, room.AccentColor, outwardDirection == Vector3.right, true));
            }
            else
            {
                RemoveChildrenByPrefix(roomRoot, outwardDirection == Vector3.forward ? "WallNorth" : "WallSouth");
                renderers.AddRange(CreateFrontBackWall(roomRoot, outwardDirection == Vector3.forward ? "WallNorth" : "WallSouth", room.LayoutCenter, room.RoomSize, room.AccentColor, outwardDirection == Vector3.forward, true));
            }

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
            primitive.transform.SetParent(parent);
            primitive.transform.localPosition = position;
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
