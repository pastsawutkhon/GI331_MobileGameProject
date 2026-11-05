using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Solution
{
    public class MorseDeskLamp : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image lampBody;
        [SerializeField] private Image armAndBase;
        [SerializeField] private Image frontRim;
        [SerializeField] private Image receivedLight;
        [SerializeField] private LampGlowGraphic tableLight;
        [SerializeField] private LampGlowGraphic bulbLight;
        private const float DotDuration = 0.2f;
        private const float DashDuration = 0.65f;
        private const float SymbolGap = 0.15f;
        private const float DigitGap = 0.85f;
        [SerializeField] private Color unlitColor = new Color(0.43f, 0.46f, 0.48f, 1f);
        [SerializeField] private Color litColor = new Color(0.76f, 0.68f, 0.52f, 1f);

        [SerializeField] private AudioClip pulseSound;
        private AudioSource pulseAudio;
        private static readonly string[] MorseDigits =
            { "-----", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----." };
        public static string CurrentCode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            CurrentCode = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && scene.name == "Scene 1") BeginNewGame();
        }

        public static void BeginNewGame()
        {
            CurrentCode = Random.Range(0, 10).ToString() + Random.Range(0, 10) + Random.Range(0, 10);
        }

        private void Awake()
        {
            if (CurrentCode == null) BeginNewGame();
            pulseAudio = gameObject.AddComponent<AudioSource>();
            pulseAudio.playOnAwake = false;
            pulseAudio.loop = true;
            pulseAudio.spatialBlend = 0f;
            pulseAudio.volume = 0f;
            pulseAudio.clip = pulseSound;
        }
        public bool IsPlaying { get; private set; }
        public bool IsLit { get; private set; }
        public event System.Action<int, int, bool> SignalChanged;
        private float brightness;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) PlayCode();
        }

        public bool PlayCode()
        {
            if (IsPlaying || !isActiveAndEnabled) return false;
            IsPlaying = true;
            StartCoroutine(FlashCode());
            return true;
        }

        private IEnumerator FlashCode()
        {
            string code = CurrentCode;
            for (int digit = 0; digit < code.Length; digit++)
            {
                string pattern = MorseDigits[code[digit] - '0'];
                for (int symbol = 0; symbol < pattern.Length; symbol++)
                {
                    IsLit = true;
                    if (pulseAudio != null && pulseAudio.clip != null) pulseAudio.Play();
                    SignalChanged?.Invoke(digit, symbol, true);
                    yield return new WaitForSecondsRealtime(pattern[symbol] == '.' ? DotDuration : DashDuration);
                    IsLit = false;
                    SignalChanged?.Invoke(digit, symbol, false);
                    if (symbol < pattern.Length - 1)
                        yield return new WaitForSecondsRealtime(SymbolGap);
                }
                if (digit < code.Length - 1)
                    yield return new WaitForSecondsRealtime(DigitGap);
            }
            IsPlaying = false;
        }

        private void Update()
        {
            float next = Mathf.MoveTowards(brightness, IsLit ? 1f : 0f, Time.unscaledDeltaTime / 0.025f);
            if (Mathf.Approximately(next, brightness)) return;
            SetBrightness(next);
        }

        private void SetBrightness(float value)
        {
            brightness = value;
            if (pulseAudio != null)
            {
                pulseAudio.volume = value * 0.7f;
                if (!IsLit && value <= 0f) pulseAudio.Stop();
            }
            if (lampBody != null) lampBody.color = unlitColor;
            if (armAndBase != null)
                armAndBase.color = unlitColor;
            if (frontRim != null)
                frontRim.color = unlitColor;
            if (receivedLight != null)
                receivedLight.color = new Color(1f, 0.74f, 0.42f, value * 0.85f);
            if (tableLight != null) tableLight.SetIntensity(value);
            if (bulbLight != null) bulbLight.SetIntensity(value);
        }

        private void OnEnable() { SetBrightness(0f); }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (pulseAudio != null) pulseAudio.Stop();
            IsLit = false;
            IsPlaying = false;
            SetBrightness(0f);
        }
    }
}
