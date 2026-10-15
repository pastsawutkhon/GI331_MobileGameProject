
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class InventoryUI : MonoBehaviour
    {
        public Inventory inventory;
        public Image[] slotImages;

        private void Start()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged += RefreshUI;
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshUI;
        }

        public void RefreshUI()
        {
            if (inventory == null) return;

            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] == null) continue;

                if (i >= inventory.slots.Count)
                {
                    slotImages[i].sprite = null;
                    slotImages[i].enabled = false;
                    continue;
                }

                InventorySlot slot = inventory.slots[i];

                slotImages[i].sprite = slot.icon;
                slotImages[i].enabled = !slot.IsEmpty();
                slotImages[i].preserveAspect = true;
            }
        }

        public int GetSlotIndex(GameObject target)
        {
            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] == null) continue;

                if (target == slotImages[i].gameObject ||
                    target.transform.IsChildOf(slotImages[i].transform))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool HasItemInSlot(int index)
        {
            return inventory != null &&
                   index >= 0 &&
                   index < inventory.slots.Count &&
                   !inventory.slots[index].IsEmpty();
        }

        public void UseSlot(int index)
        {
            if (inventory != null)
                inventory.UseItem(index);
        }
    }
}
