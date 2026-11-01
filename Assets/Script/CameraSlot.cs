using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


namespace Solution
{
    public class CameraSlot : PressAndDragBase
    {
        [Header("Camera Slot Components")]
        [SerializeField] private Image slotImage;
        [SerializeField] private CameraSlot cameraSlot;

        [Header("globalVolume slot Components")]
        [SerializeField] private Volume globalVolume;

        [Header("Night Vision Secret Marks")]
        [SerializeField] private GameObject nightVisionMarks;

        [Header("Camera Status")]
        [SerializeField] private bool isCameraActive = false;

        public event Action<bool> OnCameraToggled;

        public bool IsCameraActive => isCameraActive;

        protected override void Initialize()
        {
            base.Initialize();

            if (slotImage == null)
                slotImage = GetComponent<Image>();

            if (slotImage != null)
            {
                slotImage.enabled = true;
            }

            // ซ่อนร่องรอยไว้ตั้งแต่เริ่มเกม
            if (nightVisionMarks != null)
            {
                nightVisionMarks.SetActive(false);
            }
        }

        // Override การแตะ: ถ้ากดที่ช่องกล้อง ให้เปิด-ปิด Night Vision ทันที
        protected override bool OnPressed(Vector2 screenPosition)
        {
            Image hitImage = PickUI<Image>(screenPosition);

            if (hitImage != null && hitImage.gameObject == gameObject)
            {
                ToggleCamera();
                return true; // คืนค่า true เพื่อไม่ให้ PressAndDragBase ดึงระบบลากมาทำต่อ
            }

            return false;
        }

        // Override การลาก: ป้องกันไม่ให้ลากไอคอนกล้องออกจากช่อง
        protected override bool TryGetDragTarget(Vector2 screenPosition, out Image image)
        {
            image = null;
            return false;
        }

        public void ToggleCamera()
        {
            isCameraActive = !isCameraActive;
            Debug.Log($"Camera State: {(isCameraActive ? "Active (ON)" : "Inactive (OFF)")}");

            // 1. สั่งเปิด-ปิด GameObject ของ Global Volume โดยตรง
            if (globalVolume != null)
            {
                globalVolume.gameObject.SetActive(isCameraActive);
            }

            // 2. สั่งเปิด-ปิด รอยสัญลักษณ์ลับ 
            if (nightVisionMarks != null)
            {
                nightVisionMarks.SetActive(isCameraActive);
            }

            // 2. แจ้งเตือน Event ไปยังระบบอื่น (ถ้ามี)
            OnCameraToggled?.Invoke(isCameraActive);
        }

        public void DeactivateCamera()
        {
            if (isCameraActive)
            {
                isCameraActive = false;

                if (globalVolume != null)
                {
                    globalVolume.gameObject.SetActive(false);
                }

                if (nightVisionMarks != null)
                {
                    nightVisionMarks.SetActive(false);
                }

                OnCameraToggled?.Invoke(false);
            }
        }
    }
}