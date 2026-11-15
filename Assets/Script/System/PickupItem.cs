
using UnityEngine;

namespace Solution
{
    public class PickupItem : MonoBehaviour
    {
        public string itemName;
        public Sprite itemIcon;
        public int amount = 1;

        public Inventory inventory;

        public void TryPickup()
        {
            if (inventory == null)
            {
                Debug.LogWarning("Inventory not assigned!");
                return;
            }

            bool added = inventory.AddItem(
                itemName,
                itemIcon,
                amount
            );

            // เก็บสำเร็จเท่านั้น ถึงจะทำให้ของหาย
            if (added)
                Destroy(gameObject);
        }
    }
}
