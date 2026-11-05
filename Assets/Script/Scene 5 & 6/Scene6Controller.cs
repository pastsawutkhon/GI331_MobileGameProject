using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class Scene6Controller : MonoBehaviour, IItemReceiver
    {
        [Header("SD Card Slot")]
        [SerializeField] private string requiredItem = "SD_Card";

        [Header("Sequence Objects")]
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject teerapatActor;
        [SerializeField] private GameObject teerawatActor;
        [SerializeField] private GameObject sewerLadderEntrance;
        [SerializeField] private GameObject sewerDeadEndArea;
        [SerializeField] private GameObject screwdriverItem;

        [Header("Choice UI")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button btnFight;
        [SerializeField] private Button btnSuicide;
        [SerializeField] private CanvasGroup blackScreenFade;

        [Header("Camera & FX")]
        [SerializeField] private Transform camTransform;
        private Vector3 originCamPos;

        private void Start()
        {
            if (camTransform != null) originCamPos = camTransform.position;
            if (choicePanel != null) choicePanel.SetActive(false);
            if (sewerDeadEndArea != null) sewerDeadEndArea.SetActive(false);
            if (sewerLadderEntrance != null) sewerLadderEntrance.SetActive(false);

            btnFight.onClick.AddListener(OnChooseFight);
            btnSuicide.onClick.AddListener(OnChooseSuicide);
        }

        // ตรวจสอบการลาก SD Card เข้ามา
        public bool CanReceiveItem(string itemName) => itemName == requiredItem;

        public void OnReceiveItem(string itemName)
        {
            StartCoroutine(PlayStorySequence());
        }

        private IEnumerator PlayStorySequence()
        {
            // 1. เล่นคลิป
            if (videoPanel != null) videoPanel.SetActive(true);
            yield return new WaitForSeconds(3.0f);
            if (videoPanel != null) videoPanel.SetActive(false);

            // 2. ธีรภัทรสติแตก + จอสั่น
            yield return StartCoroutine(CameraShake(2.0f, 0.25f));

            // 3. ธีรวัฒน์พี่ชายปรากฏตัว + ทางหนีลงท่อเปิด
            if (teerawatActor != null) teerawatActor.SetActive(true);
            if (sewerLadderEntrance != null) sewerLadderEntrance.SetActive(true);
        }

        // เรียกเมื่อผู้เล่นแตะทางลงท่อระบายน้ำ
        public void PlayerEscapeToSewer()
        {
            if (sewerDeadEndArea != null) sewerDeadEndArea.SetActive(true);
            if (screwdriverItem != null) screwdriverItem.SetActive(true);

            // ย้ายกล้องไปที่ทางตัน
            if (camTransform != null)
                camTransform.position = new Vector3(sewerDeadEndArea.transform.position.x, sewerDeadEndArea.transform.position.y, originCamPos.z);
        }

        // เรียกเมื่อเก็บไขควงสำเร็จ (ผูกกับ OnDestroy/Event ของ PickupItem)
        public void ShowFinalChoice()
        {
            if (choicePanel != null) choicePanel.SetActive(true);
        }

        private void OnChooseFight()
        {
            choicePanel.SetActive(false);
            StartCoroutine(FadeAndEndRoutine("ต่อสู้แต่สู้ไม่ได้ ถูกธีรวัฒน์ฆ่า"));
        }

        private void OnChooseSuicide()
        {
            choicePanel.SetActive(false);
            // ตัดจบทันที (Instant Cut to Black)
            blackScreenFade.alpha = 1.0f;
            Debug.Log("ฉากจบ: ธีรภัทรตัดสินใจจบชีวิตตัวเอง (ตัดจบ)");
        }

        private IEnumerator FadeAndEndRoutine(string endingLog)
        {
            while (blackScreenFade.alpha < 1f)
            {
                blackScreenFade.alpha += Time.deltaTime * 1.5f;
                yield return null;
            }
            Debug.Log($"ฉากจบ: {endingLog}");
        }

        private IEnumerator CameraShake(float duration, float magnitude)
        {
            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                camTransform.position = new Vector3(originCamPos.x + x, originCamPos.y + y, originCamPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            camTransform.position = originCamPos;
        }
    }
}