using System.Collections;
using UnityEngine;
using Solution;

public class lightEvent : Document
{
    [Header("Horror Events")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioSource lightAudioSource;

    [Header("After Light Returns")]
    [SerializeField] private GameObject ui;
    [SerializeField] private Scene4PictureNavigation scene4Navigation;

    [Header("Background 1 - Reading Light")]
    [SerializeField] private UnityEngine.UI.Image readingBackground;
    [SerializeField] private TypewriterText[] readingPages;
    [SerializeField] private AudioClip readingLightBuzz;
    private AudioSource readingSpeaker;
    private Color normalColor;
    private bool reading;
    private float nextDip;
    private float dipStart;
    private const float DipDuration = .62f;

    private void LateUpdate()
    {
        bool typing = false;
        if (readingBackground && readingBackground.isActiveAndEnabled && readingPages != null)
        {
            foreach (var page in readingPages)
                if (page && page.isActiveAndEnabled && page.IsPlaying) { typing = true; break; }
        }
        if (!typing)
        {
            StopReadingLight();
            return;
        }

        float now = Time.unscaledTime;
        if (!reading)
        {
            reading = true;
            normalColor = readingBackground.color;
            dipStart = now - DipDuration;
            nextDip = now + .35f;
        }
        if (now >= nextDip)
        {
            dipStart = now;
            nextDip = now + DipDuration + Random.Range(.85f, 2.1f);
            if (readingLightBuzz)
            {
                if (!readingSpeaker)
                {
                    readingSpeaker = gameObject.AddComponent<AudioSource>();
                    readingSpeaker.playOnAwake = false;
                    readingSpeaker.spatialBlend = 0f;
                    readingSpeaker.volume = .38f;
                }
                readingSpeaker.PlayOneShot(readingLightBuzz);
            }
        }

        float elapsed = now - dipStart;
        float dim = 0f;
        if (elapsed < DipDuration)
        {
            float first = Mathf.Clamp01(elapsed / .045f) * (1f - Mathf.SmoothStep(0, 1, (elapsed - .08f) / .13f));
            float second = Mathf.Clamp01((elapsed - .24f) / .055f) * (1f - Mathf.SmoothStep(0, 1, (elapsed - .34f) / .28f));
            dim = Mathf.Max(first * .40f, second * .67f);
        }
        Color tint = normalColor * (1f - dim);
        tint.a = normalColor.a;
        readingBackground.color = tint;
    }

    private void StopReadingLight()
    {
        if (!reading) return;
        reading = false;
        if (readingBackground) readingBackground.color = normalColor;
        if (readingSpeaker) readingSpeaker.Stop();
    }

    protected override void OnDisable()
    {
        StopReadingLight();
        base.OnDisable();
    }

    public void OnDocumentClosed()
    {
        StartCoroutine(HorrorSequence());
    }

    private IEnumerator HorrorSequence()
    {

        // แสดง UI ไฟดับ + เล่นเสียงหลอดไฟแตกทันที
        if (lightObject != null)
        {
            lightObject.SetActive(true);
        }

        if (lightAudioSource != null)
        {
            lightAudioSource.Play();
        }

        // รอประมาณ 3 วินาที
        yield return new WaitForSeconds(2);

        // เล่นเสียงปิดประตู "ปึ้ง!"
        if (doorAudioSource != null)
        {
            doorAudioSource.Play();
        }

        if (scene4Navigation != null)
        {
            scene4Navigation.SwitchToMainBackground();
        }

        //รอต่ออีกประมาณ 2-3 วินาที
        yield return new WaitForSeconds(2);

        // เอา UI ไฟดับออก (ไฟกลับมาสว่าง)
        if (lightObject != null)
        {
            lightObject.SetActive(false);
            ShowSceneSymbol();
            ui.SetActive(true);
        }
    }
}
