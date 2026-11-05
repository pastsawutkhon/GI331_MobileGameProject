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

        public static void Begin(string sceneName, Inventory inventory, bool cameraActive)
        {
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
            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null && items != null) inventory.RestoreSlots(items);
            var cameraSlot = FindFirstObjectByType<CameraSlot>();
            if (nightVision && cameraSlot != null && !cameraSlot.IsCameraActive)
                cameraSlot.ToggleCamera();
            yield return Fade(1f, 0f);
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
