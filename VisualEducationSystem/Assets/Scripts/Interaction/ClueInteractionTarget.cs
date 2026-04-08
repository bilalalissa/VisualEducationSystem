#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public sealed class ClueInteractionTarget : MonoBehaviour
    {
        [SerializeField] private string clueId = string.Empty;
        [SerializeField] private string roomId = string.Empty;

        public string ClueId => clueId;
        public string RoomId => roomId;

        public void Initialize(string nextClueId, string nextRoomId)
        {
            clueId = nextClueId;
            roomId = nextRoomId;
        }
    }
}
