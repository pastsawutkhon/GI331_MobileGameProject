using System;
using System.Collections;
using UnityEngine;

namespace Solution
{
    public class InspectionTransition : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform content;
        private Coroutine animation;
        private bool closing;
        public bool IsReady => group != null && group.interactable && !closing;

        public void Show(RectTransform visual)
        {
            content = visual;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = false;
            content.localScale = Vector3.one * 0.92f;
            animation = StartCoroutine(Animate(true, null));
        }

        public void Dismiss(Action finished)
        {
            if (closing) return;
            closing = true;
            if (animation != null) StopCoroutine(animation);
            group.interactable = false;
            animation = StartCoroutine(Animate(false, finished));
        }

        private IEnumerator Animate(bool opening, Action finished)
        {
            float duration = opening ? 0.24f : 0.18f;
            float alpha = group.alpha;
            Vector3 scale = content.localScale;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(alpha, opening ? 1f : 0f, t);
                content.localScale = Vector3.Lerp(scale, Vector3.one * (opening ? 1f : 0.94f), t);
                yield return null;
            }
            if (opening) group.interactable = true;
            else
            {
                finished?.Invoke();
                Destroy(gameObject);
            }
            animation = null;
        }
    }
}
