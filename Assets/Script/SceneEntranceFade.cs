using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Solution
{
    public class SceneEntranceFade : MonoBehaviour
    {
        private CanvasGroup overlay;
        private List<InventorySlot> items;
        private bool nightVision;
        private string destination;
        private System.Action onArrival;
        public static bool InProgress { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransition() => InProgress = false;

        public static void Begin(string sceneName, Inventory inventory, bool cameraActive, System.Action onArrival = null)
        {
            if (InProgress) return;
            InProgress = true;
            var root = new GameObject("Scene Passage Fade", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(SceneEntranceFade));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var runner = root.GetComponent<SceneEntranceFade>();
            runner.overlay = root.GetComponent<CanvasGroup>();
            runner.overlay.alpha = 0f;
            var imageGO = new GameObject("Black Fade", typeof(RectTransform), typeof(Image));
            imageGO.transform.SetParent(root.transform, false);
            var image = imageGO.GetComponent<Image>();
            image.color = Color.black;
            image.canvasRenderer.cullTransparentMesh = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            runner.onArrival = onArrival;
            runner.destination = sceneName;
            runner.nightVision = cameraActive;
            if (inventory != null)
            {
                runner.items = new List<InventorySlot>();
                foreach (var slot in inventory.slots)
                    runner.items.Add(new InventorySlot
                    { itemName = slot.itemName, icon = slot.icon, amount = slot.amount });
            }
            runner.StartCoroutine(runner.CrossScene());
        }

        private IEnumerator CrossScene()
        {
            yield return Fade(0f, 1f);
            yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
            // Allow destination UI subscriptions to finish before restoring the inventory.
            yield return null;
            var inventoryUI = FindAnyObjectByType<InventoryUI>();
            var inventory = inventoryUI ? inventoryUI.inventory : FindAnyObjectByType<Inventory>();
            if (inventory != null && items != null) inventory.RestoreSlots(items);
            if (destination == "Scene 4")
            {
                foreach (var document in FindObjectsByType<Document>()) document.SyncCarriedDocument();
            }
            var cameraSlot = FindFirstObjectByType<CameraSlot>();
            if (nightVision && cameraSlot != null && !cameraSlot.IsCameraActive)
                cameraSlot.ToggleCamera();
            onArrival?.Invoke();
            yield return Fade(1f, 0f);
            InProgress = false;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                overlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            overlay.alpha = to;
        }
    }
}
