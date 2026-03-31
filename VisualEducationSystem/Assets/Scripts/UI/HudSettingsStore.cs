using System;
using UnityEngine;

namespace VisualEducationSystem.UI
{
    public enum HudPanelId
    {
        Palace,
        Room,
        Help,
        MiniMap
    }

    public enum HudThemeMode
    {
        Light,
        Dark,
        HighContrast
    }

    public enum MiniMapAnchorMode
    {
        TopRight,
        BottomRight
    }

    [Serializable]
    public sealed class HudSettingsData
    {
        public int autoHideDelayIndex = 2;
        public int themeModeIndex = 0;
        public int miniMapAnchorModeIndex = 0;
        public bool autoHidePalace = true;
        public bool autoHideRoom = true;
        public bool autoHideHelp = true;
        public bool autoHideMiniMap = true;
    }

    public static class HudSettingsStore
    {
        private const string PreferencesKey = "ves.hud.settings";
        private static readonly float[] AutoHideDelayOptions = { 0f, 3f, 5f, 10f };
        private static readonly string[] AutoHideDelayLabels = { "Off", "3 sec", "5 sec", "10 sec" };

        private static HudSettingsData? cachedSettings;

        public static HudSettingsData Get()
        {
            if (cachedSettings != null)
            {
                return cachedSettings;
            }

            if (!PlayerPrefs.HasKey(PreferencesKey))
            {
                cachedSettings = new HudSettingsData();
                return cachedSettings;
            }

            var json = PlayerPrefs.GetString(PreferencesKey, string.Empty);
            cachedSettings = string.IsNullOrWhiteSpace(json)
                ? new HudSettingsData()
                : JsonUtility.FromJson<HudSettingsData>(json) ?? new HudSettingsData();
            Clamp(cachedSettings);
            return cachedSettings;
        }

        public static void Save()
        {
            var settings = Get();
            Clamp(settings);
            PlayerPrefs.SetString(PreferencesKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }

        public static void CycleAutoHideDelay()
        {
            var settings = Get();
            settings.autoHideDelayIndex = (settings.autoHideDelayIndex + 1) % AutoHideDelayOptions.Length;
            Save();
        }

        public static void CycleThemeMode()
        {
            var settings = Get();
            settings.themeModeIndex = (settings.themeModeIndex + 1) % Enum.GetValues(typeof(HudThemeMode)).Length;
            Save();
        }

        public static void CycleMiniMapAnchorMode()
        {
            var settings = Get();
            settings.miniMapAnchorModeIndex = (settings.miniMapAnchorModeIndex + 1) % Enum.GetValues(typeof(MiniMapAnchorMode)).Length;
            Save();
        }

        public static float GetAutoHideDelaySeconds()
        {
            var settings = Get();
            Clamp(settings);
            return AutoHideDelayOptions[settings.autoHideDelayIndex];
        }

        public static string GetAutoHideDelayLabel()
        {
            var settings = Get();
            Clamp(settings);
            return AutoHideDelayLabels[settings.autoHideDelayIndex];
        }

        public static HudThemeMode GetThemeMode()
        {
            var settings = Get();
            Clamp(settings);
            return (HudThemeMode)settings.themeModeIndex;
        }

        public static string GetThemeModeLabel()
        {
            return GetThemeMode() switch
            {
                HudThemeMode.Dark => "Dark",
                HudThemeMode.HighContrast => "High Contrast",
                _ => "Light"
            };
        }

        public static MiniMapAnchorMode GetMiniMapAnchorMode()
        {
            var settings = Get();
            Clamp(settings);
            return (MiniMapAnchorMode)settings.miniMapAnchorModeIndex;
        }

        public static string GetMiniMapAnchorModeLabel()
        {
            return GetMiniMapAnchorMode() switch
            {
                MiniMapAnchorMode.BottomRight => "Bottom Right",
                _ => "Top Right"
            };
        }

        public static bool IsPanelAutoHideEnabled(HudPanelId panelId)
        {
            var settings = Get();
            return panelId switch
            {
                HudPanelId.Palace => settings.autoHidePalace,
                HudPanelId.Room => settings.autoHideRoom,
                HudPanelId.Help => settings.autoHideHelp,
                HudPanelId.MiniMap => settings.autoHideMiniMap,
                _ => true
            };
        }

        public static void TogglePanelAutoHide(HudPanelId panelId)
        {
            var settings = Get();
            switch (panelId)
            {
                case HudPanelId.Palace:
                    settings.autoHidePalace = !settings.autoHidePalace;
                    break;
                case HudPanelId.Room:
                    settings.autoHideRoom = !settings.autoHideRoom;
                    break;
                case HudPanelId.Help:
                    settings.autoHideHelp = !settings.autoHideHelp;
                    break;
                case HudPanelId.MiniMap:
                    settings.autoHideMiniMap = !settings.autoHideMiniMap;
                    break;
            }

            Save();
        }

        public static void SetPanelAutoHide(HudPanelId panelId, bool enabled)
        {
            var settings = Get();
            switch (panelId)
            {
                case HudPanelId.Palace:
                    settings.autoHidePalace = enabled;
                    break;
                case HudPanelId.Room:
                    settings.autoHideRoom = enabled;
                    break;
                case HudPanelId.Help:
                    settings.autoHideHelp = enabled;
                    break;
                case HudPanelId.MiniMap:
                    settings.autoHideMiniMap = enabled;
                    break;
            }

            Save();
        }

        public static void ToggleAllPanelsAutoHide()
        {
            var settings = Get();
            var enableAll = !(settings.autoHidePalace && settings.autoHideRoom && settings.autoHideHelp && settings.autoHideMiniMap);
            settings.autoHidePalace = enableAll;
            settings.autoHideRoom = enableAll;
            settings.autoHideHelp = enableAll;
            settings.autoHideMiniMap = enableAll;
            Save();
        }

        private static void Clamp(HudSettingsData settings)
        {
            settings.autoHideDelayIndex = Mathf.Clamp(settings.autoHideDelayIndex, 0, AutoHideDelayOptions.Length - 1);
            settings.themeModeIndex = Mathf.Clamp(settings.themeModeIndex, 0, Enum.GetValues(typeof(HudThemeMode)).Length - 1);
            settings.miniMapAnchorModeIndex = Mathf.Clamp(settings.miniMapAnchorModeIndex, 0, Enum.GetValues(typeof(MiniMapAnchorMode)).Length - 1);
        }
    }
}
