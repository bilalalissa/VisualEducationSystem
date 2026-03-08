using UnityEngine;
using UnityEngine.SceneManagement;

namespace VisualEducationSystem.Core
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "MainMenu";

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError("BootstrapLoader target scene is empty.", this);
                return;
            }

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
