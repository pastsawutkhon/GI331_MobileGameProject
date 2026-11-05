using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Solution
{
    public class MorseKeypadLock : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite closeIcon;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite openLockerSprite;
        [SerializeField] private Material openLockerMaterial;
        [SerializeField] private AudioClip buttonSound;
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip errorSound;
        private AudioSource keypadAudio;
        [SerializeField] private CanvasGroup lockerContents;
        private bool unlocking;

        private void Awake()
        {
            keypadAudio = gameObject.AddComponent<AudioSource>();
            keypadAudio.playOnAwake = false;
            keypadAudio.spatialBlend = 0f;
            keypadAudio.volume = 1f;
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip != null && keypadAudio != null) keypadAudio.PlayOneShot(clip, volume);
        }
        public UnityEvent onUnlocked = new UnityEvent();
        public bool IsUnlocked { get; private set; }
        public static bool IsOpen { get; private set; }
        private GameObject viewer;
        private Text display;
        private RectTransform lockRect;
        private string entered = "";
        private bool checking;
        private Coroutine feedback;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Open();
        }

        public void Open()
        {
            if (IsUnlocked || IsOpen || ReturnableNote.IsInspecting || lockSprite == null) return;
            if (MorseDeskLamp.CurrentCode == null) MorseDeskLamp.BeginNewGame();
            IsOpen = true;
            entered = "";
            viewer = new GameObject("Lock Inspection", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = viewer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = viewer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(810, 1440);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = MakeImage("Backdrop", viewer.transform, new Color(0, 0, 0, 0.82f));
            Stretch(backdrop.rectTransform);
            Canvas.ForceUpdateCanvases();
            var bounds = ((RectTransform)viewer.transform).rect;
            float height = Mathf.Min(bounds.height * 0.79f, bounds.width * 0.88f * 1.5f);
            var art = MakeImage("Keypad Lock", backdrop.transform, Color.white);
            art.sprite = lockSprite;
            lockRect = art.rectTransform;
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.49f);
            lockRect.sizeDelta = new Vector2(height / 1.5f, height);
            art.preserveAspect = true;
            display = MakeText("Display", lockRect, new Vector2(0.5f, 0.532f), new Vector2(0.51f, 0.067f));
            display.fontSize = 46;
            display.resizeTextForBestFit = true;
            display.resizeTextMinSize = 16;
            display.resizeTextMaxSize = 46;
            RefreshDisplay();
            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" };
            for (int i = 0; i < labels.Length; i++)
            {
                string key = labels[i];
                var button = MakeButton("Key " + key, lockRect,
                    new Vector2(0.34f + (i % 3) * 0.162f, 0.406f - (i / 3) * 0.076f),
                    new Vector2(0.132f, 0.059f));
                button.onClick.AddListener(() => PressKey(key));
            }
            var exit = MakeButton("Close", backdrop.transform, new Vector2(0.88f, 0.91f), Vector2.zero);
            exit.GetComponent<RectTransform>().sizeDelta = new Vector2(76, 76);
            exit.onClick.AddListener(Close);
            var icon = MakeImage("Close Icon", exit.transform, Color.white);
            icon.sprite = closeIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.sizeDelta = new Vector2(44, 44);
            viewer.AddComponent<InspectionTransition>().Show(lockRect);
        }

        public void PressKey(string key)
        {
            if (!IsOpen || checking || IsUnlocked || viewer == null
                || !viewer.GetComponent<InspectionTransition>().IsReady) return;
            PlaySound(buttonSound, 0.64f);
            if (key == "C") entered = "";
            else if (key == "OK")
            {
                if (entered.Length == 3 && entered == MorseDeskLamp.CurrentCode)
                {
                    PlaySound(correctSound, 0.4f);
                    IsUnlocked = true;
                    RefreshDisplay();
                    feedback = StartCoroutine(UnlockLocker());
                }
                else
                {
                    PlaySound(errorSound, 0.48f);
                    feedback = StartCoroutine(WrongCode());
                }
                return;
            }
            else if (key.Length == 1 && key[0] >= '0' && key[0] <= '9' && entered.Length < 3)
                entered += key;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (display == null) return;
            display.color = IsUnlocked ? new Color(0.4f, 1f, 0.6f) : new Color(0.63f, 0.93f, 0.86f);
            display.text = IsUnlocked ? "OPEN" : entered.PadRight(3, '_');
        }

        private IEnumerator WrongCode()
        {
            checking = true;
            display.text = "ERROR";
            display.color = new Color(1f, 0.28f, 0.2f);
            Vector2 origin = lockRect.anchoredPosition;
            float time = 0;
            while (time < 0.38f)
            {
                time += Time.unscaledDeltaTime;
                lockRect.anchoredPosition = origin + Vector2.right * Mathf.Sin(time * 90f) * 5f * (1f - Mathf.Clamp01(time / 0.38f));
                yield return null;
            }
            lockRect.anchoredPosition = origin;
            yield return new WaitForSecondsRealtime(0.35f);
            entered = "";
            checking = false;
            RefreshDisplay();
            feedback = null;
        }

        private IEnumerator UnlockLocker()
        {
            unlocking = true;
            yield return new WaitForSecondsRealtime(0.4f);
            DismissViewer();
            while (viewer != null) yield return null;
            if (backgroundImage != null && openLockerSprite != null)
            {
                var overlay = MakeImage("Locker Background Transition", backgroundImage.transform, Color.clear);
                Stretch(overlay.rectTransform);
                overlay.transform.SetAsFirstSibling();
                overlay.sprite = openLockerSprite;
                overlay.material = openLockerMaterial;
                overlay.preserveAspect = backgroundImage.preserveAspect;
                overlay.raycastTarget = false;
                if (lockerContents != null)
                {
                    lockerContents.alpha = 0f;
                    lockerContents.interactable = false;
                    lockerContents.blocksRaycasts = false;
                    lockerContents.gameObject.SetActive(true);
                }
                float elapsed = 0;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float amount = Mathf.SmoothStep(0, 1, elapsed / 0.5f);
                    overlay.color = new Color(1, 1, 1, amount);
                    if (lockerContents != null) lockerContents.alpha = amount;
                    yield return null;
                }
                backgroundImage.sprite = openLockerSprite;
                backgroundImage.material = openLockerMaterial;
                Destroy(overlay.gameObject);
            }
            if (lockerContents != null)
            {
                lockerContents.alpha = 1f;
                lockerContents.interactable = true;
                lockerContents.blocksRaycasts = true;
            }
            var hotspot = GetComponent<Image>();
            if (hotspot != null) hotspot.raycastTarget = false;
            unlocking = false;
            feedback = null;
            onUnlocked.Invoke();
        }

        public void Close()
        {
            if (unlocking || viewer == null || !viewer.GetComponent<InspectionTransition>().IsReady) return;
            if (feedback != null) StopCoroutine(feedback);
            feedback = null;
            checking = false;
            DismissViewer();
        }

        private void DismissViewer()
        {
            if (viewer == null) return;
            var current = viewer;
            current.GetComponent<InspectionTransition>().Dismiss(() =>
            {
                if (viewer == current)
                {
                    viewer = null;
                    display = null;
                    IsOpen = false;
                }
            });
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            feedback = null;
            checking = unlocking = false;
            if (viewer != null) Destroy(viewer);
            viewer = null;
            IsOpen = false;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Button MakeButton(string name, Transform parent, Vector2 centre, Vector2 size)
        {
            var image = MakeImage(name, parent, Color.white);
            var rect = image.rectTransform;
            rect.anchorMin = centre - size / 2; rect.anchorMax = centre + size / 2;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(1f, 0.84f, 0.5f, 0.10f);
            colors.pressedColor = new Color(0, 0, 0, 0.48f);
            colors.selectedColor = Color.clear;
            colors.disabledColor = Color.clear;
            colors.fadeDuration = 0.055f;
            button.colors = colors;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            return button;
        }

        private static Text MakeText(string name, Transform parent, Vector2 centre, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = centre - size / 2; rect.anchorMax = centre + size / 2;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
