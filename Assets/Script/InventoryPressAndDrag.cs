
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
                // ช่องว่าง: จบการกดตรงนี้ ไม่ไปเก็บของด้านหลัง
                // ช่องมีของ: ให้คลาสแม่ตรวจต่อว่าสามารถลากได้ไหม
                return inventoryUI == null ||
                       !inventoryUI.HasItemInSlot(slotIndex);
            }

            // ตรวจ UI ก่อนตามลำดับเดิม
            PickupItem uiPickup = PickUI<PickupItem>(
                screenPosition
            );

            if (uiPickup != null)
            {
                uiPickup.TryPickup();
                return true;
            }

            // ถ้าไม่โดน UI ที่เก็บของได้ ให้ตรวจฉาก
            PickupItem worldPickup = PickWorld<PickupItem>(
                screenPosition
            );

            if (worldPickup != null)
            {
                worldPickup.TryPickup();
                return true;
            }

            return false;
        }

        protected override bool TryGetDragTarget(
            Vector2 screenPosition,
            out Image image)
        {
            image = null;

            if (inventoryUI == null ||
                inventoryUI.slotImages == null)
            {
                return false;
            }

            int slotIndex = PickInventorySlot(screenPosition);

            if (slotIndex < 0 ||
                !inventoryUI.HasItemInSlot(slotIndex) ||
                slotIndex >= inventoryUI.slotImages.Length)
            {
                return false;
            }

            image = inventoryUI.slotImages[slotIndex];

            return image != null && image.enabled;
        }

        // Inventory ใช้ Image เป็นภาพไอคอนประจำช่อง
        // จึงต้องคืนภาพกลับช่องเดิม ไม่ว่าจะปล่อยสำเร็จหรือไม่
        protected override bool ShouldRestorePosition(
            GameObject target,
            bool dropSucceeded)
        {
            return true;
        }

        private int PickInventorySlot(Vector2 screenPosition)
        {
            if (inventoryUI == null ||
                EventSystem.current == null)
            {
                return -1;
            }

            RaycastUI(screenPosition);

            foreach (RaycastResult hit in uiHits)
            {
                int index = inventoryUI.GetSlotIndex(
                    hit.gameObject
                );

                if (index >= 0)
                    return index;
            }

            return -1;
        }
    }
}
