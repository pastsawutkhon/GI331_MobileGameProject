using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Solution
{
    public class DoorLockReceiver : InventoryDropTarget, IPointerClickHandler
    {
        [SerializeField] private string requiredKeyName = "Key";
        [SerializeField] private GameObject operatingRoomGroup;
        [SerializeField] private GameObject evidenceRoomGroup;
        [SerializeField] private Image background;
        [SerializeField] private WorldInteractionHandler navigation;
        private static bool unlocked;

        internal static void ResetLock() => unlocked = false;

        private void OnEnable() => SetOpening(unlocked ? 1f : 0f);
        private void OnDisable() => StopAllCoroutines();

        public override bool TryUse(InventoryUI inventoryUI, int slotIndex)
        {
            if (unlocked || !navigation.CanUnlockDoor || !inventoryUI.HasItemInSlot(slotIndex) ||
                inventoryUI.inventory.slots[slotIndex].itemName != requiredKeyName) return false;
            if (!inventoryUI.inventory.UseItem(slotIndex)) return false;
            unlocked = true;
            StartCoroutine(Open());
            return true;
        }

        private IEnumerator Open()
        {
            for (float time = 0; time < .4f; time += Time.unscaledDeltaTime)
            {
                SetOpening(Mathf.SmoothStep(0f, 1f, time / .4f));
                yield return null;
            }
            SetOpening(1);
        }

        private void SetOpening(float amount)
        {
            if (background && background.material.HasProperty("_DoorOpen"))
                background.material.SetFloat("_DoorOpen", amount);
        }

        public void OnPointerClick(PointerEventData tap)
        {
            if (!unlocked || !navigation.CanUnlockDoor || tap.button != PointerEventData.InputButton.Left) return;
            background.gameObject.SetActive(false);
            operatingRoomGroup.SetActive(false);
            evidenceRoomGroup.SetActive(true);
        }
    }
}
