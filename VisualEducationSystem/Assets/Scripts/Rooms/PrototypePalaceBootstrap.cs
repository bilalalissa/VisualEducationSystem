using UnityEngine;

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
            BuildRoom("EntryHall", new Vector3(0f, 1.5f, 0f), new Vector3(12f, 3f, 12f), new Color(0.35f, 0.41f, 0.48f));
            BuildRoom("Room01Box", new Vector3(0f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.46f, 0.54f, 0.62f));
            BuildRoom("Room02Box", new Vector3(12f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.62f, 0.5f, 0.42f));
            BuildRoom("Room03Box", new Vector3(24f, 1.5f, 0f), new Vector3(8f, 3f, 8f), new Color(0.43f, 0.57f, 0.45f));

            BuildConnector("Link01", new Vector3(6f, 0.1f, 0f), new Vector3(4f, 0.2f, 3f));
            BuildConnector("Link02", new Vector3(18f, 0.1f, 0f), new Vector3(4f, 0.2f, 3f));

            if (player != null)
            {
                player.position = new Vector3(0f, 1.1f, -4f);
                player.rotation = Quaternion.identity;
            }
        }

        private void BuildRoom(string name, Vector3 center, Vector3 size, Color color)
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
            CreateCube(roomRoot, "WallEast", center + new Vector3(size.x / 2f, 0f, 0f), new Vector3(0.2f, size.y, size.z), color);
            CreateCube(roomRoot, "WallWest", center + new Vector3(-size.x / 2f, 0f, 0f), new Vector3(0.2f, size.y, size.z), color);
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
