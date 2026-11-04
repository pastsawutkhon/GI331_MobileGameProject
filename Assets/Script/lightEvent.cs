using System.Collections;
using UnityEngine;

public class lightEvent : MonoBehaviour
{
    [Header("Horror Events")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioSource lightAudioSource;

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

        //รอต่ออีกประมาณ 2-3 วินาที
        yield return new WaitForSeconds(2);

        // เอา UI ไฟดับออก (ไฟกลับมาสว่าง)
        if (lightObject != null)
        {
            lightObject.SetActive(false);
        }
    }
}
