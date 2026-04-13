#if UNITY_EDITOR
#nullable enable
using UnityEditor;

namespace VisualEducationSystem.Interaction
{
    [InitializeOnLoad]
    internal static class HandTrackingEditorShutdown
    {
        static HandTrackingEditorShutdown()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            WebcamHandTrackingProvider.ShutdownAllProviders();
        }

        private static void OnBeforeAssemblyReload()
        {
            WebcamHandTrackingProvider.ShutdownAllProviders();
        }
    }
}
#endif
