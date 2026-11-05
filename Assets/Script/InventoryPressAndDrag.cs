using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Solution
{
    public class InventoryPressAndDrag : PressAndDragBase
    {
        [Header("Inventory")]
        [SerializeField] private InventoryUI inventoryUI;

        protected override bool OnPressed(Vector2 screenPosition)
        {
            int slotIndex = PickInventorySlot(screenPosition);

            if (slotIndex >= 0)
            {
                return inventoryUI == null || !inventoryUI.HasItemInSlot(slotIndex);
            }

            PickupItem uiPickup = PickUI<PickupItem>(screenPosition);
            if (uiPickup != null)
            {
                uiPickup.TryPickup();
                return true;
            }

            PickupItem worldPickup = PickWorld<PickupItem>(screenPosition);
            if (worldPickup != null)
            {
                worldPickup.TryPickup();
                return true;
            }

            return false;
        }

        protected override bool TryGetDragTarget(Vector2 screenPosition, out Image image)
        {
            image = null;

            if (inventoryUI == null || inventoryUI.slotImages == null)
                return false;

            int slotIndex = PickInventorySlot(screenPosition);

            if (slotIndex < 0 || !inventoryUI.HasItemInSlot(slotIndex) || slotIndex >= inventoryUI.slotImages.Length)
                return false;

            image = inventoryUI.slotImages[slotIndex];
            return image != null && image.enabled;
        }

        // --- ส่วนที่เพิ่ม: ตรวจสอบการนำไอเทมไปวางใส่เป้าหมาย ---
        protected override bool OnDrop(GameObject draggedObject, Vector2 screenPosition)
        {
            if (inventoryUI == null || inventory == null) return false;

            // หา Index ของช่องที่ถูกลาก
            int slotIndex = -1;
            for (int i = 0; i < inventoryUI.slotImages.Length; i++)
            {
                if (inventoryUI.slotImages[i] != null && inventoryUI.slotImages[i].gameObject == draggedObject)
                {
                    slotIndex = i;
                    break;
                }
            }

            if (slotIndex < 0 || !inventoryUI.HasItemInSlot(slotIndex)) return false;

            string draggedItemName = inventoryUI.inventory.slots[slotIndex].itemName;

            // เช็คว่าวางลงบน UI ที่รับไอเทมหรือไม่
            IItemReceiver uiReceiver = PickUI<IItemReceiver>(screenPosition);
            if (uiReceiver != null && uiReceiver.CanReceiveItem(draggedItemName))
            {
                uiReceiver.OnReceiveItem(draggedItemName);
                inventoryUI.UseSlot(slotIndex); // ลดจำนวน/ลบไอเทมออกจากกระเป๋า
                return true;
            }

            // เช็คว่าวางลงบน 2D Collider ในฉากที่รับไอเทมหรือไม่
            IItemReceiver worldReceiver = PickWorld<IItemReceiver>(screenPosition);
            if (worldReceiver != null && worldReceiver.CanReceiveItem(draggedItemName))
            {
                worldReceiver.OnReceiveItem(draggedItemName);
                inventoryUI.UseSlot(slotIndex);
                return true;
            }

            return false;
        }

        protected override bool ShouldRestorePosition(GameObject target, bool dropSucceeded)
        {
            return true;
        }

        private int PickInventorySlot(Vector2 screenPosition)
        {
            if (inventoryUI == null || EventSystem.current == null)
                return -1;

            RaycastUI(screenPosition);

            foreach (RaycastResult hit in uiHits)
            {
                int index = inventoryUI.GetSlotIndex(hit.gameObject);
                if (index >= 0)
                    return index;
            }

            return -1;
        }

        private Inventory inventory => inventoryUI != null ? inventoryUI.inventory : null;
    }
}