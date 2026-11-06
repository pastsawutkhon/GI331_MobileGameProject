using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Solution
{
    public class ReturnableSceneItem : InventoryDropTarget, IPointerClickHandler
    {
        [SerializeField] private string itemName;
        [SerializeField] private Image visual;
        [SerializeField] private Sprite inventoryIcon;
        private bool collected;
        public bool IsCollected => collected;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || collected || ReturnableNote.IsInspecting) return;
            var ui = FindFirstObjectByType<InventoryUI>();
            if (ui == null || ui.inventory == null || !ui.inventory.AddItem(itemName, inventoryIcon)) return;
            collected = true;
            visual.gameObject.SetActive(false);
        }

        public override bool TryUse(InventoryUI ui, int slotIndex)
        {
            if (!collected || ReturnableNote.IsInspecting || !ui.HasItemInSlot(slotIndex)
                || ui.inventory.slots[slotIndex].itemName != itemName) return false;
            ui.UseSlot(slotIndex);
            collected = false;
            visual.gameObject.SetActive(true);
            return true;
        }
    }
}
