using UnityEngine;
using VisualEducationSystem.UI;

namespace VisualEducationSystem.Rooms
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerRoomTracker : MonoBehaviour
    {
        [SerializeField] private CurrentRoomHUD roomHud = null!;

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

            roomHud.SetCurrentRoom(roomZone.RoomDisplayName);
        }
    }
}
