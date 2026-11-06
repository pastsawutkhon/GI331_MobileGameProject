using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Solution
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PalletBarricade : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Scene1PictureNavigation navigation;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform palletVisual;
        [SerializeField] private PalletDustGraphic dust;
        [SerializeField] private float slideDistance = 210f;
        [SerializeField, Min(0.1f)] private float slideDuration = 1.75f;

        public bool IsMoving { get; private set; }
        public bool IsMoved { get; private set; }
        public event System.Action Moved;
        private CanvasGroup visibility;
        private Vector2 restPosition, backgroundRest;
        private Vector3 backgroundRestScale;
        private Quaternion restRotation;
        private bool capturedBackground;

        private void Awake()
        {
            visibility = GetComponent<CanvasGroup>();
            if (palletVisual != null)
            {
                restPosition = palletVisual.anchoredPosition;
                restRotation = palletVisual.localRotation;
            }
        }

        private void OnEnable()
        {
            if (visibility == null) visibility = GetComponent<CanvasGroup>();
            if (navigation != null) navigation.ViewChanged += ShowForView;
            ShowForView(navigation != null ? navigation.CurrentView : 0);
        }

        private void ShowForView(int view)
        {
            bool visible = navigation != null && navigation.IsExploring && view == 1;
            visibility.alpha = visible ? 1f : 0f;
            visibility.blocksRaycasts = visible && !IsMoving && !IsMoved;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || IsMoving || IsMoved ||
                navigation == null || !navigation.IsExploring || navigation.CurrentView != 1 ||
                navigation.IsBusy || navigation.InteractionLocked || palletVisual == null || background == null)
                return;
            IsMoving = true;
            visibility.blocksRaycasts = false;
            navigation.SetInteractionLocked(true);
            backgroundRest = background.anchoredPosition;
            backgroundRestScale = background.localScale;
            capturedBackground = true;
            StartCoroutine(ScrapeRight());
        }

        private IEnumerator ScrapeRight()
        {
            float elapsed = 0f, nextDust = 0f;
            float duration = Mathf.Max(0.1f, slideDuration);
            if (dust != null) dust.Emit(restPosition + Vector2.up * 8f, 9);
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float progress = Mathf.SmoothStep(0f, 1f, t);
                float scrape = Mathf.Sin(t * Mathf.PI * 12f) * 0.012f * envelope;
                float distance = slideDistance * Mathf.Clamp01(progress + scrape);
                palletVisual.anchoredPosition = restPosition + new Vector2(distance,
                    Mathf.Sin(elapsed * 47f) * 1.8f * envelope);
                palletVisual.localRotation = restRotation * Quaternion.Euler(0, 0,
                    Mathf.Sin(elapsed * 31f) * 0.65f * envelope);
                background.anchoredPosition = backgroundRest + new Vector2(
                    Mathf.Sin(elapsed * 53f), Mathf.Sin(elapsed * 67f)) * (1.25f * envelope);
                // Overscan follows the shake envelope, keeping all viewport edges covered.
                float padding = 2f * envelope;
                float width = Mathf.Max(1f, background.rect.width * Mathf.Abs(backgroundRestScale.x));
                float height = Mathf.Max(1f, background.rect.height * Mathf.Abs(backgroundRestScale.y));
                float zoom = 1f + Mathf.Max(2f * padding / width, 2f * padding / height);
                background.localScale = backgroundRestScale * zoom;
                if (elapsed >= nextDust && dust != null)
                {
                    dust.Emit(restPosition + new Vector2(distance, 6f), 4);
                    nextDust = elapsed + 0.13f;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            palletVisual.anchoredPosition = restPosition + Vector2.right * slideDistance;
            palletVisual.localRotation = restRotation;
            background.anchoredPosition = backgroundRest;
            background.localScale = backgroundRestScale;
            capturedBackground = false;
            if (dust != null) dust.Emit(palletVisual.anchoredPosition + Vector2.up * 6f, 8);
            IsMoved = true;
            IsMoving = false;
            navigation.SetInteractionLocked(false);
            Moved?.Invoke();
        }

        private void OnDisable()
        {
            if (navigation != null)
            {
                navigation.ViewChanged -= ShowForView;
                if (IsMoving) navigation.SetInteractionLocked(false);
            }
            StopAllCoroutines();
            if (capturedBackground && background != null)
            {
                background.anchoredPosition = backgroundRest;
                background.localScale = backgroundRestScale;
            }
            if (IsMoving && palletVisual != null)
            {
                palletVisual.anchoredPosition = restPosition;
                palletVisual.localRotation = restRotation;
            }
            capturedBackground = false;
            IsMoving = false;
            if (dust != null) dust.ClearDust();
        }
    }
}
