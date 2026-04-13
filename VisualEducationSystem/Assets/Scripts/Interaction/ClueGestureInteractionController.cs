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
        private const float WebcamPointerSmoothing = 0.2f;
        private const float WebcamScaleActivationThreshold = 0.045f;
        private const float WebcamScaleStep = 0.08f;
        private const float DragDepth = 8f;
        private const float ClueWallInset = 0.72f;
        private const float DragPinchGraceSeconds = 1.2f;
        private const float PinchSelectionRadiusPixels = 140f;
        private const float InkSampleIntervalSeconds = 0.02f;
        private const float InkGuideHalfWidth = 0.76f;
        private const float InkGuideHalfHeight = 0.5f;
        private const float InkGuideLocalZ = -0.095f;
        private const float InkGuideLocalYOffset = -0.03f;
        private const float CluePivotHeightOffset = 0.98f;

        private Camera? playerCamera;
        private PlayerRoomTracker? roomTracker;
        private PrototypePalaceBootstrap? palaceBootstrap;
        private Vector2 simulatedRightPointerViewport = new(0.72f, 0.54f);
        private string selectedClueId = string.Empty;
        private string draggingClueId = string.Empty;
        private float lastDragLogTime;
        private bool wasLeftPinching;
        private float leftPinchAnchorY;
        private bool mirrorWebcamPointerX = true;
        private bool dragPlaneIsZ = true;
        private float dragPlaneValue;
        private float dragMinTangent;
        private float dragMaxTangent;
        private float dragMinY;
        private float dragMaxY;
        private Vector3 dragGrabOffset;
        private float lastRightPinchTime = -100f;
        private bool inkStrokeActive;
        private float lastInkSampleTime = -100f;
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

            var coordinator = HandTrackingCoordinator.Instance;
            if (coordinator == null || coordinator.Lifecycle != HandTrackingLifecycle.Active || RoomEditorController.IsAnyEditorOpen)
            {
                EndDragIfNeeded();
                ResetScaleGesture();
                return;
            }

            UpdatePointer(coordinator);

            if (RoomEditorController.IsInkDrawModeActive)
            {
                HandleInkDrawingMode(coordinator);
                return;
            }

            var rightPinchActive = coordinator.RightHand.Gesture == HandGestureType.Pinch;
            if (rightPinchActive)
            {
                lastRightPinchTime = Time.unscaledTime;
            }

            var keepDragAlive = !string.IsNullOrWhiteSpace(draggingClueId) && Time.unscaledTime - lastRightPinchTime <= DragPinchGraceSeconds;
            if (rightPinchActive || keepDragAlive)
            {
                HandlePinchInteraction();
            }
            else
            {
                EndDragIfNeeded();
                TrySelectHoveredClue();
            }

            HandleScaleShortcut(coordinator);
            HandleWebcamScaleGesture(coordinator);
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
            var pointerColor = new Color(1f, 0.2f, 0.8f, 0.95f);
            var pointerSize = 12f;
            if (RoomEditorController.IsInkDrawModeActive)
            {
                pointerColor = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                    ? (inkStrokeActive ? new Color(1f, 0.4f, 0.2f, 0.98f) : new Color(1f, 0.78f, 0.2f, 0.98f))
                    : (inkStrokeActive ? new Color(0.15f, 1f, 0.45f, 0.98f) : new Color(0.2f, 0.85f, 1f, 0.98f));
                pointerSize = 14f;
                GUI.color = new Color(pointerColor.r, pointerColor.g, pointerColor.b, 0.24f);
                GUI.DrawTexture(new Rect(screenPoint.x - 14f, screenPoint.y - 14f, 28f, 28f), Texture2D.whiteTexture);
            }
            GUI.color = pointerColor;
            GUI.DrawTexture(new Rect(screenPoint.x - pointerSize * 0.5f, screenPoint.y - pointerSize * 0.5f, pointerSize, pointerSize), Texture2D.whiteTexture);
            GUI.color = previousColor;
            if (RoomEditorController.IsInkDrawModeActive)
            {
                DrawInkTargetGuide();
            }
        }

        private void HandleInkDrawingMode(HandTrackingCoordinator coordinator)
        {
            selectedClueId = RoomEditorController.ActiveInkClueId;
            EndDragIfNeeded();
            ResetScaleGesture();

            if (string.IsNullOrWhiteSpace(selectedClueId) || roomTracker?.CurrentRoom == null || palaceBootstrap == null || playerCamera == null)
            {
                RoomEditorController.EndInkDrawMode();
                statusMessage = "Ink clue unavailable.";
                return;
            }

            var actionActive = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                ? coordinator.RightHand.Gesture == HandGestureType.Fist
                : coordinator.RightHand.Gesture == HandGestureType.Pinch;
            if (!actionActive)
            {
                if (inkStrokeActive)
                {
                    inkStrokeActive = false;
                    PalaceSaveManager.SaveCurrentState();
                    statusMessage = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                        ? $"Finished erasing on {selectedClueId}."
                        : $"Finished ink stroke on {selectedClueId}.";
                    RuntimeEventLogger.LogEvent("gesture.ink", RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                        ? $"Finished erasing on clue {selectedClueId}."
                        : $"Finished ink stroke on clue {selectedClueId}.");
                }

                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    RoomEditorController.EndInkDrawMode();
                    statusMessage = "Exited ink mode.";
                }
                else
                {
                    statusMessage = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                        ? $"Erase mode active for {selectedClueId}. Move the pointer onto the board, then hold right-hand fist to erase."
                        : $"Draw mode active for {selectedClueId}. Move the pointer onto the board, then hold right-hand pinch to draw.";
                }
                return;
            }

            var ray = playerCamera.ViewportPointToRay(new Vector3(simulatedRightPointerViewport.x, simulatedRightPointerViewport.y, 0f));
            if (!TryGetClueWallPoint(selectedClueId, ray, out var wallPoint))
            {
                statusMessage = $"Aim at the selected clue wall to draw on {selectedClueId}.";
                return;
            }

            if (Time.unscaledTime - lastInkSampleTime < InkSampleIntervalSeconds)
            {
                return;
            }

            var appended = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                ? palaceBootstrap.TryEraseInkAtPoint(roomTracker.CurrentRoom, selectedClueId, wallPoint)
                : palaceBootstrap.TryAppendInkPoint(roomTracker.CurrentRoom, selectedClueId, wallPoint, !inkStrokeActive);
            lastInkSampleTime = Time.unscaledTime;
            if (appended)
            {
                if (!inkStrokeActive)
                {
                    RuntimeEventLogger.LogEvent("gesture.ink", RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                        ? $"Started erasing on clue {selectedClueId}."
                        : $"Started ink stroke on clue {selectedClueId}.");
                }

                inkStrokeActive = true;
                statusMessage = RoomEditorController.ActiveInkMode == RoomEditorController.InkInteractionMode.Erase
                    ? $"Erasing on {selectedClueId}."
                    : $"Drawing on {selectedClueId}.";
            }
        }

        private void DrawInkTargetGuide()
        {
            if (playerCamera == null || roomTracker?.CurrentRoom == null || string.IsNullOrWhiteSpace(selectedClueId))
            {
                return;
            }

            if (!PalaceSessionState.TryGetClue(selectedClueId, out var clue))
            {
                return;
            }

            var clueRotation = GetApproximateClueWallRotation(roomTracker.CurrentRoom, clue.LocalPosition);
            var clueCenter = roomTracker.CurrentRoom.LayoutCenter + clue.LocalPosition + new Vector3(0f, CluePivotHeightOffset, 0f);
            var localBottom = InkGuideLocalYOffset - InkGuideHalfHeight * Mathf.Max(0.01f, clue.LocalScale.y);
            var localTop = InkGuideLocalYOffset + InkGuideHalfHeight * Mathf.Max(0.01f, clue.LocalScale.y);
            var localLeft = -InkGuideHalfWidth * Mathf.Max(0.01f, clue.LocalScale.x);
            var localRight = InkGuideHalfWidth * Mathf.Max(0.01f, clue.LocalScale.x);

            var topLeft = ProjectGuideCorner(clueCenter, clueRotation, new Vector3(localLeft, localTop, InkGuideLocalZ));
            var topRight = ProjectGuideCorner(clueCenter, clueRotation, new Vector3(localRight, localTop, InkGuideLocalZ));
            var bottomLeft = ProjectGuideCorner(clueCenter, clueRotation, new Vector3(localLeft, localBottom, InkGuideLocalZ));
            var bottomRight = ProjectGuideCorner(clueCenter, clueRotation, new Vector3(localRight, localBottom, InkGuideLocalZ));
            if (!topLeft.HasValue || !topRight.HasValue || !bottomLeft.HasValue || !bottomRight.HasValue)
            {
                return;
            }

            var previousColor = GUI.color;
            var guideColor = inkStrokeActive ? new Color(0.15f, 1f, 0.45f, 0.9f) : new Color(0.2f, 0.85f, 1f, 0.85f);
            DrawScreenLine(topLeft.Value, topRight.Value, guideColor, 3f);
            DrawScreenLine(topRight.Value, bottomRight.Value, guideColor, 3f);
            DrawScreenLine(bottomRight.Value, bottomLeft.Value, guideColor, 3f);
            DrawScreenLine(bottomLeft.Value, topLeft.Value, guideColor, 3f);
            GUI.color = new Color(guideColor.r, guideColor.g, guideColor.b, 0.12f);
            var fillRect = Rect.MinMaxRect(
                Mathf.Min(topLeft.Value.x, bottomLeft.Value.x),
                Mathf.Min(topLeft.Value.y, topRight.Value.y),
                Mathf.Max(topRight.Value.x, bottomRight.Value.x),
                Mathf.Max(bottomLeft.Value.y, bottomRight.Value.y));
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private Vector2? ProjectGuideCorner(Vector3 clueCenter, Quaternion clueRotation, Vector3 localCorner)
        {
            if (playerCamera == null)
            {
                return null;
            }

            var screenPoint = playerCamera.WorldToScreenPoint(clueCenter + clueRotation * localCorner);
            if (screenPoint.z <= 0f)
            {
                return null;
            }

            return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            var previousColor = GUI.color;
            var previousMatrix = GUI.matrix;
            var delta = end - start;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private static Quaternion GetApproximateClueWallRotation(RoomInstance room, Vector3 localPosition)
        {
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            var distanceToEast = Mathf.Abs(halfWidth - localPosition.x);
            var distanceToWest = Mathf.Abs(-halfWidth - localPosition.x);
            var distanceToNorth = Mathf.Abs(halfDepth - localPosition.z);
            var distanceToSouth = Mathf.Abs(-halfDepth - localPosition.z);

            if (distanceToNorth <= distanceToSouth && distanceToNorth <= distanceToEast && distanceToNorth <= distanceToWest)
            {
                return Quaternion.identity;
            }

            if (distanceToSouth <= distanceToEast && distanceToSouth <= distanceToWest)
            {
                return Quaternion.Euler(0f, 180f, 0f);
            }

            if (distanceToEast <= distanceToWest)
            {
                return Quaternion.Euler(0f, 90f, 0f);
            }

            return Quaternion.Euler(0f, -90f, 0f);
        }

        private void UpdatePointer(HandTrackingCoordinator coordinator)
        {
            if (coordinator.ActiveProviderName.Contains("Webcam"))
            {
                if (coordinator.RightHand.IsTracked)
                {
                    var targetViewport = new Vector2(
                        Mathf.Clamp(mirrorWebcamPointerX ? 1f - coordinator.RightHand.ViewportPosition.x : coordinator.RightHand.ViewportPosition.x, 0.01f, 0.99f),
                        Mathf.Clamp(coordinator.RightHand.ViewportPosition.y, 0.01f, 0.99f));
                    simulatedRightPointerViewport = Vector2.Lerp(
                        simulatedRightPointerViewport,
                        targetViewport,
                        1f - WebcamPointerSmoothing);
                }
                return;
            }

            UpdatePointerFromKeyboard();
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
                Mathf.Clamp(nextPointer.x, 0.01f, 0.99f),
                Mathf.Clamp(nextPointer.y, 0.01f, 0.99f));
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
                BeginDragPlane();
                CaptureDragGrabOffset();
                statusMessage = $"Dragging clue {draggingClueId}.";
                RuntimeEventLogger.LogEvent("gesture.clue", $"Started dragging clue {draggingClueId}.");
            }

            if (roomTracker?.CurrentRoom == null || playerCamera == null)
            {
                return;
            }

            var ray = playerCamera.ViewportPointToRay(new Vector3(simulatedRightPointerViewport.x, simulatedRightPointerViewport.y, 0f));
            if (!TryGetDragWallPoint(ray, out var wallPoint))
            {
                return;
            }

            var targetPoint = wallPoint + dragGrabOffset;
            if (palaceBootstrap != null && palaceBootstrap.TryMoveClueToWorldPoint(roomTracker.CurrentRoom, draggingClueId, targetPoint))
            {
                statusMessage = $"Dragging clue {draggingClueId}.";
                if (Time.unscaledTime - lastDragLogTime > 0.2f)
                {
                    lastDragLogTime = Time.unscaledTime;
                    RuntimeEventLogger.LogEvent("gesture.clue", $"Dragged clue {draggingClueId} to {targetPoint}.");
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
                return TrySelectNearestVisibleClue();
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

        private bool TrySelectNearestVisibleClue()
        {
            if (playerCamera == null || roomTracker?.CurrentRoom == null)
            {
                return false;
            }

            var pointerScreen = new Vector2(simulatedRightPointerViewport.x * Screen.width, (1f - simulatedRightPointerViewport.y) * Screen.height);
            var bestDistance = PinchSelectionRadiusPixels;
            ClueInteractionTarget? bestTarget = null;

            foreach (var target in FindObjectsOfType<ClueInteractionTarget>())
            {
                if (target == null || target.RoomId != roomTracker.CurrentRoom.RoomId)
                {
                    continue;
                }

                var screenPoint = playerCamera.WorldToScreenPoint(target.transform.position);
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                var distance = Vector2.Distance(pointerScreen, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = target;
            }

            if (bestTarget == null)
            {
                return false;
            }

            if (!string.Equals(selectedClueId, bestTarget.ClueId, System.StringComparison.Ordinal))
            {
                selectedClueId = bestTarget.ClueId;
                statusMessage = $"Selected clue {selectedClueId}.";
                RuntimeEventLogger.LogEvent("gesture.clue", $"Selected nearest clue {selectedClueId}.");
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
            ResetDragPlane();
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

        private void HandleWebcamScaleGesture(HandTrackingCoordinator coordinator)
        {
            if (string.IsNullOrWhiteSpace(selectedClueId) || palaceBootstrap == null || !coordinator.ActiveProviderName.Contains("Webcam"))
            {
                ResetScaleGesture();
                return;
            }

            if (!coordinator.LeftHand.IsTracked || coordinator.LeftHand.Gesture != HandGestureType.Pinch)
            {
                ResetScaleGesture();
                return;
            }

            if (!wasLeftPinching)
            {
                wasLeftPinching = true;
                leftPinchAnchorY = coordinator.LeftHand.ViewportPosition.y;
                statusMessage = $"Scaling clue {selectedClueId}.";
                return;
            }

            var deltaY = coordinator.LeftHand.ViewportPosition.y - leftPinchAnchorY;
            if (Mathf.Abs(deltaY) < WebcamScaleActivationThreshold)
            {
                return;
            }

            var scaleDelta = deltaY > 0f ? WebcamScaleStep : -WebcamScaleStep;
            if (palaceBootstrap.TryScaleClue(selectedClueId, scaleDelta))
            {
                PalaceSaveManager.SaveCurrentState();
                statusMessage = scaleDelta > 0f
                    ? $"Scaled up clue {selectedClueId}."
                    : $"Scaled down clue {selectedClueId}.";
                RuntimeEventLogger.LogEvent("gesture.clue", $"{(scaleDelta > 0f ? "Scaled up" : "Scaled down")} clue {selectedClueId} via webcam left pinch.");
            }

            leftPinchAnchorY = coordinator.LeftHand.ViewportPosition.y;
        }

        private void ResetScaleGesture()
        {
            wasLeftPinching = false;
            leftPinchAnchorY = 0f;
        }

        private bool TryGetClueWallPoint(string clueId, Ray ray, out Vector3 wallPoint)
        {
            wallPoint = default;
            if (roomTracker?.CurrentRoom == null || !PalaceSessionState.TryGetClue(clueId, out var clue))
            {
                return false;
            }

            var room = roomTracker.CurrentRoom;
            var localPosition = clue.LocalPosition;
            var center = room.LayoutCenter;
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            var minY = center.y - 1.15f + 0.98f;
            var maxY = center.y + 0.3f + 0.98f;
            var northDistance = Mathf.Abs((halfDepth - ClueWallInset) - localPosition.z);
            var southDistance = Mathf.Abs((-halfDepth + ClueWallInset) - localPosition.z);
            var eastDistance = Mathf.Abs((halfWidth - ClueWallInset) - localPosition.x);
            var westDistance = Mathf.Abs((-halfWidth + ClueWallInset) - localPosition.x);

            var planeIsZ = true;
            var planeValue = center.z + halfDepth - ClueWallInset;
            var minTangent = center.x - halfWidth;
            var maxTangent = center.x + halfWidth;
            var best = northDistance;

            if (southDistance < best)
            {
                best = southDistance;
                planeIsZ = true;
                planeValue = center.z - halfDepth + ClueWallInset;
                minTangent = center.x - halfWidth;
                maxTangent = center.x + halfWidth;
            }

            if (eastDistance < best)
            {
                best = eastDistance;
                planeIsZ = false;
                planeValue = center.x + halfWidth - ClueWallInset;
                minTangent = center.z - halfDepth;
                maxTangent = center.z + halfDepth;
            }

            if (westDistance < best)
            {
                planeIsZ = false;
                planeValue = center.x - halfWidth + ClueWallInset;
                minTangent = center.z - halfDepth;
                maxTangent = center.z + halfDepth;
            }

            return TryHitVerticalPlane(ray, planeValue, planeIsZ, minTangent, maxTangent, minY, maxY, out wallPoint, out _);
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

        private void BeginDragPlane()
        {
            if (roomTracker?.CurrentRoom == null || !PalaceSessionState.TryGetClue(draggingClueId, out var clue))
            {
                ResetDragPlane();
                return;
            }

            var room = roomTracker.CurrentRoom;
            var localPosition = clue.LocalPosition;
            var center = room.LayoutCenter;
            var halfWidth = room.RoomSize.x * 0.5f - 1.05f;
            var halfDepth = room.RoomSize.z * 0.5f - 1.2f;
            dragMinY = center.y - 1.15f + 0.98f;
            dragMaxY = center.y + 0.3f + 0.98f;

            var northDistance = Mathf.Abs((halfDepth - ClueWallInset) - localPosition.z);
            var southDistance = Mathf.Abs((-halfDepth + ClueWallInset) - localPosition.z);
            var eastDistance = Mathf.Abs((halfWidth - ClueWallInset) - localPosition.x);
            var westDistance = Mathf.Abs((-halfWidth + ClueWallInset) - localPosition.x);

            dragPlaneIsZ = true;
            dragPlaneValue = center.z + halfDepth - ClueWallInset;
            dragMinTangent = center.x - halfWidth;
            dragMaxTangent = center.x + halfWidth;
            var best = northDistance;

            if (southDistance < best)
            {
                best = southDistance;
                dragPlaneIsZ = true;
                dragPlaneValue = center.z - halfDepth + ClueWallInset;
                dragMinTangent = center.x - halfWidth;
                dragMaxTangent = center.x + halfWidth;
            }

            if (eastDistance < best)
            {
                best = eastDistance;
                dragPlaneIsZ = false;
                dragPlaneValue = center.x + halfWidth - ClueWallInset;
                dragMinTangent = center.z - halfDepth;
                dragMaxTangent = center.z + halfDepth;
            }

            if (westDistance < best)
            {
                dragPlaneIsZ = false;
                dragPlaneValue = center.x - halfWidth + ClueWallInset;
                dragMinTangent = center.z - halfDepth;
                dragMaxTangent = center.z + halfDepth;
            }
        }

        private void CaptureDragGrabOffset()
        {
            dragGrabOffset = Vector3.zero;
            if (playerCamera == null || roomTracker?.CurrentRoom == null || !PalaceSessionState.TryGetClue(draggingClueId, out var clue))
            {
                return;
            }

            var ray = playerCamera.ViewportPointToRay(new Vector3(simulatedRightPointerViewport.x, simulatedRightPointerViewport.y, 0f));
            if (!TryGetDragWallPoint(ray, out var wallPoint))
            {
                return;
            }

            var clueCenter = roomTracker.CurrentRoom.LayoutCenter + clue.LocalPosition + new Vector3(0f, 0.98f, 0f);
            dragGrabOffset = clueCenter - wallPoint;
            if (dragPlaneIsZ)
            {
                dragGrabOffset.z = 0f;
            }
            else
            {
                dragGrabOffset.x = 0f;
            }
        }

        private void ResetDragPlane()
        {
            dragPlaneValue = 0f;
            dragMinTangent = 0f;
            dragMaxTangent = 0f;
            dragMinY = 0f;
            dragMaxY = 0f;
            dragGrabOffset = Vector3.zero;
        }

        private bool TryGetDragWallPoint(Ray ray, out Vector3 wallPoint)
        {
            if (!string.IsNullOrWhiteSpace(draggingClueId))
            {
                return TryHitVerticalPlane(ray, dragPlaneValue, dragPlaneIsZ, dragMinTangent, dragMaxTangent, dragMinY, dragMaxY, out wallPoint, out _);
            }

            if (roomTracker?.CurrentRoom != null)
            {
                return TryGetRoomWallPoint(roomTracker.CurrentRoom, ray, out wallPoint);
            }

            wallPoint = ray.origin + ray.direction * DragDepth;
            return false;
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
