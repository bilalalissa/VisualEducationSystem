#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualEducationSystem.Interaction
{
    public interface IHandTrackingProvider
    {
        bool TryGetFrame(out HandTrackingFrame frame);
    }

    public sealed class SimulatedHandTrackingProvider : MonoBehaviour, IHandTrackingProvider
    {
        [SerializeField] private bool userVisible = true;
        [SerializeField] private float userConfidence = 0.92f;
        [SerializeField] private Vector2 leftHandViewportPosition = new(0.28f, 0.54f);
        [SerializeField] private Vector2 rightHandViewportPosition = new(0.72f, 0.54f);

        private const int SimulatedUserId = 1;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f6Key.wasPressedThisFrame)
            {
                userVisible = !userVisible;
            }
        }

        public bool TryGetFrame(out HandTrackingFrame frame)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                frame = default;
                return false;
            }

            var leftGesture = HandGestureType.None;
            if (keyboard.f8Key.isPressed)
            {
                leftGesture = HandGestureType.OpenPalm;
            }
            else if (keyboard.f9Key.isPressed)
            {
                leftGesture = HandGestureType.Fist;
            }

            var rightGesture = HandGestureType.None;
            if (keyboard.f7Key.isPressed)
            {
                rightGesture = HandGestureType.ThumbsUp;
            }
            else if (keyboard.pKey.isPressed)
            {
                rightGesture = HandGestureType.Pinch;
            }
            else if (keyboard.oKey.isPressed)
            {
                rightGesture = HandGestureType.Point;
            }

            var leftHand = new HandSample(userVisible, userConfidence, leftHandViewportPosition, leftGesture);
            var rightHand = new HandSample(userVisible, userConfidence, rightHandViewportPosition, rightGesture);
            frame = new HandTrackingFrame(
                userVisible,
                SimulatedUserId,
                userConfidence,
                new Rect(0.3f, 0.12f, 0.4f, 0.76f),
                leftHand,
                rightHand,
                Time.unscaledTimeAsDouble);
            return true;
        }
    }
}
