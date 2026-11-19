using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Solution
{
    [Serializable]
    public class HintEntry
    {
        public string name;
        public string[] hasAll;
        public string[] hasNone;
        [TextArea] public string hint;
    }

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    public class HintSystem : MonoBehaviour
    {
        [Header("References (เว้นว่างได้ ระบบจะหาเอง)")]
        [HideInInspector] public Inventory inventory;
        public AdsSample ads;
        [Tooltip("Panel หรือปุ่มทั้งหมดที่บังข้อความคำใบ้ไว้ (จะปิดหายไปตอนดูโฆษณาจบ และกลับมาบังใหม่เมื่อมีคำใบ้ใหม่)")]
        public GameObject[] hintPanels;

        [Header("Hints ส่วนกลาง (ใช้ทุกซีน ต่อท้ายคำใบ้ของ SceneHints)")]
        public List<HintEntry> hints = new List<HintEntry>();
        [TextArea] public string fallbackHint = "ลองสำรวจรอบ ๆ ให้ละเอียดอีกครั้งนะ";

        [Tooltip("เปิดไว้ = ตอนเล่นใน Unity Editor กดปุ่มแล้วได้คำใบ้ทันทีโดยไม่ดูโฆษณา (บนมือถือยังดูโฆษณาตามปกติ)")]
        public bool skipAdInEditor = true;

        [Header("Events (ผูกกับ UI ของคุณ)")]
        public StringEvent onHintShown;
        public UnityEvent onHintReset;

        private string unlockedHint;
        private bool waitingAd;
        private Inventory subscribedInventory;

        private void OnEnable()
        {
            SceneHints.OnChanged += Recheck;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureRefs();
        }

        private void OnDisable()
        {
            SceneHints.OnChanged -= Recheck;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (subscribedInventory != null)
                subscribedInventory.OnInventoryChanged -= Recheck;

            subscribedInventory = null;
        }

        private void Start()
        {
            SetPanelsActive(true);
        }

        // เปลี่ยนซีนแล้วค้นหา Inventory ใหม่
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (subscribedInventory != null)
                subscribedInventory.OnInventoryChanged -= Recheck;

            subscribedInventory = null;
            inventory = FindObjectOfType<Inventory>(true);

            EnsureRefs();

            unlockedHint = null;
            waitingAd = false;

            SetPanelsActive(true);
            onHintReset?.Invoke();
        }

        // สั่งเปิด/ปิดทุกตัวใน Array พร้อมกัน
        private void SetPanelsActive(bool state)
        {
            if (hintPanels == null) return;
            foreach (GameObject panel in hintPanels)
            {
                if (panel != null)
                    panel.SetActive(state);
            }
        }

        // หา Inventory / AdsSample อัตโนมัติ
        private void EnsureRefs()
        {
            if (inventory == null)
                inventory = FindObjectOfType<Inventory>(true);

            if (ads == null)
                ads = FindObjectOfType<AdsSample>();

            if (inventory != subscribedInventory)
            {
                if (subscribedInventory != null)
                    subscribedInventory.OnInventoryChanged -= Recheck;

                subscribedInventory = inventory;

                if (subscribedInventory != null)
                    subscribedInventory.OnInventoryChanged += Recheck;
            }
        }

        public void OnHintButtonClicked()
        {
            EnsureRefs();

            if (unlockedHint != null)
            {
                Display(unlockedHint);
                return;
            }

            if (waitingAd) return;

            if (Application.isEditor && skipAdInEditor)
            {
                OnAdRewarded();
                return;
            }

            if (ads == null)
            {
                Debug.LogWarning("HintSystem: AdsSample not found");
                return;
            }

            waitingAd = true;
            bool started = ads.ShowRewarded(OnAdRewarded, () => waitingAd = false);

            if (!started)
            {
                waitingAd = false;
                Debug.Log("Ad not ready");
            }
        }

        public void CloseHint()
        {
            SetPanelsActive(true);
        }

        private void OnAdRewarded()
        {
            unlockedHint = GetHint();
            Display(unlockedHint);
        }

        private void Display(string text)
        {
            SetPanelsActive(false);
            onHintShown?.Invoke(text);
        }

        private void Recheck()
        {
            if (unlockedHint == null) return;
            if (GetHint() == unlockedHint) return;

            unlockedHint = null;
            SetPanelsActive(true);
            onHintReset?.Invoke();
        }

        public string GetHint()
        {
            EnsureRefs();
            if (inventory == null) return fallbackHint;

            SceneHints scene = SceneHints.Current;

            if (scene != null)
                foreach (HintEntry e in scene.hints)
                    if (Matches(e)) return e.hint;

            foreach (HintEntry e in hints)
                if (Matches(e)) return e.hint;

            if (scene != null && !string.IsNullOrEmpty(scene.fallbackHint))
                return scene.fallbackHint;

            return fallbackHint;
        }

        private bool Matches(HintEntry e)
        {
            if (inventory == null) return false;

            if (e.hasAll != null)
                foreach (string item in e.hasAll)
                    if (!inventory.HasItem(item, 1)) return false;

            if (e.hasNone != null)
                foreach (string item in e.hasNone)
                    if (inventory.HasItem(item, 1)) return false;

            return true;
        }
    }
}