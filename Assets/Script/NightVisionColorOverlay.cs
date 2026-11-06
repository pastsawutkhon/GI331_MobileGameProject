using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    // Draw clue colors after camera grading, while preserving the existing marks fade.
    public class NightVisionColorOverlay : MonoBehaviour
    {
        private Canvas overlay;
        private Image[] sources;
        private Image[] copies;
        private bool[] originalEnabled;
        private CanvasGroup visibility;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            if (overlay == null)
            {
                var list = new System.Collections.Generic.List<Image>(GetComponentsInChildren<Image>(true));
                var note = FindFirstObjectByType<ReturnableNote>();
                if (note != null && note.NightVisionMark != null)
                {
                    var mark = note.NightVisionMark.GetComponent<Image>();
                    if (mark != null && !list.Contains(mark)) list.Add(mark);
                }
                sources = list.ToArray();
                copies = new Image[sources.Length];
                originalEnabled = new bool[sources.Length];
                visibility = GetComponent<CanvasGroup>();
                var root = new GameObject("Night Vision Gold Clues", typeof(RectTransform), typeof(Canvas));
                overlay = root.GetComponent<Canvas>();
                overlay.renderMode = RenderMode.ScreenSpaceOverlay;
                overlay.sortingOrder = -2;
                for (int i = 0; i < sources.Length; i++)
                {
                    originalEnabled[i] = sources[i].enabled || sources[i].GetComponent<PaperHandprintAttachment>() != null;
                    sources[i].enabled = false;
                    var go = new GameObject(sources[i].name, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(root.transform, false);
                    copies[i] = go.GetComponent<Image>();
                    copies[i].sprite = sources[i].sprite;
                    copies[i].preserveAspect = sources[i].preserveAspect;
                    copies[i].raycastTarget = false;
                    copies[i].color = Color.clear;

                }
            }
            overlay.gameObject.SetActive(true);
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (overlay == null) return;
            float opacity = visibility != null ? visibility.alpha : 1f;
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var copy = copies[i];
                bool shown = source != null && source.gameObject.activeInHierarchy && originalEnabled[i];
                var canvas = source != null ? source.canvas : null;
                bool screenOverlay = canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
                copy.enabled = shown && !screenOverlay;
                if (!copy.enabled) continue;
                Camera camera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : (canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main);
                source.rectTransform.GetWorldCorners(corners);
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
                Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
                Vector2 centre = (topLeft + bottomRight) * 0.5f;
                var rect = copy.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = centre / overlay.scaleFactor;
                rect.sizeDelta = new Vector2((bottomRight-bottomLeft).magnitude, (topLeft-bottomLeft).magnitude) / overlay.scaleFactor;
                Vector2 axis = bottomRight-bottomLeft;
                rect.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg);
                copy.color = new Color(1f, 1f, 1f, opacity);
            }
        }

        private void OnDisable() { if (overlay != null) overlay.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            if (sources != null)
                for (int i = 0; i < sources.Length; i++)
                    if (sources[i] != null) sources[i].enabled = originalEnabled[i];
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}
