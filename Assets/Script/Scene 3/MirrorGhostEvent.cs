using System.Collections;
using UnityEngine;

namespace Solution
{
    [System.Serializable]
    public class GhostImage
    {
        [Tooltip("รูปที่จะเปิด")]
        public GameObject image;

        [Tooltip("เสียงของรูปนี้")]
        public AudioClip sound;
    }

    [System.Serializable]
    public class GhostSequence
    {
        [Tooltip("รูปแต่ละขั้นของ Event")]
        public GhostImage[] images;

        [Tooltip("เวลาคั่นระหว่างแต่ละรูป")]
        public float interval = 0.5f;
    }

    public class MirrorGhostEvent : GhostEvent
    {
        [Header("Bathroom Check")]
        [SerializeField] private GameObject exitBathroomButton;

        [Header("UI ที่ซ่อนระหว่าง Event")]
        [SerializeField] private GameObject[] disableObjects;

        [Header("Ghost Events")]
        [SerializeField] private GhostSequence[] ghostEvents;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private int currentEvent;

        protected override void OnTriggered()
        {
            // ต้องอยู่ในห้องน้ำ
            if (exitBathroomButton == null || !exitBathroomButton.activeSelf)
            {
                FinishEvent();
                return;
            }

            // เล่นครบทุก Event แล้ว
            if (currentEvent >= ghostEvents.Length)
            {
                FinishEvent();
                return;
            }

            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            GhostSequence sequence = ghostEvents[currentEvent];

            // ซ่อน UI
            foreach (GameObject obj in disableObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }

            // ปิดรูปทั้งหมดก่อนเริ่ม
            foreach (GhostImage ghostImage in sequence.images)
            {
                if (ghostImage.image != null)
                    ghostImage.image.SetActive(false);
            }

            // เปิดทีละรูป
            foreach (GhostImage ghostImage in sequence.images)
            {
                if (ghostImage.image == null)
                    continue;

                // เปิดรูป
                ghostImage.image.SetActive(true);

                // เล่นเสียงของรูปนี้
                if (audioSource != null && ghostImage.sound != null)
                {
                    audioSource.PlayOneShot(ghostImage.sound);
                }

                yield return new WaitForSeconds(sequence.interval);
            }

            // ค้างไว้ 1 วินาที
            yield return new WaitForSeconds(1f);

            // ปิดทั้งหมดพร้อมกัน
            foreach (GhostImage ghostImage in sequence.images)
            {
                if (ghostImage.image != null)
                    ghostImage.image.SetActive(false);
            }

            // เปิด UI กลับ
            foreach (GameObject obj in disableObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }

            currentEvent++;

            FinishEvent();
        }
    }
}