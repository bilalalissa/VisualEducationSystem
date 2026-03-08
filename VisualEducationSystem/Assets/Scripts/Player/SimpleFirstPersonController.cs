using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualEducationSystem.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleFirstPersonController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSensitivity = 0.12f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller = null!;
        private Camera playerCamera = null!;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            EnsureCamera();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            UpdateLook();
            UpdateMove();
        }

        private void EnsureCamera()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                return;
            }

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
        }

        private void UpdateLook()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var delta = mouse.delta.ReadValue() * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);

            transform.Rotate(Vector3.up * delta.x);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMove()
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;

            Vector3 move = transform.right * input.x + transform.forward * input.y;
            move = move.normalized * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }
    }
}
