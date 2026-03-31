#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomPortal : MonoBehaviour
    {
        private PrototypePalaceBootstrap bootstrap = null!;
        private string destinationRoomId = string.Empty;
        private float nextUseTime;

        public void Initialize(PrototypePalaceBootstrap palaceBootstrap, string targetRoomId)
        {
            bootstrap = palaceBootstrap;
            destinationRoomId = targetRoomId;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < nextUseTime)
            {
                return;
            }

            if (other.GetComponent<VisualEducationSystem.Player.SimpleFirstPersonController>() == null
                && other.GetComponent<PlayerRoomTracker>() == null)
            {
                return;
            }

            if (bootstrap.TryTeleportToRoom(destinationRoomId))
            {
                nextUseTime = Time.time + 0.5f;
            }
        }

        private void Reset()
        {
            var boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
    }
}
