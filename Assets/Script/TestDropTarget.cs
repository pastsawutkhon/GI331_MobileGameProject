
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class TestDropTarget : PressAndDragBase
    {
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private string requiredItemName = "Coba";
        [SerializeField] private string requiredTargetTag = "DropTarget";
        [SerializeField] private Color successColor = Color.green;

        private Image targetImage;

        protected override void Initialize()
        {
            base.Initialize();

            GameObject target = GameObject.FindWithTag(requiredTargetTag);

            if (target != null)
            {
                targetImage = target.GetComponent<Image>();
            }
        }

        protected override bool TryGetDragTarget(
            Vector2 screenPosition, out Image image)
        {
            image = null;

            if (inventoryUI == null)
                return false;

            foreach (var hit in RaycastUI(screenPosition))
            {
                int index = inventoryUI.GetSlotIndex(hit.gameObject);

                if (index < 0 || !inventoryUI.HasItemInSlot(index))
                    continue;

                image = inventoryUI.slotImages[index];
                return true;
            }

            return false;
        }

        protected override bool OnDrop(GameObject draggedObject, Vector2 screenPosition)
        {
            if (inventoryUI == null || inventoryUI.inventory == null)
                return false;

            int index = inventoryUI.GetSlotIndex(draggedObject);

            if (index < 0 || !inventoryUI.HasItemInSlot(index))
                return false;

            string itemName = inventoryUI.inventory.slots[index].itemName;

            if (itemName != requiredItemName)
                return false;

            foreach (var hit in RaycastUI(screenPosition))
            {
                Transform target = hit.gameObject.transform;

                while (target != null)
                {
                    if (target.CompareTag(requiredTargetTag))
                    {
                        if (targetImage != null)
                            targetImage.color = successColor;

                        inventoryUI.UseSlot(index);
                        inventoryUI.RefreshUI();

                        return true;
                    }

                    target = target.parent;
                }
            }

            return false;
        }
    }
}
