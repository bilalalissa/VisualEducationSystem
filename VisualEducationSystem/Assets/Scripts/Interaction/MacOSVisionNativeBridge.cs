#nullable enable
using System;
using System.Runtime.InteropServices;

namespace VisualEducationSystem.Interaction
{
    internal static class MacOSVisionNativeBridge
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private const string PluginName = "VESVisionBridge";

        [DllImport(PluginName, EntryPoint = "VESVision_IsBackendAvailable")]
        private static extern int NativeIsBackendAvailable();

        [DllImport(PluginName, EntryPoint = "VESVision_CopyStatusMessage")]
        private static extern IntPtr NativeCopyStatusMessage();

        [DllImport(PluginName, EntryPoint = "VESVision_CopyLatestFrameJson")]
        private static extern IntPtr NativeCopyLatestFrameJson();

        [DllImport(PluginName, EntryPoint = "VESVision_StopBackend")]
        private static extern void NativeStopBackend();

        [DllImport(PluginName, EntryPoint = "VESVision_FreeCopiedString")]
        private static extern void NativeFreeCopiedString(IntPtr pointer);
#endif

        public static bool TryIsBackendAvailable(out bool isAvailable, out string status)
        {
            try
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                isAvailable = NativeIsBackendAvailable() != 0;
                status = TryReadStatusMessage() ?? (isAvailable
                    ? "Native macOS Vision backend is available."
                    : "Native macOS Vision backend reported unavailable.");
                return true;
#else
                isAvailable = false;
                status = "macOS Vision bridge is only supported on macOS.";
                return true;
#endif
            }
            catch (DllNotFoundException)
            {
                isAvailable = false;
                status = "Native macOS Vision plugin library was not found.";
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                isAvailable = false;
                status = "Native macOS Vision plugin is missing required entry points.";
                return false;
            }
            catch (Exception ex)
            {
                isAvailable = false;
                status = $"Native macOS Vision bridge error: {ex.Message}";
                return false;
            }
        }

        private static string? TryReadStatusMessage()
        {
            return TryReadCopiedString(NativeCopyStatusMessage);
        }

        public static bool TryGetLatestFrameJson(out string json, out string status)
        {
            try
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                json = TryReadCopiedString(NativeCopyLatestFrameJson) ?? string.Empty;
                status = string.IsNullOrWhiteSpace(json)
                    ? "Native macOS Vision backend returned no frame payload."
                    : "Native macOS Vision frame payload received.";
                return !string.IsNullOrWhiteSpace(json);
#else
                json = string.Empty;
                status = "macOS Vision bridge is only supported on macOS.";
                return false;
#endif
            }
            catch (DllNotFoundException)
            {
                json = string.Empty;
                status = "Native macOS Vision plugin library was not found.";
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                json = string.Empty;
                status = "Native macOS Vision plugin is missing the frame JSON entry point.";
                return false;
            }
            catch (Exception ex)
            {
                json = string.Empty;
                status = $"Native macOS Vision frame bridge error: {ex.Message}";
                return false;
            }
        }

        public static void TryStopBackend()
        {
            try
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                NativeStopBackend();
#endif
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static string? TryReadCopiedString(Func<IntPtr> copyFunction)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            var pointer = IntPtr.Zero;
            try
            {
                pointer = copyFunction();
                if (pointer == IntPtr.Zero)
                {
                    return null;
                }

                return Marshal.PtrToStringAnsi(pointer);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    NativeFreeCopiedString(pointer);
                }
            }
#else
            return null;
#endif
        }
    }
}
