using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomZone : MonoBehaviour
    {
        [SerializeField] private string roomDisplayName = "Room";

        public string RoomDisplayName => roomDisplayName;

        public void SetDisplayName(string displayName)
        {
            roomDisplayName = displayName;
        }

        private void Reset()
        {
            var boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
    }
}
