using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ข้อความ TextMeshPro ค่อยๆ พิมพ์ทีละตัวอักษร พอพิมพ์จบจะมีเส้นขีดฆ่าค่อยๆ ลากผ่านข้อความที่เลือกไว้
/// แปะบน object ที่มี TextMeshPro - Text (UI)
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    [System.Serializable]
    public class StrikeTarget
    {
        [Tooltip("ข้อความที่จะขีดฆ่า ต้องพิมพ์ให้ตรงกับในข้อความเป๊ะๆ เช่น ชื่อคน")]
        public string text;

        [Tooltip("0 = ขีดทุกที่ที่เจอ, 1 = เฉพาะที่เจอครั้งแรก, 2 = ครั้งที่สอง ...")]
        [Min(0)] public int occurrence = 0;
    }

    [Header("Speed")]
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float startDelay = 0f;

    [Header("Play")]
    [SerializeField] private bool playOnEnable = true;

    [Header("Sound (ไม่ใส่ก็ได้)")]
    [Tooltip("เสียงเขียน เล่นวนต่อเนื่องตั้งแต่เริ่มพิมพ์จนพิมพ์ครบ แล้วหยุดเอง ใช้คลิปเสียงปากกาเขียนยาวๆ ที่วนได้")]
    [SerializeField] private AudioSource typeSound;

    [Header("Strike Through (ขีดฆ่าหลังพิมพ์จบ)")]
    [Tooltip("รายการข้อความที่จะถูกขีดฆ่า ทีละรายการตามลำดับ เว้นว่างไว้ = ไม่ขีดฆ่า")]
    [SerializeField] private StrikeTarget[] strikeTargets;
    [Tooltip("รอกี่วินาทีหลังพิมพ์จบ ก่อนเริ่มขีดรายการแรก")]
    [SerializeField] private float strikeStartDelay = 1f;
    [Tooltip("เวลาที่ใช้ลากเส้นหนึ่งเส้น (วินาที)")]
    [SerializeField] private float strikeDuration = 0.5f;
    [Tooltip("เวลาพักระหว่างขีดแต่ละรายการ")]
    [SerializeField] private float delayBetweenStrikes = 0.6f;
    [SerializeField] private Color strikeColor = new Color(0.55f, 0.02f, 0.05f, 1f);
    [Tooltip("ความหนาเส้น เป็นสัดส่วนของขนาดฟอนต์")]
    [SerializeField] private float thicknessRatio = 0.09f;
    [Tooltip("เส้นยื่นเลยขอบข้อความออกไปกี่พิกเซล")]
    [SerializeField] private float overshoot = 6f;
    [Tooltip("เอียงเส้นเล็กน้อยให้ดูเหมือนขีดด้วยมือ")]
    [SerializeField, Range(0f, 5f)] private float angleJitter = 1.5f;
    [Tooltip("ตำแหน่งความสูงของเส้นในบรรทัด 0 = ที่เส้นฐานตัวอักษร, 1 = บนสุดของฟอนต์ ถ้าเส้นต่ำไปให้เพิ่มค่า")]
    [SerializeField, Range(0f, 1f)] private float lineHeightRatio = 0.5f;
    [SerializeField] private AudioSource strikeSound;

    [Header("Events")]
    public UnityEvent OnFinished;
    public UnityEvent OnStrikesFinished;

    private class Segment
    {
        public int line;
        public float minX, maxX, baseLine, ascender;
    }

    private TMP_Text text;
    private Coroutine routine;
    private Coroutine strikeRoutine;
    private readonly List<GameObject> strikeLines = new List<GameObject>();

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        if (strikeRoutine != null) StopCoroutine(strikeRoutine);
        routine = null;
        strikeRoutine = null;
        IsPlaying = false;
        StopTypeSound();
        StopStrikeSound();
    }

    // ---------- Public API ----------

    /// <summary>เล่นจากต้น</summary>
    public void Play()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (routine != null) StopCoroutine(routine);
        StopTypeSound();
        ClearStrikes();
        routine = StartCoroutine(Run());
    }

    /// <summary>เปลี่ยนข้อความแล้วเล่นใหม่</summary>
    public void Show(string newText)
    {
        if (text == null) text = GetComponent<TMP_Text>();
        text.text = newText;
        Play();
    }

    /// <summary>ข้ามไปแสดงข้อความทั้งหมดทันที</summary>
    public void Skip()
    {
        if (!IsPlaying) return;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        Finish();
    }

    /// <summary>ลบเส้นขีดทั้งหมด</summary>
    public void ClearStrikes()
    {
        if (strikeRoutine != null) StopCoroutine(strikeRoutine);
        strikeRoutine = null;

        StopStrikeSound();

        foreach (var g in strikeLines)
            if (g != null) Destroy(g);
        strikeLines.Clear();
    }

    // ---------- Sound ----------

    // เสียงเขียน: เล่นวนตลอดช่วงที่กำลังพิมพ์
    private void StartTypeSound()
    {
        if (typeSound == null || typeSound.clip == null) return;
        typeSound.loop = true;
        typeSound.Play();
    }

    private void StopTypeSound()
    {
        if (typeSound == null || typeSound == strikeSound) return;
        typeSound.Stop();
    }

    private void StopStrikeSound()
    {
        if (strikeSound != null) strikeSound.Stop();
    }

    // ---------- Typewriter ----------

    private IEnumerator Run()
    {
        IsPlaying = true;
        text.maxVisibleCharacters = 0;

        // รอ 1 เฟรมให้ TMP สร้างข้อมูลตัวอักษรก่อน
        yield return null;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        if (charactersPerSecond <= 0f || total == 0)
        {
            routine = null;
            Finish();
            yield break;
        }

        StartTypeSound();

        float progress = 0f;
        int shown = 0;

        while (shown < total)
        {
            progress += charactersPerSecond * Time.unscaledDeltaTime;
            int target = Mathf.Min(total, Mathf.FloorToInt(progress));

            if (target > shown)
            {
                shown = target;
                text.maxVisibleCharacters = shown;
            }

            yield return null;
        }

        routine = null;
        Finish();
    }

    private void Finish()
    {
        text.maxVisibleCharacters = int.MaxValue;
        IsPlaying = false;

        // พิมพ์ครบแล้ว หยุดเสียงเขียน ก่อนจะเริ่มเสียงขีด
        StopTypeSound();

        if (text.text.Contains("CLH-PT-0724"))
            Solution.ChapterDialogue.Show("S04_PAT_01", "S04_PAT_02");
        OnFinished?.Invoke();

        if (HasStrikeTargets() && isActiveAndEnabled)
        {
            if (strikeRoutine != null) StopCoroutine(strikeRoutine);
            strikeRoutine = StartCoroutine(StrikeSequence());
        }
    }

    // ---------- Strike Through ----------

    private bool HasStrikeTargets()
    {
        if (strikeTargets == null) return false;
        foreach (var t in strikeTargets)
            if (t != null && !string.IsNullOrEmpty(t.text)) return true;
        return false;
    }

    private IEnumerator StrikeSequence()
    {
        if (strikeStartDelay > 0f)
            yield return new WaitForSecondsRealtime(strikeStartDelay);

        // ให้ TMP อัปเดตข้อมูลตัวอักษรให้ครบก่อนอ่านตำแหน่ง
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        string plain = BuildPlainString(info);

        foreach (var target in strikeTargets)
        {
            if (target == null || string.IsNullOrEmpty(target.text)) continue;

            int found = 0;
            int from = 0;

            while (from <= plain.Length)
            {
                int idx = plain.IndexOf(target.text, from, System.StringComparison.Ordinal);
                if (idx < 0) break;

                found++;
                from = idx + target.text.Length;

                bool wanted = target.occurrence == 0 || found == target.occurrence;
                if (wanted)
                {
                    yield return StrikeRange(idx, idx + target.text.Length - 1);
                    yield return new WaitForSecondsRealtime(delayBetweenStrikes);
                }

                if (target.occurrence != 0 && found >= target.occurrence) break;
            }

            if (found == 0)
                Debug.LogWarning($"[{name}] หาข้อความ \"{target.text}\" ไม่เจอ ต้องพิมพ์ให้ตรงกับในข้อความเป๊ะๆ", this);
        }

        strikeRoutine = null;
        OnStrikesFinished?.Invoke();
    }

    private string BuildPlainString(TMP_TextInfo info)
    {
        var sb = new StringBuilder(info.characterCount);
        for (int i = 0; i < info.characterCount; i++)
            sb.Append((char)info.characterInfo[i].character);
        return sb.ToString();
    }

    // รวบรวมตำแหน่งของข้อความช่วงหนึ่ง แยกเป็นส่วนตามบรรทัด
    private IEnumerator StrikeRange(int first, int last)
    {
        TMP_TextInfo info = text.textInfo;
        var segments = new List<Segment>();
        Segment cur = null;

        for (int i = first; i <= last && i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            if (cur == null || cur.line != ch.lineNumber)
            {
                cur = new Segment
                {
                    line = ch.lineNumber,
                    minX = ch.bottomLeft.x,
                    maxX = ch.topRight.x,
                    baseLine = ch.baseLine,
                    ascender = ch.ascender
                };
                segments.Add(cur);
            }
            else
            {
                cur.minX = Mathf.Min(cur.minX, ch.bottomLeft.x);
                cur.maxX = Mathf.Max(cur.maxX, ch.topRight.x);
                cur.ascender = Mathf.Max(cur.ascender, ch.ascender);
            }
        }

        yield return StrikeSegments(segments);
    }

    // ลากทุกส่วนต่อเนื่องกันเหมือนปากกาเส้นเดียว
    private IEnumerator StrikeSegments(List<Segment> segments)
    {
        if (segments.Count == 0) yield break;

        float thickness = Mathf.Round(Mathf.Max(3f, text.fontSize * thicknessRatio));

        // จำกัดมุมเอียงตามความยาวเส้น เส้นยาวถ้าเอียงมากปลายเส้นจะไหลลงไปทับบรรทัดอื่น
        float longest = 1f;
        foreach (var s in segments)
            longest = Mathf.Max(longest, (s.maxX - s.minX) + overshoot * 2f);
        float maxAngle = Mathf.Min(angleJitter,
            Mathf.Atan2(text.fontSize * 0.08f, longest) * Mathf.Rad2Deg);
        float angle = Random.Range(-maxAngle, maxAngle);

        var lines = new List<RectTransform>();
        var lengths = new List<float>();
        float total = 0f;

        foreach (var seg in segments)
        {
            float startX = seg.minX - overshoot;
            float length = (seg.maxX - seg.minX) + overshoot * 2f;
            float y = Mathf.Round(seg.baseLine + (seg.ascender - seg.baseLine) * lineHeightRatio);

            var rt = CreateLine(startX, y, thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle); // มุมเดียวกันทุกเส้น
            lines.Add(rt);
            lengths.Add(length);
            total += length;
        }

        if (strikeSound != null)
        {
            strikeSound.loop = false;
            strikeSound.Stop();
            strikeSound.Play();
        }

        float duration = Mathf.Max(0.05f, strikeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - p) * (1f - p);   // เริ่มเร็ว ปลายช้าลง เหมือนลากปากกา
            float drawn = total * eased;

            for (int i = 0; i < lines.Count; i++)
            {
                float w = Mathf.Clamp(drawn, 0f, lengths[i]);
                lines[i].sizeDelta = new Vector2(w, thickness);
                drawn -= lengths[i];
            }
            yield return null;
        }

        for (int i = 0; i < lines.Count; i++)
            lines[i].sizeDelta = new Vector2(lengths[i], thickness);

        // เส้นลากเสร็จแล้ว หยุดเสียงขีดให้พอดีกัน
        StopStrikeSound();
    }

    private RectTransform CreateLine(float x, float y, float thickness)
    {
        var go = new GameObject("StrikeLine", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(text.transform, false);

        var img = go.GetComponent<Image>();
        img.color = strikeColor;
        img.raycastTarget = false;

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(0f, thickness);
        rt.localPosition = new Vector3(x, y, 0f);

        strikeLines.Add(go);
        return rt;
    }
}