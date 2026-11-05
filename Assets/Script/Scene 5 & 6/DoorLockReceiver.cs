using UnityEngine;

namespace Solution
{
    public class DoorLockReceiver : MonoBehaviour, IItemReceiver
    {
        [SerializeField] private string requiredKeyName = "Key";
        [SerializeField] private GameObject operatingRoomGroup;
        [SerializeField] private GameObject evidenceRoomGroup;

        public bool CanReceiveItem(string itemName)
        {
            return itemName == requiredKeyName;
        }

        public void OnReceiveItem(string itemName)
        {
            Debug.Log("Door Unlocked!");
            // สลับการแสดงผลไปยังห้องหลักฐาน
            if (operatingRoomGroup != null) operatingRoomGroup.SetActive(false);
            if (evidenceRoomGroup != null) evidenceRoomGroup.SetActive(true);
        }
    }
}