using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Solution
{
    public class DoorLockReceiver : InventoryDropTarget, IPointerClickHandler
    {
        [SerializeField] private string requiredKeyName = "Key";
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private CameraSlot cameraSlot;
        [SerializeField] private Image background;
        [SerializeField] private WorldInteractionHandler navigation;
        [SerializeField] private AudioClip openingSound;
        private static bool unlocked;

        internal static void ResetLock() => unlocked = false;

        private void Start() => SetOpening(unlocked ? 1f : 0f);
        private void OnEnable() => SetOpening(unlocked ? 1f : 0f);
        private void OnDisable() => StopAllCoroutines();

        public override bool TryUse(InventoryUI inventoryUI, int slotIndex)
        {
            if (unlocked || !navigation.CanUnlockDoor || !inventoryUI.HasItemInSlot(slotIndex) ||
                inventoryUI.inventory.slots[slotIndex].itemName != requiredKeyName) return false;
            if (!inventoryUI.inventory.UseItem(slotIndex)) return false;
            unlocked = true;
            if (openingSound)
            {
                // Let the creak finish even if the player immediately enters the next room.
                var sound = new GameObject("Opening door to Scene 6").AddComponent<AudioSource>();
                DontDestroyOnLoad(sound.gameObject);
                sound.clip = openingSound;
                sound.Play();
                Destroy(sound.gameObject, openingSound.length);
            }
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
            SceneEntranceFade.Begin("Scene 6", inventoryUI.inventory, cameraSlot.IsCameraActive);
        }
    }
}
