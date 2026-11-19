using UnityEngine;

namespace Solution
{
    public sealed class Scene6CameraSlot : InventoryDropTarget
    {
        public Scene6Controller Story { private get; set; }

        public override bool TryUse(InventoryUI ui, int index)
        {
            if (!Story || !Story.CanInsertCard || !ui.HasItemInSlot(index) ||
                ui.inventory.slots[index].itemName != Scene6Controller.CardName) return false;
            if (!ui.inventory.UseItem(index)) return false;
            Story.PlayRecording();
            return true;
        }
    }
}
