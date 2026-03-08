using UnityEngine;

namespace VisualEducationSystem.UI
{
    public sealed class MainMenuSceneBootstrap : MonoBehaviour
    {
        private void Start()
        {
            EnsureCamera();
            EnsureLight();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 2.5f, -8f);
                Camera.main.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                Camera.main.backgroundColor = new Color(0.08f, 0.1f, 0.16f);
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.16f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.transform.position = new Vector3(0f, 2.5f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
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
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }
    }
}
