#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualEducationSystem.Interaction
{
    public sealed class RuntimeEventLogger : MonoBehaviour
    {
        private static readonly object FileLock = new();
        private static RuntimeEventLogger? instance;
        private static StreamWriter? writer;
        private static string currentLogFilePath = string.Empty;

        private Transform? observedTransform;
        private Vector3 lastLoggedPosition;
        private float lastLoggedYaw;
        private float lastMovementLogTime;

        public static string CurrentLogFilePath => currentLogFilePath;

        public static void EnsureOn(GameObject host)
        {
            if (host.GetComponent<RuntimeEventLogger>() == null)
            {
                host.AddComponent<RuntimeEventLogger>();
            }
        }

        public static void LogEvent(string category, string message)
        {
            WriteLine("EVENT", category, message);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            observedTransform = transform;
            lastLoggedPosition = transform.position;
            lastLoggedYaw = transform.eulerAngles.y;
            EnsureWriterInitialized();
            Application.logMessageReceivedThreaded += HandleUnityLog;
            WriteLine("SESSION", "app", "Runtime logger initialized.");
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= HandleUnityLog;
            WriteLine("SESSION", "app", "Runtime logger destroyed.");
            lock (FileLock)
            {
                writer?.Flush();
                writer?.Dispose();
                writer = null;
            }
            instance = null;
        }

        private void OnApplicationQuit()
        {
            WriteLine("SESSION", "app", "Application quitting.");
        }

        private void Update()
        {
            LogKeyboardEvents();
            LogMouseEvents();
            LogMovement();
        }

        private void LogKeyboardEvents()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            foreach (var key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame)
                {
                    continue;
                }

                LogEvent("input.key", $"Pressed {key.displayName} ({key.name})");
            }
        }

        private void LogMouseEvents()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                LogEvent("input.mouse", $"Left click at {mouse.position.ReadValue()}");
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                LogEvent("input.mouse", $"Right click at {mouse.position.ReadValue()}");
            }

            if (mouse.middleButton.wasPressedThisFrame)
            {
                LogEvent("input.mouse", $"Middle click at {mouse.position.ReadValue()}");
            }

            var scroll = mouse.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                LogEvent("input.mouse", $"Scroll {scroll}");
            }
        }

        private void LogMovement()
        {
            if (observedTransform == null)
            {
                return;
            }

            if (Time.unscaledTime - lastMovementLogTime < 0.25f)
            {
                return;
            }

            var position = observedTransform.position;
            var yaw = observedTransform.eulerAngles.y;
            var movedDistance = Vector3.Distance(position, lastLoggedPosition);
            var yawDelta = Mathf.Abs(Mathf.DeltaAngle(yaw, lastLoggedYaw));
            if (movedDistance < 0.15f && yawDelta < 2f)
            {
                return;
            }

            lastMovementLogTime = Time.unscaledTime;
            lastLoggedPosition = position;
            lastLoggedYaw = yaw;
            LogEvent("player.move", $"Position={position} Yaw={yaw:F1}");
        }

        private static void EnsureWriterInitialized()
        {
            if (writer != null)
            {
                return;
            }

            lock (FileLock)
            {
                if (writer != null)
                {
                    return;
                }

                var logsDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(logsDirectory);
                currentLogFilePath = Path.Combine(
                    logsDirectory,
                    $"ves-session-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
                writer = new StreamWriter(currentLogFilePath, false, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [SESSION] [app] Log file created at {currentLogFilePath}");
            }
        }

        private static void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            var category = type switch
            {
                LogType.Error => "unity.error",
                LogType.Assert => "unity.assert",
                LogType.Warning => "unity.warning",
                LogType.Exception => "unity.exception",
                _ => "unity.log"
            };

            var message = type == LogType.Error || type == LogType.Assert || type == LogType.Exception
                ? $"{condition}\n{stackTrace}"
                : condition;
            WriteLine(type.ToString().ToUpperInvariant(), category, message);
        }

        private static void WriteLine(string level, string category, string message)
        {
            try
            {
                EnsureWriterInitialized();
                lock (FileLock)
                {
                    if (writer == null)
                    {
                        return;
                    }

                    writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] [{category}] {message}");
                }
            }
            catch
            {
            }
        }
    }
}
