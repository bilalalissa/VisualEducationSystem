using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VisualEducationSystem.Core
{
    public sealed class ReturnToMenuHotkey : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "MainMenu";

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }
    }
}
