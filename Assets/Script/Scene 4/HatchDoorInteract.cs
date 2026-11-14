using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Solution
{
    public class HatchDoorInteract : PressAndDragBase
    {

        [Header("Navigation Reference")]
        [SerializeField] private Scene4PictureNavigation sceneNavigation;

        [Header("Background Image")]
        [SerializeField] private Image roomBackgroundImage;

        [Header("Hatch Sprites")]
        [SerializeField] private Sprite closedHatchSprite;
        [SerializeField] private Sprite openHatchSprite;
        [SerializeField] private Graphic hatchTouchZone;

        [Header("Audio & Events")]
        [SerializeField] private AudioSource openSound;
        public UnityEvent OnHatchOpened;

        private bool isOpened = false;

        protected override void Initialize()
        {
            base.Initialize();

            // ตั้งค่าเริ่มต้นให้เป็นรูป Sprite ฝาปิด
            if (roomBackgroundImage != null && closedHatchSprite != null)
            {
                roomBackgroundImage.sprite = closedHatchSprite;
            }
        }

        protected override bool OnPressed(Vector2 screenPosition)
        {
            if (isOpened) return false;

            if (sceneNavigation != null && sceneNavigation.CurrentView != -1)
            {
                return false;
            }

            // เช็กว่านิ้วที่แตะหน้าจอมือถือ โดนพื้นที่ฝา (hatchTouchZone) หรือไม่
            List<RaycastResult> hits = RaycastUI(screenPosition);
            foreach (var hit in hits)
            {
                if (hatchTouchZone != null && hit.gameObject == hatchTouchZone.gameObject)
                {
                    OpenHatch();
                    return true; // คืนค่า true เพื่อบอกว่าจัดการการแตะนี้แล้ว
                }
            }

            return base.OnPressed(screenPosition);
        }

        public void OpenHatch()
        {
            if (isOpened) return;
            isOpened = true;

            // สลับ Sprite ของพื้นหลังเป็นรูปฝาเปิดทันที
            if (roomBackgroundImage != null && openHatchSprite != null)
            {
                roomBackgroundImage.sprite = openHatchSprite;
            }

            // เล่นเสียงเปิดฝา (ถ้ามี)
            if (openSound != null)
            {
                openSound.Play();
            }

            // ปิดการรับการแตะตรงฝา ไม่ให้แตะซ้ำ
            if (hatchTouchZone != null)
            {
                hatchTouchZone.raycastTarget = false;
            }

            Debug.Log("แตะเปิดฝาทางลงใต้ดินแล้ว!");
            OnHatchOpened?.Invoke();
        }
    }
}