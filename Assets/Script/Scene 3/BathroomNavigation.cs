using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class BathroomNavigation : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite hallwaySprite, bathroomSprite;
        [SerializeField] private Button enterBtn, exitBtn;
        [SerializeField] private GameObject hallwayUI, bathroomUI, bathroomUI2;
        [SerializeField] private GameObject UImirror;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.2f;

        private bool isBusy;

        private void Start()
        {
            enterBtn.onClick.AddListener(() => SwitchRoom(true));
            exitBtn.onClick.AddListener(() => SwitchRoom(false));
            ApplyRoom(false);
        }

        private void SwitchRoom(bool toBathroom)
        {
            if (!isBusy) StartCoroutine(FadeRoom(toBathroom));
        }

        private IEnumerator FadeRoom(bool toBathroom)
        {
            isBusy = true;
            fadeOverlay.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            ApplyRoom(toBathroom);
            yield return Fade(1f, 0f);
            fadeOverlay.blocksRaycasts = false;
            isBusy = false;
        }

        private void ApplyRoom(bool inBath)
        {
            backgroundImage.sprite = inBath ? bathroomSprite : hallwaySprite;
            enterBtn.gameObject.SetActive(!inBath);
            exitBtn.gameObject.SetActive(inBath);
            if (hallwayUI) hallwayUI.SetActive(!inBath);
            if (bathroomUI) bathroomUI.SetActive(inBath);
            if (bathroomUI2) bathroomUI2.SetActive(inBath);
            if (UImirror) UImirror.SetActive(inBath);
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                fadeOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = to;
        }
    }
}