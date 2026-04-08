using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VisualEducationSystem.Interaction;
using VisualEducationSystem.Save;

namespace VisualEducationSystem.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum MenuMode
        {
            Root,
            Create,
            Load,
            Settings
        }

        [SerializeField] private string prototypeSceneName = "PrototypePalace";
        [SerializeField] private string loadSceneName = "PrototypePalace";

        private MenuMode menuMode;
        private string newPalaceName = "My Palace";
        private Vector2 loadScrollPosition;
        private Vector2 settingsScrollPosition;
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
                case MenuMode.Settings:
                    DrawSettingsMenu();
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
                RuntimeEventLogger.LogEvent("main_menu", "Opened create palace menu.");
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Load Palace", GUILayout.Height(40f)))
            {
                menuMode = MenuMode.Load;
                RuntimeEventLogger.LogEvent("main_menu", "Opened load palace menu.");
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Settings", GUILayout.Height(40f)))
            {
                menuMode = MenuMode.Settings;
                RuntimeEventLogger.LogEvent("main_menu", "Opened settings menu.");
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Exit", GUILayout.Height(40f)))
            {
                RuntimeEventLogger.LogEvent("main_menu", "Exit requested from root menu.");
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
                    RuntimeEventLogger.LogEvent("main_menu", $"Create palace rejected for duplicate name \"{newPalaceName}\".");
                    return;
                }

                PalaceSaveManager.CreateNewPalace(newPalaceName);
                menuStatus = string.Empty;
                RuntimeEventLogger.LogEvent("main_menu", $"Created new palace \"{newPalaceName}\" and entering scene.");
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
                RuntimeEventLogger.LogEvent("main_menu", "Returned to root menu from create menu.");
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
                            RuntimeEventLogger.LogEvent("main_menu", $"Loaded palace \"{palace.DisplayName}\".");
                            LoadScene(loadSceneName);
                        }
                        else
                        {
                            menuStatus = "Could not load that palace.";
                            RuntimeEventLogger.LogEvent("main_menu", $"Failed to load palace \"{palace.DisplayName}\".");
                        }
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(100f), GUILayout.Height(36f)))
                    {
                        menuStatus = PalaceSaveManager.DeletePalace(palace.PalaceId)
                            ? $"Deleted palace: {palace.DisplayName}"
                            : $"Could not delete palace: {palace.DisplayName}";
                        RuntimeEventLogger.LogEvent("main_menu", menuStatus);
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
                RuntimeEventLogger.LogEvent("main_menu", "Returned to root menu from load menu.");
            }
        }

        private void DrawSettingsMenu()
        {
            GUILayout.Label("HUD Settings");
            GUILayout.Space(8f);

            var scrollHeight = Mathf.Clamp(Screen.height - 240f, 180f, 280f);
            settingsScrollPosition = GUILayout.BeginScrollView(settingsScrollPosition, false, true, GUILayout.Height(scrollHeight));

            if (GUILayout.Button($"Auto-Hide Delay: {HudSettingsStore.GetAutoHideDelayLabel()}", GUILayout.Height(38f)))
            {
                HudSettingsStore.CycleAutoHideDelay();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button($"HUD Theme: {HudSettingsStore.GetThemeModeLabel()}", GUILayout.Height(38f)))
            {
                HudSettingsStore.CycleThemeMode();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button($"Mini Map Position: {HudSettingsStore.GetMiniMapAnchorModeLabel()}", GUILayout.Height(38f)))
            {
                HudSettingsStore.CycleMiniMapAnchorMode();
            }

            GUILayout.Space(12f);
            GUILayout.Label("In the palace:");
            GUILayout.Label("- HUD panels hide while you move and reappear after you stop.");
            GUILayout.Label("- Hold Tab to temporarily reveal slid-away HUD panels.");
            GUILayout.Label("- Hold Left Alt to show panel numbers.");
            GUILayout.Label("- Press Alt+1/2/3/4 to toggle Palace/Room/Help/Mini Map.");
            GUILayout.Label("- Press Alt+0 to toggle all panels together.");
            GUILayout.Label("- Auto-hide slides panels to the edge instead of fully hiding them.");
            GUILayout.Space(12f);

            var settings = HudSettingsStore.Get();
            GUILayout.Label("Current panel defaults");
            GUILayout.Label($"Palace box: {(settings.autoHidePalace ? "Auto-hide" : "Pinned")}");
            GUILayout.Label($"Room box: {(settings.autoHideRoom ? "Auto-hide" : "Pinned")}");
            GUILayout.Label($"Help box: {(settings.autoHideHelp ? "Auto-hide" : "Pinned")}");
            GUILayout.Label($"Mini map: {(settings.autoHideMiniMap ? "Auto-hide" : "Pinned")}");
            GUILayout.Space(12f);

            GUILayout.EndScrollView();

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
