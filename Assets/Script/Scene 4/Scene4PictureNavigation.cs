using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class Scene4PictureNavigation : MonoBehaviour
    {
        [Header("Background & Views")]
        [SerializeField] private Image background1;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image mainView;
        [SerializeField] private Image leftView;
        [SerializeField] private Image rightView;

        [Header("Navigation UI")]
        [SerializeField] private GameObject navigationUI;
        [SerializeField] private Button leftArrow;
        [SerializeField] private Button rightArrow;

        [Header("Transition")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.2f;

        public bool IsExploring { get; private set; }
        public bool IsBusy { get; private set; }
        public int CurrentView { get; private set; }
        public bool InteractionLocked { get; private set; }
        public event System.Action<int> ViewChanged;


        private Sprite runtimeLeftSprite;
        private bool hasRuntimeLeftSprite;

        private Sprite GetViewSprite(int view)
        {
            if (view < 0 && hasRuntimeLeftSprite) return runtimeLeftSprite;
            Image source = view < 0 ? leftView : view > 0 ? rightView : mainView;
            return source != null ? source.sprite : null;
        }

        private void OnEnable()
        {
            if (leftArrow != null) leftArrow.onClick.AddListener(LookLeft);
            if (rightArrow != null) rightArrow.onClick.AddListener(LookRight);

            // เริ่มโหมดสำรวจและตั้งภาพหลักทันทีเมื่อเปิดฉาก
            CurrentView = 0;
            IsExploring = false;
            IsBusy = false;

            if (background1 != null) background1.gameObject.SetActive(true);
            if (backgroundImage != null && mainView != null)
            {
                backgroundImage.gameObject.SetActive(false);
                backgroundImage.sprite = mainView.sprite;
                backgroundImage.gameObject.name = "Pic 4 - Main";
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }

            RefreshControls();
        }

        public void SwitchToMainBackground()
        {
            if (IsBusy || IsExploring) return;
            StartCoroutine(TransitionToMainBackground());
        }

        private IEnumerator TransitionToMainBackground()
        {
            IsBusy = true;
            RefreshControls();

            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.blocksRaycasts = true;
                yield return Fade(0f, 1f);
            }

            // ปิด Background 1 และเปิด Pic 1 - Entrance ขึ้นมาแทน
            if (background1 != null) background1.gameObject.SetActive(false);
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
                if (mainView != null) backgroundImage.sprite = mainView.sprite;
                backgroundImage.gameObject.name = "Pic 4 - Main";
            }

            CurrentView = 0;
            IsExploring = true;
            RefreshControls();

            if (fadeOverlay != null)
            {
                yield return Fade(1f, 0f);
                fadeOverlay.blocksRaycasts = false;
            }

            IsBusy = false;
            RefreshControls();
        }

        public void ReturnToOpenDoor(Sprite openDoor)
        {
            UpdateLeftView(openDoor);
            background1.gameObject.SetActive(false);
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.sprite = openDoor;
            backgroundImage.gameObject.name = "Pic 4 - Left";
            var fitter = backgroundImage.GetComponent<AspectRatioFitter>();
            if (fitter && openDoor) fitter.aspectRatio = openDoor.rect.width / openDoor.rect.height;
            CurrentView = -1;
            IsExploring = true;
            InteractionLocked = false;
            ViewChanged?.Invoke(CurrentView);
            RefreshControls();
        }

        public void LookLeft()
        {
            if (IsExploring && !IsBusy && !InteractionLocked && CurrentView > -1 && GetViewSprite(CurrentView - 1) != null)
                BeginViewChange(CurrentView - 1);
        }

        public void LookRight()
        {

            if (IsExploring && !IsBusy && !InteractionLocked && CurrentView < 1 && GetViewSprite(CurrentView + 1) != null)
                BeginViewChange(CurrentView + 1);
        }

        private void BeginViewChange(int view)
        {
            if (backgroundImage == null)
                return;

            IsBusy = true;
            RefreshControls();

            if (fadeOverlay != null)
                fadeOverlay.blocksRaycasts = true;

            StartCoroutine(ChangeView(view));
        }

        private IEnumerator ChangeView(int view)
        {
            if (fadeOverlay != null)
                yield return Fade(0f, 1f);

            CurrentView = view;
            backgroundImage.sprite = GetViewSprite(view);
            backgroundImage.gameObject.name = view < 0 ? "Pic 4 - Left"
                : view > 0 ? "Pic 4 - Right" : "Pic 4 - Main";

            var aspect = backgroundImage.GetComponent<AspectRatioFitter>();
            if (aspect != null && backgroundImage.sprite != null)
                aspect.aspectRatio = backgroundImage.sprite.rect.width / backgroundImage.sprite.rect.height;

            ViewChanged?.Invoke(view);
            RefreshControls();

            if (fadeOverlay != null)
            {
                yield return Fade(1f, 0f);
                fadeOverlay.blocksRaycasts = false;
            }

            IsBusy = false;
            RefreshControls();
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, fadeDuration);
            while (elapsed < duration)
            {
                fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / duration));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            fadeOverlay.alpha = to;
        }

        public void SetInteractionLocked(bool locked)
        {
            InteractionLocked = locked;
            RefreshControls();
        }
        public void UpdateLeftView(Sprite newLeftSprite)
        {
            runtimeLeftSprite = newLeftSprite;
            hasRuntimeLeftSprite = true;
            if (CurrentView == -1 && backgroundImage != null)
            {
                backgroundImage.sprite = newLeftSprite;
            }
        }

        private void RefreshControls()
        {
            if (navigationUI != null) navigationUI.SetActive(IsExploring);

            if (leftArrow != null)
            {
                leftArrow.gameObject.SetActive(IsExploring && CurrentView > -1);
                //leftArrow.gameObject.SetActive(CurrentView > -1);
                leftArrow.interactable = !IsBusy && !InteractionLocked;
            }

            if (rightArrow != null)
            {
                rightArrow.gameObject.SetActive(IsExploring && CurrentView < 1);
                //rightArrow.gameObject.SetActive(CurrentView < 1);
                rightArrow.interactable = !IsBusy && !InteractionLocked;
            }
        }

        private void OnDisable()
        {
            if (leftArrow != null) leftArrow.onClick.RemoveListener(LookLeft);
            if (rightArrow != null) rightArrow.onClick.RemoveListener(LookRight);
            StopAllCoroutines();

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            IsBusy = false;
        }
    }
}
