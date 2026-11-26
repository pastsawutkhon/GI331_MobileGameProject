using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Solution
{
    public class UsePowderAndBrush : PressAndDragBase
    {
        [Header("Inventory Settings")]
        [SerializeField] private InventoryUI inventoryUI;

        [Header("Step 1: Black Powder")]
        [SerializeField] private string step1ItemName = "Powder";
        [SerializeField] private GameObject step1UIToShow;

        [Header("Step 2: Makeup Brush")]
        [SerializeField] private string step2ItemName = "Brush";
        [SerializeField] private GameObject step2UIToShow;
        [SerializeField] private AudioSource audioWhisper;

        [Header("Target Settings")]
        [SerializeField] private string requiredTargetTag = "DropTarget";
        [SerializeField] private bool consumeItemOnDrop = true;

        private enum Step { Powder, Brush, Completed }
        private Step currentStep = Step.Powder;

        private CameraSlot nightCamera;
        private bool sawMirror;
        private void LateUpdate()
        {
            if (sawMirror || currentStep != Step.Powder || !mirrorRoom || !mirrorRoom.activeInHierarchy) return;
            if (!nightCamera) nightCamera = FindAnyObjectByType<CameraSlot>();
            if (!nightCamera || nightCamera.NightVisionOpacity < .8f) return;
            sawMirror = true;
            ChapterDialogue.Show("S03_PAT_01");
        }

        [SerializeField] private GameObject mirrorRoom;

        protected override bool TryGetDragTarget(Vector2 screenPosition, out Image image)
        {
            image = null;
            if (inventoryUI == null || inventoryUI.inventory == null || currentStep == Step.Completed)
                return false;

            string requiredItem = (currentStep == Step.Powder) ? step1ItemName : step2ItemName;

            foreach (var hit in RaycastUI(screenPosition))
            {
                int index = inventoryUI.GetSlotIndex(hit.gameObject);
                if (index < 0 || !inventoryUI.HasItemInSlot(index)) continue;

                if (inventoryUI.inventory.slots[index].itemName == requiredItem)
                {
                    image = inventoryUI.slotImages[index];
                    return true;
                }
            }
            return false;
        }

        protected override bool OnDrop(GameObject draggedObject, Vector2 screenPosition)
        {
            if (inventoryUI == null || inventoryUI.inventory == null || currentStep == Step.Completed)
                return false;

            int index = inventoryUI.GetSlotIndex(draggedObject);
            if (index < 0 || !inventoryUI.HasItemInSlot(index)) return false;

            string currentItemName = inventoryUI.inventory.slots[index].itemName;
            string requiredItem = (currentStep == Step.Powder) ? step1ItemName : step2ItemName;
            if (currentItemName != requiredItem) return false;

            foreach (var hit in RaycastUI(screenPosition))
            {
                Transform target = hit.gameObject.transform;
                while (target != null)
                {
                    if (target.CompareTag(requiredTargetTag))
                    {
                        if (currentStep == Step.Powder)
                        {
                            if (step1UIToShow != null) step1UIToShow.SetActive(true);
                            currentStep = Step.Brush;
                            ChapterDialogue.Show("S03_PAT_02");
                        }
                        else if (currentStep == Step.Brush)
                        {
                            if (step2UIToShow != null) step2UIToShow.SetActive(true);
                            currentStep = Step.Completed;
                            ChapterDialogue.Show("S03_PAT_03", "S03_PAT_04");

                            audioWhisper.Play();
                        }

                        if (consumeItemOnDrop)
                        {
                            inventoryUI.UseSlot(index);
                            inventoryUI.RefreshUI();
                        }
                        return true;
                    }
                    target = target.parent;
                }
            }
            return false;
        }
    }
}