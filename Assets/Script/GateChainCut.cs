using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class GateChainCut : InventoryDropTarget
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite openedGate;
        [SerializeField] private GameObject firstPictureMarks;
        [SerializeField] private string requiredItem = "BoltCutters";
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.4f;

        public bool IsCompleted { get; private set; }
        public bool IsBusy { get; private set; }
        public event System.Action Opened;

        private GameObject coverRoot;
        private Image cover;
        private RectTransform cuttingTool;
        private Inventory consumedFrom;
        private Sprite consumedIcon;
        private bool consumed;

        public override bool TryUse(InventoryUI inventoryUI, int slotIndex)
        {
            if (IsBusy || IsCompleted || !isActiveAndEnabled || backgroundImage == null ||
                openedGate == null || inventoryUI == null || !inventoryUI.HasItemInSlot(slotIndex))
                return false;
            var slot = inventoryUI.inventory.slots[slotIndex];
            if (slot.itemName != requiredItem || slot.icon == null)
                return false;

            consumedIcon = slot.icon;
            consumedFrom = inventoryUI.inventory;
            if (!consumedFrom.UseItem(slotIndex))
                return false;
            consumed = true;
            IsBusy = true;
            StartCoroutine(CutAndOpen());
            return true;
        }

        private IEnumerator CutAndOpen()
        {
            CreateCover();
            CreateCuttingTool(out RectTransform left, out RectTransform right);
            float elapsed = 0f;
            while (elapsed < 0.24f)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.24f);
                cuttingTool.anchoredPosition = Vector2.Lerp(new Vector2(-70f, -110f), Vector2.zero, t);
                cuttingTool.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-12f, -24f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Both halves pivot near the jaws so the handles squeeze around the chain.
            elapsed = 0f;
            while (elapsed < 0.85f)
            {
                float t = Mathf.Clamp01(elapsed / 0.85f);
                float pressure = Mathf.SmoothStep(0f, 1f, t);
                left.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-3.5f, 2.2f, pressure));
                right.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(3.5f, -2.2f, pressure));
                float strain = Mathf.Sin(t * Mathf.PI * 9f) * Mathf.Sin(t * Mathf.PI);
                cuttingTool.localRotation = Quaternion.Euler(0, 0, -24f + strain * 1.2f);
                cuttingTool.anchoredPosition = new Vector2(strain * 1.5f, -strain);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Fade(0f, 1f);

            backgroundImage.sprite = openedGate;
            backgroundImage.gameObject.name = "Pic 2 - Open Gate";
            var aspect = backgroundImage.GetComponent<AspectRatioFitter>();
            if (aspect != null)
                aspect.aspectRatio = openedGate.rect.width / openedGate.rect.height;
            if (firstPictureMarks != null)
                firstPictureMarks.SetActive(false);
            var hotspot = GetComponent<Graphic>();
            if (hotspot != null)
            {
                hotspot.raycastTarget = false;
                hotspot.enabled = false;
            }
            IsCompleted = true;
            consumed = false;
            Destroy(cuttingTool.gameObject);
            cuttingTool = null;

            yield return new WaitForSecondsRealtime(0.12f);
            yield return Fade(1f, 0f);
            Destroy(coverRoot);
            coverRoot = null;
            IsBusy = false;
            Opened?.Invoke();
        }

        private void CreateCover()
        {
            coverRoot = new GameObject("Gate Transition", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = coverRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000;
            var panel = new GameObject("Fade and Input Blocker", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(coverRoot.transform, false);
            cover = panel.GetComponent<Image>();
            cover.color = Color.clear;
            cover.raycastTarget = true;
            cover.canvasRenderer.cullTransparentMesh = false;
            var rect = cover.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void CreateCuttingTool(out RectTransform left, out RectTransform right)
        {
            var root = new GameObject("Cutters - Chain Cutting", typeof(RectTransform));
            cuttingTool = root.GetComponent<RectTransform>();
            cuttingTool.SetParent(backgroundImage.transform, false);
            var target = GetComponent<RectTransform>();
            cuttingTool.anchorMin = cuttingTool.anchorMax = target.anchorMin;
            cuttingTool.sizeDelta = new Vector2(52f, 164f);
            left = CreateHalf("Left Jaw and Handle", true);
            right = CreateHalf("Right Jaw and Handle", false);
        }

        private RectTransform CreateHalf(string objectName, bool left)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(cuttingTool, false);
            var image = go.GetComponent<RawImage>();
            image.texture = consumedIcon.texture;
            image.color = new Color(0.76f, 0.78f, 0.74f, 1f);
            image.raycastTarget = false;
            Rect r = consumedIcon.rect;
            float half = r.width * 0.5f;
            image.uvRect = new Rect((r.x + (left ? 0f : half)) / image.texture.width,
                r.y / image.texture.height, half / image.texture.width, r.height / image.texture.height);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(left ? 1f : 0f, 0.96f);
            rect.sizeDelta = new Vector2(cuttingTool.sizeDelta.x * 0.5f, cuttingTool.sizeDelta.y);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0, 0, left ? -3.5f : 3.5f);
            return rect;
        }

        private IEnumerator Fade(float from, float to)
        {
            float duration = Mathf.Max(0.05f, fadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                cover.color = new Color(0, 0, 0, Mathf.Lerp(from, to,
                    Mathf.SmoothStep(0, 1, elapsed / duration)));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            cover.color = new Color(0, 0, 0, to);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (cuttingTool != null)
                Destroy(cuttingTool.gameObject);
            if (coverRoot != null)
                Destroy(coverRoot);
            // Return the tool if a scene interruption cancelled an unfinished cut.
            if (consumed && !IsCompleted && consumedFrom != null)
                consumedFrom.AddItem(requiredItem, consumedIcon);
            consumed = false;
            IsBusy = false;
        }
    }
}
