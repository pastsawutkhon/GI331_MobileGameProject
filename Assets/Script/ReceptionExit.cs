using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class ReceptionExit : MonoBehaviour
    {
        private Inventory inventory;
        private CameraSlot cameraSlot;
        private MorseKeypadLock locker;
        private CanvasGroup group;
        private GameObject overlay;
        private bool leaving;
        private const string Destination = "scene 3";

        private void Start()
        {
            var ui = FindFirstObjectByType<InventoryUI>();
            inventory = ui != null ? ui.inventory : null;
            cameraSlot = FindFirstObjectByType<CameraSlot>();
            locker = GetComponent<MorseKeypadLock>();

            overlay = new GameObject("Reception Exit UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            // A root overlay uses screen coordinates independently of the locker canvas.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlay, gameObject.scene);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(810, 1440);
            scaler.matchWidthOrHeight = 0.5f;

            var target = new GameObject("Continue to Scene 3", typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(CanvasGroup));
            target.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)target.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.138f, 0.55f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(76, 70);
            target.GetComponent<Image>().color = Color.clear;
            var button = target.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Continue);
            group = target.GetComponent<CanvasGroup>();
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = false;

            var plate = new GameObject("Arrow Plate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(rect, false);
            ((RectTransform)plate.transform).sizeDelta = new Vector2(51, 41);
            var plateImage = plate.GetComponent<Image>();
            plateImage.color = new Color(0.22f, 0.24f, 0.19f, 0.92f);
            plateImage.raycastTarget = false;
            for (int i = 0; i < 2; i++)
            {
                var stroke = new GameObject("Chevron Stroke", typeof(RectTransform), typeof(Image));
                stroke.transform.SetParent(plate.transform, false);
                var line = (RectTransform)stroke.transform;
                line.sizeDelta = new Vector2(12, 3.5f);
                line.anchoredPosition = new Vector2(i == 0 ? -4.5f : 4.5f, 0);
                line.localRotation = Quaternion.Euler(0, 0, i == 0 ? 33f : -33f);
                var ink = stroke.GetComponent<Image>();
                ink.color = new Color(0.81f, 0.83f, 0.75f);
                ink.raycastTarget = false;
            }
        }

        private bool Ready => !leaving && locker != null && locker.IsUnlocked && inventory != null &&
            inventory.HasItem("Patient Binder", 1) && inventory.HasItem("Lighter", 1);

        private void Update()
        {
            if (group == null) return;
            bool show = Ready && !ReturnableNote.IsInspecting;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime / 0.45f);
            group.interactable = group.blocksRaycasts = show && group.alpha >= 0.99f;
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
        }

        private void Continue()
        {
            if (!Ready || ReturnableNote.IsInspecting || !Application.CanStreamedLevelBeLoaded(Destination)) return;
            leaving = true;
            SceneEntranceFade.Begin(Destination, inventory, cameraSlot != null && cameraSlot.IsCameraActive);
        }
    }
}
