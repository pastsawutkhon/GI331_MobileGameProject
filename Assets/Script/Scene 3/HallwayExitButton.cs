using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class HallwayExitButton : MonoBehaviour
    {
        [Header("ลากของที่มีอยู่แล้วมาใส่เพื่อเช็กเงื่อนไข")]
        [SerializeField] private GameObject brushDoneImage; // ลาก Image (2) มาใส่ (เช็กว่าใช้แปรงเสร็จยัง)
        [SerializeField] private GameObject enterBathroomBtn; // ลากปุ่ม '<' มาใส่ (เช็กว่าอยู่โถงไหม)

        [Header("ปุ่มเปลี่ยนซีน")]
        [SerializeField] private Button exitSceneButton; // ลากปุ่มไปซีนถัดไปมาใส่
        [SerializeField] private string destinationScene = "scene 4"; // พิมพ์ชื่อซีนถัดไป

        private bool isTransitioning = false;

        private void Start()
        {
            if (exitSceneButton != null)
            {
                exitSceneButton.gameObject.SetActive(false); // เริ่มเกมซ่อนปุ่มไว้ก่อน
                exitSceneButton.onClick.AddListener(GoToNextScene);
            }
        }

        private void Update()
        {
            if (exitSceneButton == null || brushDoneImage == null || enterBathroomBtn == null) return;

            // เงื่อนไข: รูปหลังปัดแปรงเปิดอยู่ (activeSelf) + ตอนนี้อยู่ที่โถง (ปุ่มเข้าห้องน้ำเปิดอยู่)
            bool canShow = brushDoneImage.activeSelf && enterBathroomBtn.activeSelf;

            if (exitSceneButton.gameObject.activeSelf != canShow)
            {
                exitSceneButton.gameObject.SetActive(canShow);
            }
        }

        private void GoToNextScene()
        {
            if (isTransitioning || string.IsNullOrEmpty(destinationScene)) return;
            isTransitioning = true;

            var inventory = FindFirstObjectByType<Inventory>();
            var cameraSlot = FindFirstObjectByType<CameraSlot>();
            bool isCameraOn = (cameraSlot != null && cameraSlot.IsCameraActive);

            SceneEntranceFade.Begin(destinationScene, inventory, isCameraOn);
        }
    }
}