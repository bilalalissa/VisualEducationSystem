using UnityEngine;

namespace VisualEducationSystem.Rooms
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomZone : MonoBehaviour
    {
        [SerializeField] private string roomDisplayName = "Room";
        private RoomInstance? roomInstance;

        public string RoomDisplayName => roomInstance != null ? roomInstance.RoomDisplayName : roomDisplayName;
        public RoomInstance? RoomInstance => roomInstance;

        public void SetDisplayName(string displayName)
        {
            roomDisplayName = displayName;
        }

        public void BindRoom(RoomInstance instance)
        {
            roomInstance = instance;
            roomDisplayName = instance.RoomDisplayName;
        }

        private void Reset()
        {
            var boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
    }
}
