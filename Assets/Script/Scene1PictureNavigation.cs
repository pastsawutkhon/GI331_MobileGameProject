using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class Scene1PictureNavigation : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GateChainCut gateSequence;
        [SerializeField] private Sprite mainView;
        [SerializeField] private Sprite leftView;
        [SerializeField] private Sprite rightView;
        [SerializeField] private Button enterGate;
        [SerializeField] private GameObject navigationUI;
        [SerializeField] private Button leftArrow;
        [SerializeField] private Button rightArrow;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.2f;

        public bool IsExploring { get; private set; }
        public bool IsBusy { get; private set; }
        public int CurrentView { get; private set; }
        public bool InteractionLocked { get; private set; }
        public event System.Action<int> ViewChanged;

        public void SetInteractionLocked(bool locked)
        {
            InteractionLocked = locked;
            RefreshControls();
        }

        private void OnEnable()
        {
            if (gateSequence != null) gateSequence.Opened += RefreshControls;
            if (enterGate != null) enterGate.onClick.AddListener(EnterHospitalGrounds);
            if (leftArrow != null) leftArrow.onClick.AddListener(LookLeft);
            if (rightArrow != null) rightArrow.onClick.AddListener(LookRight);
            RefreshControls();
        }

        public void EnterHospitalGrounds()
        {
            if (IsExploring || IsBusy || InteractionLocked || gateSequence == null ||
                !gateSequence.IsCompleted || gateSequence.IsBusy || mainView == null)
                return;
            BeginViewChange(0);
        }

        public void LookLeft()
        {
            if (IsExploring && !IsBusy && !InteractionLocked && CurrentView > -1 && leftView != null)
                BeginViewChange(CurrentView - 1);
        }

        public void LookRight()
        {
            if (IsExploring && !IsBusy && !InteractionLocked && CurrentView < 1 && rightView != null)
                BeginViewChange(CurrentView + 1);
        }

        private void BeginViewChange(int view)
        {
            if (backgroundImage == null || fadeOverlay == null)
                return;
            IsBusy = true;
            RefreshControls();
            fadeOverlay.blocksRaycasts = true;
            StartCoroutine(ChangeView(view));
        }

        private IEnumerator ChangeView(int view)
        {
            yield return Fade(0f, 1f);
            CurrentView = view;
            IsExploring = true;
            backgroundImage.sprite = view < 0 ? leftView : view > 0 ? rightView : mainView;
            backgroundImage.gameObject.name = view < 0 ? "Pic 3 - Left"
                : view > 0 ? "Pic 3 - Right" : "Pic 3 - Main";
            var aspect = backgroundImage.GetComponent<AspectRatioFitter>();
            if (aspect != null)
                aspect.aspectRatio = backgroundImage.sprite.rect.width / backgroundImage.sprite.rect.height;
            ViewChanged?.Invoke(view);
            RefreshControls();
            yield return Fade(1f, 0f);
            fadeOverlay.blocksRaycasts = false;
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

        private void RefreshControls()
        {
            bool ready = gateSequence != null && gateSequence.IsCompleted && !gateSequence.IsBusy;
            if (enterGate != null)
            {
                enterGate.gameObject.SetActive(ready && !IsExploring);
                enterGate.interactable = !IsBusy && !InteractionLocked;
            }
            if (navigationUI != null) navigationUI.SetActive(IsExploring);
            if (leftArrow != null)
            {
                leftArrow.gameObject.SetActive(CurrentView > -1);
                leftArrow.interactable = !IsBusy && !InteractionLocked;
            }
            if (rightArrow != null)
            {
                rightArrow.gameObject.SetActive(CurrentView < 1);
                rightArrow.interactable = !IsBusy && !InteractionLocked;
            }
        }

        private void OnDisable()
        {
            if (gateSequence != null) gateSequence.Opened -= RefreshControls;
            if (enterGate != null) enterGate.onClick.RemoveListener(EnterHospitalGrounds);
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
