using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public sealed class GateIntro : MonoBehaviour
    {
        [SerializeField] private AudioClip narration;
        [SerializeField] private Font thaiFont;
        private GameObject screen;
        private Transform blackPanel;
        [SerializeField] private Shader revealShader;
        private Material reveal;
        private bool continued;

        private void Awake()
        {
            screen = new GameObject("Gate introduction", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(screen, gameObject.scene);
            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
            screen.GetComponent<Image>().color = Color.clear;
            var panel = new GameObject("Black", typeof(RectTransform), typeof(RawImage), typeof(Button));
            blackPanel = panel.transform;
            blackPanel.SetParent(screen.transform, false);
            var image = panel.GetComponent<RawImage>();
            reveal = new Material(revealShader);
            image.material = reveal;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tap = panel.GetComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.interactable = false;
            tap.onClick.AddListener(() => continued = true);
            StartCoroutine(PlayIntro(tap));
        }

        private IEnumerator PlayIntro(Button tap)
        {
            var words = Label("Narration", 0.32f, 0.68f, 26);
            var prompt = Label("Continue", 0.09f, 0.19f, 18);
            var voice = screen.AddComponent<AudioSource>();
            voice.clip = narration;
            yield return new WaitForSecondsRealtime(2f);
            voice.Play();
            float[] starts = {0f, 2.03f, 5.97f, 9.77f, 11.08f};
            string[] phrases = {
                "3 เดือนแล้ว...",
                "ตั้งแต่พี่ชายของผม\nหายตัวไปอย่างไร้ร่องรอย",
                "เบาะแสสุดท้ายพาผมมาหยุดอยู่\nที่โรงพยาบาลร้างแห่งนี้",
                "พี่ธีรวัฒน์...",
                "หวังว่าพี่จะยังปลอดภัยนะ"
            };
            int cue = 0;
            while (voice.isPlaying)
            {
                float seconds = (float)voice.timeSamples / narration.frequency;
                while (cue + 1 < starts.Length && seconds >= starts[cue + 1]) cue++;
                words.text = phrases[cue];
                words.canvasRenderer.SetAlpha(cue == 0 ? 1f : Mathf.SmoothStep(0f, 1f, (seconds - starts[cue]) / 0.42f));
                yield return null;
            }
            words.text = phrases[phrases.Length - 1];
            words.canvasRenderer.SetAlpha(1f);
            prompt.text = "แตะเพื่อไปต่อ";
            tap.interactable = true;
            while (!continued) yield return null;
            tap.interactable = false;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.25f)
            {
                reveal.SetFloat("_Progress", Mathf.SmoothStep(0f, 1f, t));
                float textAlpha = 1f - Mathf.SmoothStep(0f, 1f, t / 0.32f);
                words.canvasRenderer.SetAlpha(textAlpha);
                prompt.canvasRenderer.SetAlpha(textAlpha);
                yield return null;
            }
            Destroy(screen);
            ChapterDialogue.Show("S01_PAT_01", "S01_PAT_02");
        }

        private void OnDestroy()
        {
            if (reveal) Destroy(reveal);
        }

        private Text Label(string title, float bottom, float top, int size)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(blackPanel, false);
            var label = go.GetComponent<Text>();
            label.font = thaiFont;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.color = new Color(0.93f, 0.92f, 0.88f);
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.06f, bottom);
            rect.anchorMax = new Vector2(0.94f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return label;
        }
    }
}
