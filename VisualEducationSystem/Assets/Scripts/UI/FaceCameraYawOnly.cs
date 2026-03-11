using UnityEngine;

namespace VisualEducationSystem.UI
{
    public sealed class FaceCameraYawOnly : MonoBehaviour
    {
        private Camera? targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }
            }

            var targetPosition = targetCamera.transform.position;
            targetPosition.y = transform.position.y;
            var direction = targetPosition - transform.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
