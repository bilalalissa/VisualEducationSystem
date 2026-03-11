using UnityEngine;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Rooms
{
    public sealed class PrototypePalaceBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform player = null!;
        private PlayerRoomTracker? playerRoomTracker;
        private VisualEducationSystem.Player.SimpleFirstPersonController? playerController;
        private int nextDynamicRoomIndex = 4;
        private bool hasWestBranchRoom;
        public bool CanAddRoomFromEntryHall => !hasWestBranchRoom;

        private void Start()
        {
            playerRoomTracker = player != null ? player.GetComponent<PlayerRoomTracker>() : null;
            playerController = player != null ? player.GetComponent<VisualEducationSystem.Player.SimpleFirstPersonController>() : null;
            EnsureLight();
            BuildGreybox();
        }

        private void BuildGreybox()
        {
            var entryHall = BuildRoom("EntryHall", "Entry Hall", new Vector3(0f, 1.5f, 0f), new Vector3(12f, 3f, 12f), new Color(0.18f, 0.3f, 0.62f), true, true, true, true);
            var room01 = BuildRoom("Room01Box", "Room 01", new Vector3(14f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.78f, 0.36f, 0.24f), true, false, false, false);
            var room02 = BuildRoom("Room02Box", "Room 02", new Vector3(0f, 1.5f, 14f), new Vector3(8f, 3f, 8f), new Color(0.18f, 0.58f, 0.3f), false, false, false, true);
            var room03 = BuildRoom("Room03Box", "Room 03", new Vector3(0f, 1.5f, -14f), new Vector3(8f, 3f, 8f), new Color(0.6f, 0.24f, 0.64f), false, false, true, false);

            BuildConnector("LinkEntryTo01", new Vector3(8f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            BuildConnector("LinkEntryTo02", new Vector3(0f, 0.1f, 8f), new Vector3(4f, 0.2f, 4f));
            BuildConnector("LinkEntryTo03", new Vector3(0f, 0.1f, -8f), new Vector3(4f, 0.2f, 4f));
            var room01Sign = BuildEntranceLabel("LabelRoom01", "Room 01", new Vector3(4.65f, 1.65f, 0f), Quaternion.Euler(0f, 90f, 0f), new Color(0.78f, 0.36f, 0.24f), IconShape.Circle, true);
            var room02Sign = BuildEntranceLabel("LabelRoom02", "Room 02", new Vector3(0f, 1.65f, 4.65f), Quaternion.identity, new Color(0.18f, 0.58f, 0.3f), IconShape.Triangle, false);
            var room03Sign = BuildEntranceLabel("LabelRoom03", "Room 03", new Vector3(0f, 1.65f, -4.65f), Quaternion.Euler(0f, 180f, 0f), new Color(0.6f, 0.24f, 0.64f), IconShape.Diamond, false);
            room01.AttachEntranceSign(room01Sign.textMesh, room01Sign.plateRenderer);
            room02.AttachEntranceSign(room02Sign.textMesh, room02Sign.plateRenderer);
            room03.AttachEntranceSign(room03Sign.textMesh, room03Sign.plateRenderer);

            hasWestBranchRoom = false;
            if (PalaceSessionState.HasWestBranchRoom)
            {
                TryAddRoomFromEntryHall();
            }

            SpawnPlayerAtEntryHall();

            playerRoomTracker?.SetCurrentRoom(entryHall, entryHall.RoomDisplayName);
        }

        public void TryAddRoomFromEntryHall()
        {
            if (!CanAddRoomFromEntryHall)
            {
                return;
            }

            var roomName = $"Room {nextDynamicRoomIndex:00}";
            var dynamicRoom = BuildRoom("Room04Box", roomName, new Vector3(-14f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.76f, 0.66f, 0.24f), false, true, false, false);
            BuildConnector("LinkEntryTo04", new Vector3(-8f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            var room04Sign = BuildEntranceLabel("LabelRoom04", roomName, new Vector3(-4.65f, 1.65f, 0f), Quaternion.Euler(0f, -90f, 0f), new Color(0.76f, 0.66f, 0.24f), IconShape.Square, true);
            dynamicRoom.AttachEntranceSign(room04Sign.textMesh, room04Sign.plateRenderer);
            hasWestBranchRoom = true;
            PalaceSessionState.HasWestBranchRoom = true;
            nextDynamicRoomIndex++;
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
            bool openWest,
            bool openEast,
            bool openNorth,
            bool openSouth)
        {
            if (transform.Find(name) != null)
            {
                return transform.Find(name)!.GetComponent<RoomInstance>();
            }

            var roomRoot = new GameObject(name).transform;
            roomRoot.SetParent(transform);
            var roomRenderers = new System.Collections.Generic.List<Renderer>();

            roomRenderers.Add(CreateCube(roomRoot, "Floor", center + new Vector3(0f, -1.5f, 0f), new Vector3(size.x, 0.2f, size.z), color * 0.8f));
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
            if (PalaceSessionState.TryGetRoom(name, out var snapshot))
            {
                initialName = snapshot.DisplayName;
                initialColor = snapshot.AccentColor;
            }
            roomInstance.Initialize(name, initialName, initialColor, roomRenderers);

            var roomZone = roomRoot.gameObject.AddComponent<RoomZone>();
            roomZone.BindRoom(roomInstance);
            roomZone.SetDisplayName(initialName);
            return roomInstance;
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

            var meshRenderer = textObject.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 10;

            BuildIcon(labelRoot, accentColor, iconShape);
            return (textMesh, backPlate.GetComponent<Renderer>());
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
