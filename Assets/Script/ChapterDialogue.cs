using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public sealed class ChapterDialogue : MonoBehaviour
    {
        private static ChapterDialogue instance;
        private readonly HashSet<string> played = new HashSet<string>();
        private readonly Queue<string> pending = new Queue<string>();
        private ChapterVoiceLines library;
        private AudioSource voice;
        private Button advance;
        private Text subtitle;
        private bool next;
        public static bool Reading => instance && instance.advance.gameObject.activeSelf;

        public static void Show(params string[] ids)
        {
            if (!instance)
                instance = new GameObject("Chapter dialogue").AddComponent<ChapterDialogue>();
            foreach (string id in ids)
                if (instance.played.Add(id)) instance.pending.Enqueue(id);
            if (!Reading && instance.pending.Count > 0)
                instance.StartCoroutine(instance.Read());
        }

        public static IEnumerator Sequence(params string[] ids)
        {
            Show(ids);
            while (Reading) yield return null;
        }

        private void Awake()
        {
            library = Resources.Load<ChapterVoiceLines>("ChapterVoiceLines");
            voice = gameObject.AddComponent<AudioSource>();
            var ui = new GameObject("Tap to continue", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image), typeof(Button));
            ui.transform.SetParent(transform, false);
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32762;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = .5f;
            ui.GetComponent<Image>().color = new Color(0,0,0,.14f);
            advance = ui.GetComponent<Button>();
            advance.transition = Selectable.Transition.None;
            advance.onClick.AddListener(() => next = true);
            var panel = new GameObject("Dialogue", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(ui.transform,false);
            rect.anchorMin = new Vector2(.035f,.045f);
            rect.anchorMax = new Vector2(.965f,.265f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.035f,.05f,.05f,.96f);
            subtitle = new GameObject("Subtitle",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            rect = subtitle.rectTransform;
            rect.SetParent(panel.transform,false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16,8);
            rect.offsetMax = new Vector2(-16,-8);
            subtitle.font = library.font;
            subtitle.fontSize = 22;
            subtitle.resizeTextForBestFit = true;
            subtitle.resizeTextMinSize = 14;
            subtitle.resizeTextMaxSize = 22;
            subtitle.alignment = TextAnchor.MiddleLeft;
            subtitle.raycastTarget = false;
            ui.SetActive(false);
        }

        private IEnumerator Read()
        {
            advance.gameObject.SetActive(true);
            while (pending.Count > 0)
            {
                string id = pending.Dequeue();
                var line = Array.Find(library.lines, entry => entry.id == id);
                subtitle.text = line.speaker + "\n" + line.text + "  ›";
                voice.clip = Resources.Load<AudioClip>("Chapter1Voice/" + id);
                voice.Play();
                next = false;
                yield return null;
                while (!next) yield return null;
                voice.Stop();
            }
            advance.gameObject.SetActive(false);
        }
    }
}
