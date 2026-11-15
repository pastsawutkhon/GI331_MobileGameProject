using UnityEngine;

namespace Solution
{
    public abstract class InventoryDropTarget : MonoBehaviour
    {
        public abstract bool TryUse(InventoryUI inventoryUI, int slotIndex);
    }
}
