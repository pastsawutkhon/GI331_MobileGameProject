using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Solution
{
    public class PressAndDrag : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private InventoryUI inventoryUI;   // เพิ่มใหม่: ลาก Invent ใส่ช่องนี้ใน Inspector

        public event Action<GameObject> DragStarted;
        public event Action<GameObject, Vector2> DragEnded;

        private InputActionMap touchMap;
        private InputAction pressAction;
        private InputAction positionAction;

        private Camera cam;
        private bool isDragging;

        private readonly List<RaycastResult> uiHits =
            new List<RaycastResult>();

        private void Awake()
        {
            cam = Camera.main;

            touchMap = inputActions.FindActionMap("Touch", true);
            pressAction = touchMap.FindAction("TouchPress", true);
            positionAction = touchMap.FindAction("TouchPosition", true);
        }

        private void OnEnable()
        {
            pressAction.performed += TouchPressed;
            touchMap.Enable();
        }

        private void OnDisable()
        {
            pressAction.performed -= TouchPressed;
            touchMap.Disable();
        }

        private void TouchPressed(InputAction.CallbackContext context)
        {
            if (isDragging) return;

            Vector2 pos = positionAction.ReadValue<Vector2>();

            // 1) เช็คช่อง inventory ก่อน ถ้ามีของให้เริ่มลาก
            int slotIndex = PickInventorySlot(pos);

            if (slotIndex >= 0)
            {
                if (inventoryUI.HasItemInSlot(slotIndex))
                    StartCoroutine(DragSlot(inventoryUI, slotIndex, pos));

                // กดโดนช่อง inventory แล้ว ไม่ต้องไปเช็คอย่างอื่นต่อ
                return;
            }

            // 2) หลักฐานที่เป็น UI (Image บน Canvas)
            GameObject target = PickUI(pos);

            if (target != null)
            {
                PickupItem uiPickup =
                    target.GetComponentInParent<PickupItem>();

                if (uiPickup != null)
                    uiPickup.TryPickup();

                return;
            }

            // 3) หลักฐานในฉาก 2D/3D
            PickupItem pickup = PickWorld(pos);

            if (pickup != null)
                pickup.TryPickup();
        }

        // หาว่ากดโดนช่อง inventory ช่องไหน (คืน -1 ถ้าไม่โดน)
        private int PickInventorySlot(Vector2 pos)
        {
            if (EventSystem.current == null || inventoryUI == null)
                return -1;

            uiHits.Clear();

            EventSystem.current.RaycastAll(
                new PointerEventData(EventSystem.current)
                {
                    position = pos
                },
                uiHits
            );

            foreach (RaycastResult hit in uiHits)
            {
                int index = inventoryUI.GetSlotIndex(hit.gameObject);

                if (index >= 0)
                    return index;
            }

            return -1;
        }

        private GameObject PickUI(Vector2 pos)
        {
            if (EventSystem.current == null)
                return null;

            uiHits.Clear();

            EventSystem.current.RaycastAll(
                new PointerEventData(EventSystem.current)
                {
                    position = pos
                },
                uiHits
            );

            foreach (RaycastResult hit in uiHits)
            {
                PickupItem pickup =
                    hit.gameObject.GetComponentInParent<PickupItem>();

                if (pickup != null)
                    return hit.gameObject;
            }

            return null;
        }

        private PickupItem PickWorld(Vector2 pos)
        {
            if (cam == null)
                return null;

            Ray ray = cam.ScreenPointToRay(pos);

            // ตรวจวัตถุ 3D
            if (Physics.Raycast(ray, out RaycastHit hit3D))
            {
                PickupItem pickup =
                    hit3D.collider.GetComponentInParent<PickupItem>();

                if (pickup != null)
                    return pickup;
            }

            // ตรวจวัตถุ 2D
            RaycastHit2D hit2D =
                Physics2D.GetRayIntersection(ray);

            if (hit2D.collider != null)
            {
                PickupItem pickup =
                    hit2D.collider.GetComponentInParent<PickupItem>();

                if (pickup != null)
                    return pickup;
            }

            return null;
        }

        private IEnumerator DragSlot(
            InventoryUI ui,
            int index,
            Vector2 startPos)
        {
            if (ui == null ||
                ui.slotImages == null ||
                index < 0 ||
                index >= ui.slotImages.Length)
                yield break;

            Image image = ui.slotImages[index];

            if (image == null)
                yield break;

            RectTransform rect = image.rectTransform;
            Vector3 originalPosition = rect.position;

            Canvas canvas = image.GetComponentInParent<Canvas>();

            if (canvas == null)
                yield break;

            Camera uiCam =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            isDragging = true;

            GameObject draggedItem = image.gameObject;
            DragStarted?.Invoke(draggedItem);

            while (pressAction.IsPressed())
            {
                Vector2 pos = positionAction.ReadValue<Vector2>();

                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rect.parent as RectTransform,
                    pos,
                    uiCam,
                    out Vector3 worldPosition
                );

                rect.position = worldPosition;

                yield return null;
            }

            Vector2 endPos = positionAction.ReadValue<Vector2>();

            // คืนไอคอนเข้าช่องเดิม
            rect.position = originalPosition;

            isDragging = false;

            DragEnded?.Invoke(draggedItem, endPos);
        }
    }
}