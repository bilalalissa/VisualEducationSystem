using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VisualEducationSystem.Save;

namespace VisualEducationSystem.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum MenuMode
        {
            Root,
            Create,
            Load
        }

        [SerializeField] private string prototypeSceneName = "PrototypePalace";
        [SerializeField] private string loadSceneName = "PrototypePalace";

        private MenuMode menuMode;
        private string newPalaceName = "My Palace";
        private Vector2 loadScrollPosition;
        private string menuStatus = string.Empty;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (menuMode != MenuMode.Root)
            {
                menuStatus = string.Empty;
                menuMode = MenuMode.Root;
            }
        }

        private void OnGUI()
        {
            var windowWidth = Mathf.Min(560f, Screen.width - 48f);
            var windowHeight = Mathf.Min(520f, Screen.height - 48f);
            var windowRect = new Rect(
                24f,
                24f,
                windowWidth,
                windowHeight);

            GUILayout.BeginArea(windowRect, GUI.skin.box);
            GUILayout.Label("Visual Education System");
            GUILayout.Space(8f);
            GUILayout.Label("Phase 2 palace flow");
            GUILayout.Space(16f);

            switch (menuMode)
            {
                case MenuMode.Root:
                    DrawRootMenu();
                    break;
                case MenuMode.Create:
                    DrawCreateMenu();
                    break;
                case MenuMode.Load:
                    DrawLoadMenu();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawRootMenu()
        {
            menuStatus = string.Empty;

            if (GUILayout.Button("New Palace", GUILayout.Height(40f)))
            {
                menuMode = MenuMode.Create;
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Load Palace", GUILayout.Height(40f)))
            {
                menuMode = MenuMode.Load;
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Exit", GUILayout.Height(40f)))
            {
                ExitApplication();
            }
        }

        private void DrawCreateMenu()
        {
            GUILayout.Label("Create New Palace");
            GUILayout.Space(8f);
            GUILayout.Label("Palace Name");
            newPalaceName = GUILayout.TextField(newPalaceName, 32);
            GUILayout.Space(12f);

            if (GUILayout.Button("Create And Enter", GUILayout.Height(40f)))
            {
                if (!PalaceSaveManager.IsPalaceNameAvailable(newPalaceName))
                {
                    menuStatus = "Choose a unique palace name.";
                    return;
                }

                PalaceSaveManager.CreateNewPalace(newPalaceName);
                menuStatus = string.Empty;
                LoadScene(prototypeSceneName);
            }

            GUILayout.Space(8f);

            if (!string.IsNullOrWhiteSpace(menuStatus))
            {
                GUILayout.Label(menuStatus, GUI.skin.box, GUILayout.Height(40f));
                GUILayout.Space(8f);
            }

            if (GUILayout.Button("Back", GUILayout.Height(34f)))
            {
                menuStatus = string.Empty;
                menuMode = MenuMode.Root;
            }
        }

        private void DrawLoadMenu()
        {
            GUILayout.Label("Load Palace");
            GUILayout.Space(8f);
            GUILayout.Label("Select a palace to load or delete.");
            GUILayout.Space(6f);

            var scrollHeight = Mathf.Clamp(Screen.height - 280f, 160f, 250f);
            loadScrollPosition = GUILayout.BeginScrollView(loadScrollPosition, false, true, GUILayout.Height(scrollHeight));
            var palaces = PalaceSaveManager.GetPalaceSummaries();
            if (palaces.Count == 0)
            {
                GUILayout.Label("No saved palaces found.");
            }
            else
            {
                foreach (var palace in palaces)
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(palace.DisplayName, GUILayout.Width(260f), GUILayout.Height(36f));

                    if (GUILayout.Button("Load", GUILayout.Width(100f), GUILayout.Height(36f)))
                    {
                        if (PalaceSaveManager.LoadPalaceIntoSession(palace.PalaceId))
                        {
                            LoadScene(loadSceneName);
                        }
                        else
                        {
                            menuStatus = "Could not load that palace.";
                        }
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(100f), GUILayout.Height(36f)))
                    {
                        menuStatus = PalaceSaveManager.DeletePalace(palace.PalaceId)
                            ? $"Deleted palace: {palace.DisplayName}"
                            : $"Could not delete palace: {palace.DisplayName}";
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(6f);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(8f);

            if (!string.IsNullOrWhiteSpace(menuStatus))
            {
                GUILayout.Label(menuStatus, GUI.skin.box, GUILayout.Height(40f));
                GUILayout.Space(8f);
            }

            if (GUILayout.Button("Back", GUILayout.Height(34f)))
            {
                menuStatus = string.Empty;
                menuMode = MenuMode.Root;
            }
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
