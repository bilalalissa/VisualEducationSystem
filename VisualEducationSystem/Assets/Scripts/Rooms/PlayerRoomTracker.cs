using UnityEngine;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Rooms
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerRoomTracker : MonoBehaviour
    {
        [SerializeField] private CurrentRoomHUD roomHud = null!;
        public RoomInstance? CurrentRoom { get; private set; }

        private void Start()
        {
            if (roomHud != null)
            {
                roomHud.SetCurrentRoom("Entry Hall");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var roomZone = other.GetComponent<RoomZone>();
            if (roomZone == null || roomHud == null)
            {
                return;
            }

            SetCurrentRoom(roomZone.RoomInstance, roomZone.RoomDisplayName);
        }

        public void SetCurrentRoom(RoomInstance? roomInstance, string displayName)
        {
            CurrentRoom = roomInstance;
            if (roomHud != null)
            {
                roomHud.SetCurrentRoom(displayName);
            }
        }
    }
}
