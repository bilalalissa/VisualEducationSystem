using UnityEngine;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Rooms
{
    public sealed class PrototypePalaceBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform player = null!;

        private void Start()
        {
            EnsureLight();
            BuildGreybox();
        }

        private void BuildGreybox()
        {
            BuildRoom("EntryHall", "Entry Hall", new Vector3(0f, 1.5f, 0f), new Vector3(10f, 3f, 10f), new Color(0.18f, 0.3f, 0.62f), false, true);
            BuildRoom("Room01Box", "Room 01", new Vector3(14f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.78f, 0.36f, 0.24f), true, true);
            BuildRoom("Room02Box", "Room 02", new Vector3(28f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.18f, 0.58f, 0.3f), true, true);
            BuildRoom("Room03Box", "Room 03", new Vector3(42f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.6f, 0.24f, 0.64f), true, false);

            BuildConnector("LinkEntryTo01", new Vector3(7f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            BuildConnector("Link01To02", new Vector3(21f, 0.1f, 0f), new Vector3(6f, 0.2f, 4f));
            BuildConnector("Link02To03", new Vector3(35f, 0.1f, 0f), new Vector3(6f, 0.2f, 4f));

            if (player != null)
            {
                player.position = new Vector3(0f, 1.1f, -4f);
                player.rotation = Quaternion.identity;
            }
        }

        private void BuildRoom(
            string name,
            string displayName,
            Vector3 center,
            Vector3 size,
            Color color,
            bool openWest,
            bool openEast)
        {
            if (transform.Find(name) != null)
            {
                return;
            }

            var roomRoot = new GameObject(name).transform;
            roomRoot.SetParent(transform);

            CreateCube(roomRoot, "Floor", center + new Vector3(0f, -1.5f, 0f), new Vector3(size.x, 0.2f, size.z), color * 0.8f);
            CreateCube(roomRoot, "Ceiling", center + new Vector3(0f, 1.5f, 0f), new Vector3(size.x, 0.2f, size.z), color * 0.65f);
            CreateCube(roomRoot, "WallNorth", center + new Vector3(0f, 0f, size.z / 2f), new Vector3(size.x, size.y, 0.2f), color);
            CreateCube(roomRoot, "WallSouth", center + new Vector3(0f, 0f, -size.z / 2f), new Vector3(size.x, size.y, 0.2f), color);
            CreateSideWall(roomRoot, "WallEast", center, size, color, true, openEast);
            CreateSideWall(roomRoot, "WallWest", center, size, color, false, openWest);

            var zone = roomRoot.gameObject.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = center;
            zone.size = new Vector3(size.x - 0.4f, size.y - 0.2f, size.z - 0.4f);

            var roomZone = roomRoot.gameObject.AddComponent<RoomZone>();
            roomZone.SetDisplayName(displayName);
        }

        private static void CreateSideWall(
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
                CreateCube(parent, name, new Vector3(wallX, center.y, center.z), new Vector3(0.2f, size.y, size.z), color);
                return;
            }

            float sideZ = size.z * 0.18f;
            float sideOffset = size.z * 0.41f;
            CreateCube(parent, $"{name}_North", new Vector3(wallX, center.y, center.z + sideOffset), new Vector3(0.2f, size.y, sideZ), color);
            CreateCube(parent, $"{name}_South", new Vector3(wallX, center.y, center.z - sideOffset), new Vector3(0.2f, size.y, sideZ), color);
            CreateCube(parent, $"{name}_FrameNorth", new Vector3(wallX + (eastSide ? -0.15f : 0.15f), center.y, center.z + 1.65f), new Vector3(0.2f, size.y, 0.18f), Color.white);
            CreateCube(parent, $"{name}_FrameSouth", new Vector3(wallX + (eastSide ? -0.15f : 0.15f), center.y, center.z - 1.65f), new Vector3(0.2f, size.y, 0.18f), Color.white);
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

        private static void CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(color);
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
