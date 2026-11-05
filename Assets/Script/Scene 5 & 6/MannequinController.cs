using UnityEngine;

namespace Solution
{
    public class MannequinController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int totalOrgans = 3;
        [SerializeField] private GameObject hiddenKeyObject;
        [SerializeField] private Collider2D keyCollider;

        [Header("References")]
        [SerializeField] private CameraSlot cameraSlot;

        private int removedCount = 0;

        private void Start()
        {
            if (keyCollider != null)
                keyCollider.enabled = false; // ปิดไม่ให้แตะโดนกุญแจทะลุอวัยวะ
        }

        public void OnOrganRemoved()
        {
            removedCount++;
            CheckUnlockKey();
        }

        private void CheckUnlockKey()
        {
            // ถอดอวัยวะหมดแล้ว ให้เปิด Collider กุญแจเพื่อให้แตะเก็บได้
            if (removedCount >= totalOrgans)
            {
                if (keyCollider != null)
                    keyCollider.enabled = true;
            }
        }
    }
}