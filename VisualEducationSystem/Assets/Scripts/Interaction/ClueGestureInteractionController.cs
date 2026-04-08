#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using VisualEducationSystem.Rooms;
using VisualEducationSystem.Save;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Interaction
{
    public sealed class ClueGestureInteractionController : MonoBehaviour
    {
        private const float PointerMoveStep = 0.012f;
        private const float GestureScaleStep = 0.1f;

        private Camera? playerCamera;
        private PlayerRoomTracker? roomTracker;
        private PrototypePalaceBootstrap? palaceBootstrap;
        private Vector2 simulatedRightPointerViewport = new(0.72f, 0.54f);
        private string selectedClueId = string.Empty;
        private string draggingClueId = string.Empty;
        private float lastDragLogTime;
        private string statusMessage = "No clue selected.";

        public string SelectedClueId => selectedClueId;
        public Vector2 SimulatedRightPointerViewport => simulatedRightPointerViewport;
        public string StatusMessage => statusMessage;

        private void Awake()
        {
            playerCamera = GetComponentInChildren<Camera>();
            roomTracker = GetComponent<PlayerRoomTracker>();
        }

        private void Update()
        {
            palaceBootstrap ??= PrototypePalaceBootstrap.Instance;
            if (Keyboard.current == null || roomTracker == null || palaceBootstrap == null)
            {
                return;
            }

            UpdatePointerFromKeyboard();

            var coordinator = HandTrackingCoordinator.Instance;
            if (coordinator == null || coordinator.Lifecycle != HandTrackingLifecycle.Active || RoomEditorController.IsAnyEditorOpen)
            {
                EndDragIfNeeded();
                return;
            }

            if (coordinator.RightHand.Gesture == HandGestureType.Pinch)
            {
                HandlePinchInteraction();
            }
            else
            {
                EndDragIfNeeded();
                TrySelectHoveredClue();
            }

            HandleScaleShortcut(coordinator);
        }

        private void OnGUI()
        {
            var coordinator = HandTrackingCoordinator.Instance;
            if (coordinator == null || coordinator.Lifecycle != HandTrackingLifecycle.Active || RoomEditorController.IsAnyEditorOpen)
            {
                return;
            }

            var screenPoint = new Vector2(simulatedRightPointerViewport.x * Screen.width, (1f - simulatedRightPointerViewport.y) * Screen.height);
            var previousColor = GUI.color;
            GUI.color = new Color(1f, 0.2f, 0.8f, 0.95f);
            GUI.DrawTexture(new Rect(screenPoint.x - 6f, screenPoint.y - 6f, 12f, 12f), Texture2D.whiteTexture);
            GUI.color = previousColor;

            var infoRect = new Rect(Screen.width - 376f, 310f, 360f, 94f);
            GUI.Box(infoRect, string.Empty);
            GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 10f, infoRect.width - 24f, 18f), "Gesture Clue Control", GUI.skin.label);
            GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 30f, infoRect.width - 24f, 18f), $"Selected clue: {(string.IsNullOrWhiteSpace(selectedClueId) ? "None" : selectedClueId)}", GUI.skin.label);
            GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 48f, infoRect.width - 24f, 18f), "I/J/K/L move pointer. P pinch grab/drag. Hold F8 + [ or ] scale.", GUI.skin.label);
            GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 66f, infoRect.width - 24f, 18f), statusMessage, GUI.skin.label);
        }

        private void UpdatePointerFromKeyboard()
        {
            var keyboard = Keyboard.current!;
            var nextPointer = simulatedRightPointerViewport;
            if (keyboard.iKey.isPressed)
            {
                nextPointer.y += PointerMoveStep;
            }

            if (keyboard.kKey.isPressed)
            {
                nextPointer.y -= PointerMoveStep;
            }

            if (keyboard.jKey.isPressed)
            {
                nextPointer.x -= PointerMoveStep;
            }

            if (keyboard.lKey.isPressed)
            {
                nextPointer.x += PointerMoveStep;
            }

            simulatedRightPointerViewport = new Vector2(
                Mathf.Clamp(nextPointer.x, 0.05f, 0.95f),
                Mathf.Clamp(nextPointer.y, 0.05f, 0.95f));
        }

        private void HandlePinchInteraction()
        {
            if (string.IsNullOrWhiteSpace(draggingClueId))
            {
                if (!TrySelectHoveredClue())
                {
                    statusMessage = "Pinch on a clue to grab it.";
                    return;
                }

                draggingClueId = selectedClueId;
                statusMessage = $"Dragging clue {draggingClueId}.";
                RuntimeEventLogger.LogEvent("gesture.clue", $"Started dragging clue {draggingClueId}.");
            }

            if (roomTracker?.CurrentRoom == null || playerCamera == null)
            {
                return;
            }

            var ray = playerCamera.ViewportPointToRay(new Vector3(simulatedRightPointerViewport.x, simulatedRightPointerViewport.y, 0f));
            if (!TryGetRoomWallPoint(roomTracker.CurrentRoom, ray, out var wallPoint))
            {
                return;
            }

            if (palaceBootstrap != null && palaceBootstrap.TryMoveClueToWorldPoint(roomTracker.CurrentRoom, draggingClueId, wallPoint))
            {
                statusMessage = $"Dragging clue {draggingClueId}.";
                if (Time.unscaledTime - lastDragLogTime > 0.2f)
                {
                    lastDragLogTime = Time.unscaledTime;
                    RuntimeEventLogger.LogEvent("gesture.clue", $"Dragged clue {draggingClueId} to {wallPoint}.");
                }
            }
        }

        private bool TrySelectHoveredClue()
        {
            if (playerCamera == null)
            {
                return false;
            }

            var ray = playerCamera.ViewportPointToRay(new Vector3(simulatedRightPointerViewport.x, simulatedRightPointerViewport.y, 0f));
            if (!Physics.Raycast(ray, out var hit, 40f))
            {
                return false;
            }

            var target = hit.collider.GetComponent<ClueInteractionTarget>();
            if (target == null && hit.collider.transform.parent != null)
            {
                target = hit.collider.transform.parent.GetComponent<ClueInteractionTarget>();
            }

            if (target == null || roomTracker?.CurrentRoom == null || target.RoomId != roomTracker.CurrentRoom.RoomId)
            {
                return false;
            }

            if (!string.Equals(selectedClueId, target.ClueId, System.StringComparison.Ordinal))
            {
                selectedClueId = target.ClueId;
                statusMessage = $"Selected clue {selectedClueId}.";
                RuntimeEventLogger.LogEvent("gesture.clue", $"Selected clue {selectedClueId}.");
            }

            return true;
        }

        private void EndDragIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(draggingClueId))
            {
                return;
            }

            RuntimeEventLogger.LogEvent("gesture.clue", $"Finished dragging clue {draggingClueId}.");
            PalaceSaveManager.SaveCurrentState();
            statusMessage = $"Placed clue {draggingClueId}.";
            draggingClueId = string.Empty;
        }

        private void HandleScaleShortcut(HandTrackingCoordinator coordinator)
        {
            if (string.IsNullOrWhiteSpace(selectedClueId) || palaceBootstrap == null || coordinator.LeftHand.Gesture != HandGestureType.OpenPalm)
            {
                return;
            }

            if (Keyboard.current!.leftBracketKey.wasPressedThisFrame)
            {
                if (palaceBootstrap.TryScaleClue(selectedClueId, -GestureScaleStep))
                {
                    PalaceSaveManager.SaveCurrentState();
                    statusMessage = $"Scaled down clue {selectedClueId}.";
                    RuntimeEventLogger.LogEvent("gesture.clue", $"Scaled down clue {selectedClueId}.");
                }
            }

            if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {
                if (palaceBootstrap.TryScaleClue(selectedClueId, GestureScaleStep))
                {
                    PalaceSaveManager.SaveCurrentState();
                    statusMessage = $"Scaled up clue {selectedClueId}.";
                    RuntimeEventLogger.LogEvent("gesture.clue", $"Scaled up clue {selectedClueId}.");
                }
            }
        }

        private static bool TryGetRoomWallPoint(RoomInstance room, Ray ray, out Vector3 worldPoint)
        {
            worldPoint = default;
            var center = room.LayoutCenter;
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            var minY = center.y - 1.15f + 0.98f;
            var maxY = center.y + 0.3f + 0.98f;

            var bestDistance = float.MaxValue;
            if (!room.OpenNorth && TryHitVerticalPlane(ray, center.z + halfDepth, true, center.x - halfWidth, center.x + halfWidth, minY, maxY, out var northPoint, out var northDistance) && northDistance < bestDistance)
            {
                bestDistance = northDistance;
                worldPoint = northPoint;
            }

            if (!room.OpenSouth && TryHitVerticalPlane(ray, center.z - halfDepth, true, center.x - halfWidth, center.x + halfWidth, minY, maxY, out var southPoint, out var southDistance) && southDistance < bestDistance)
            {
                bestDistance = southDistance;
                worldPoint = southPoint;
            }

            if (!room.OpenEast && TryHitVerticalPlane(ray, center.x + halfWidth, false, center.z - halfDepth, center.z + halfDepth, minY, maxY, out var eastPoint, out var eastDistance) && eastDistance < bestDistance)
            {
                bestDistance = eastDistance;
                worldPoint = eastPoint;
            }

            if (!room.OpenWest && TryHitVerticalPlane(ray, center.x - halfWidth, false, center.z - halfDepth, center.z + halfDepth, minY, maxY, out var westPoint, out var westDistance) && westDistance < bestDistance)
            {
                bestDistance = westDistance;
                worldPoint = westPoint;
            }

            return bestDistance < float.MaxValue;
        }

        private static bool TryHitVerticalPlane(Ray ray, float planeValue, bool planeIsZ, float minTangentOffset, float maxTangentOffset, float minY, float maxY, out Vector3 point, out float distance)
        {
            point = default;
            distance = 0f;

            var axisValue = planeIsZ ? ray.direction.z : ray.direction.x;
            if (Mathf.Abs(axisValue) < 0.0001f)
            {
                return false;
            }

            var t = (planeValue - (planeIsZ ? ray.origin.z : ray.origin.x)) / axisValue;
            if (t <= 0f)
            {
                return false;
            }

            var candidate = ray.origin + ray.direction * t;
            var tangentOffset = planeIsZ ? candidate.x : candidate.z;
            if (tangentOffset < minTangentOffset || tangentOffset > maxTangentOffset || candidate.y < minY || candidate.y > maxY)
            {
                return false;
            }

            point = candidate;
            distance = t;
            return true;
        }
    }
}
