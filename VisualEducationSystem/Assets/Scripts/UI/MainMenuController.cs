using UnityEngine;
using UnityEngine.SceneManagement;

namespace VisualEducationSystem.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string prototypeSceneName = "PrototypePalace";
        [SerializeField] private string loadSceneName = "PrototypePalace";

        private readonly Rect windowRect = new(24f, 24f, 320f, 220f);

        private void OnGUI()
        {
            GUILayout.BeginArea(windowRect, GUI.skin.box);
            GUILayout.Label("Visual Education System");
            GUILayout.Space(8f);
            GUILayout.Label("Phase 1 menu wiring");
            GUILayout.Space(16f);

            if (GUILayout.Button("New Palace", GUILayout.Height(40f)))
            {
                LoadScene(prototypeSceneName);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Load Palace", GUILayout.Height(40f)))
            {
                LoadScene(loadSceneName);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Exit", GUILayout.Height(40f)))
            {
                ExitApplication();
            }

            GUILayout.EndArea();
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("MainMenuController scene name is empty.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private static void ExitApplication()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
