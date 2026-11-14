using System;
using System.Collections;
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
        [Header("Night Vision")]
        [SerializeField] private Volume globalVolume;
        [SerializeField] private GameObject nightVisionMarks;
        [SerializeField] private bool isCameraActive;
        [Header("Lens Transition")]
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.38f;

        public event Action<bool> OnCameraToggled;
        public bool IsCameraActive => isCameraActive;
        public float NightVisionOpacity => fullWeight > 0f ? currentWeight / fullWeight : 0f;

        private Coroutine transition;
        private float fullWeight = 1f;
        private float currentWeight;
        private float lensAmount;
        private Canvas lensCanvas;
        private NightVisionLensMask lensMask;
        private CanvasGroup marksGroup;
        private SpriteRenderer[] markSprites;
        private Color[] markSpriteColors;
        private Collider[] markColliders;
        private bool[] markColliderStates;
        private Collider2D[] markColliders2D;
        private bool[] markColliderStates2D;
        private float marksGroupAlpha;
        private bool marksGroupInteractable;
        private bool marksGroupRaycasts;

        protected override void Initialize()
        {
            base.Initialize();
            if (Cam != null)
            {
                var data = Cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (data == null)
                    data = Cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                if (globalVolume != null)
                    data.volumeLayerMask |= 1 << globalVolume.gameObject.layer;
            }
            if (slotImage == null)
                slotImage = GetComponent<Image>();
            if (slotImage != null)
                slotImage.enabled = true;
            if (globalVolume != null)
                fullWeight = globalVolume.weight > 0f ? globalVolume.weight : 1f;
            SetWeight(isCameraActive ? fullWeight : 0f);
            SetMarks(isCameraActive);
            if (isCameraActive)
                EnsureLens();
            SetLens(isCameraActive ? 1f : 0f);
        }

        protected override bool OnPressed(Vector2 screenPosition)
        {
            var hit = PickUI<Image>(screenPosition);
            if (hit == null || hit.gameObject != gameObject)
                return false;
            ToggleCamera();
            return true;
        }

        protected override bool TryGetDragTarget(Vector2 screenPosition, out Image image)
        {
            image = null;
            return false;
        }

        public void ToggleCamera()
        {
            SetCameraActive(!isCameraActive);
        }

        public void DeactivateCamera()
        {
            if (isCameraActive)
                SetCameraActive(false);
        }

        private void SetCameraActive(bool active)
        {
            isCameraActive = active;
            if (transition != null)
                StopCoroutine(transition);
            if (isActiveAndEnabled)
            {
                EnsureLens();
                transition = StartCoroutine(TransitionTo(active));
            }
            else
            {
                transition = null;
                SetWeight(active ? fullWeight : 0f);
                SetMarks(active);
                SetLens(active ? 1f : 0f);
            }
            OnCameraToggled?.Invoke(active);
        }

        private IEnumerator TransitionTo(bool active)
        {
            float startWeight = currentWeight;
            float startLens = lensAmount;
            float targetWeight = active ? fullWeight : 0f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, transitionDuration);
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetWeight(Mathf.Lerp(startWeight, targetWeight, eased));
                // Raise the goggle opening into view, or lower it when removed.
                SetLens(Mathf.Lerp(startLens, active ? 1f : 0f, eased));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetWeight(targetWeight);
            SetMarks(active);
            SetLens(active ? 1f : 0f);
            transition = null;
        }

        private void SetWeight(float weight)
        {
            currentWeight = weight;
            SetMarksOpacity(fullWeight > 0f ? weight / fullWeight : 0f);
            if (globalVolume == null)
                return;
            globalVolume.weight = weight;
            bool visible = weight > 0.0001f;
            if (globalVolume.gameObject.activeSelf != visible)
                globalVolume.gameObject.SetActive(visible);
        }

        private void SetMarks(bool visible)
        {
            SetMarksOpacity(visible ? 1f : 0f);
        }

        private void SetMarksOpacity(float opacity)
        {
            if (nightVisionMarks == null)
                return;
            if (marksGroup == null)
            {
                marksGroup = nightVisionMarks.GetComponent<CanvasGroup>();
                if (marksGroup == null)
                    marksGroup = nightVisionMarks.AddComponent<CanvasGroup>();
                marksGroupAlpha = marksGroup.alpha;
                marksGroupInteractable = marksGroup.interactable;
                marksGroupRaycasts = marksGroup.blocksRaycasts;
                markSprites = nightVisionMarks.GetComponentsInChildren<SpriteRenderer>(true);
                markSpriteColors = new Color[markSprites.Length];
                for (int i = 0; i < markSprites.Length; i++)
                    markSpriteColors[i] = markSprites[i].color;
                markColliders = nightVisionMarks.GetComponentsInChildren<Collider>(true);
                markColliderStates = new bool[markColliders.Length];
                for (int i = 0; i < markColliders.Length; i++)
                    markColliderStates[i] = markColliders[i].enabled;
                markColliders2D = nightVisionMarks.GetComponentsInChildren<Collider2D>(true);
                markColliderStates2D = new bool[markColliders2D.Length];
                for (int i = 0; i < markColliders2D.Length; i++)
                    markColliderStates2D[i] = markColliders2D[i].enabled;
            }

            opacity = Mathf.Clamp01(opacity);
            bool canInteract = isCameraActive && opacity >= 0.95f;
            // Set alpha before activation so the first rendered frame cannot flash.
            marksGroup.alpha = marksGroupAlpha * opacity;
            marksGroup.interactable = marksGroupInteractable && canInteract;
            marksGroup.blocksRaycasts = marksGroupRaycasts && canInteract;
            for (int i = 0; i < markSprites.Length; i++)
            {
                if (markSprites[i] == null)
                    continue;
                Color tint = markSpriteColors[i];
                tint.a *= opacity;
                markSprites[i].color = tint;
            }
            for (int i = 0; i < markColliders.Length; i++)
                if (markColliders[i] != null)
                    markColliders[i].enabled = markColliderStates[i] && canInteract;
            for (int i = 0; i < markColliders2D.Length; i++)
                if (markColliders2D[i] != null)
                    markColliders2D[i].enabled = markColliderStates2D[i] && canInteract;
            bool visible = opacity > 0f;
            if (nightVisionMarks.activeSelf != visible)
                nightVisionMarks.SetActive(visible);
        }

        private void EnsureLens()
        {
            if (lensCanvas != null)
                return;
            var root = new GameObject("Night Vision Goggle Mask", typeof(RectTransform), typeof(Canvas));
            lensCanvas = root.GetComponent<Canvas>();
            lensCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Keep inventory controls readable and clickable over the lens rim.
            lensCanvas.sortingOrder = slotImage != null && slotImage.canvas != null
                ? slotImage.canvas.rootCanvas.sortingOrder - 1 : -1;
            var mask = new GameObject("Donut Lens", typeof(RectTransform), typeof(NightVisionLensMask));
            mask.transform.SetParent(root.transform, false);
            lensMask = mask.GetComponent<NightVisionLensMask>();
            lensMask.raycastTarget = false;
            var rect = lensMask.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            SetLens(lensAmount);
        }

        private void SetLens(float amount)
        {
            lensAmount = Mathf.Clamp01(amount);
            if (lensCanvas == null)
                return;
            lensMask.SetProgress(lensAmount);
            lensCanvas.gameObject.SetActive(lensAmount > 0.001f && lensAmount < 0.999f);
        }

        protected override void OnDisable()
        {
            if (transition != null)
                StopCoroutine(transition);
            transition = null;
            isCameraActive = false;
            SetWeight(0f);
            SetMarks(false);
            SetLens(0f);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (lensCanvas != null)
                Destroy(lensCanvas.gameObject);
            base.OnDestroy();
        }
    }
}
