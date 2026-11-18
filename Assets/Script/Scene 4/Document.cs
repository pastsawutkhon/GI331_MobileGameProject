using Solution;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Document : PressAndDragBase
{
    [Header("Pickup (ของที่เก็บในฉาก)")]
    [SerializeField] private GameObject collectTarget;
    [SerializeField] private GameObject[] hideWhenCollected;

    [Header("Paper (ข้างในเอกสาร)")]
    [SerializeField] private GameObject paperObject;
    [SerializeField] private float slideOffset = 200f;

    [Header("Close Button")]
    [SerializeField] private GameObject closeButton;

    [Header("Pages (หน้าเอกสาร)")]
    [Tooltip("Image ของแต่ละหน้า ลำดับแรก = หน้า 1 (ไม่ต้องใส่ปุ่มในนี้)")]
    [SerializeField] private Image[] pages;
    [SerializeField] private GameObject nextPageButton;
    [SerializeField] private GameObject prevPageButton;
    private int currentPage;

    [Header("Debug")]
    [Tooltip("ติ๊กเพื่อดูใน Console ว่ากดโดนอะไรบ้างตอนเปิดเอกสารอยู่")]
    [SerializeField] private bool debugLogHits;

    [Header("Document UI")]
    [SerializeField] private GameObject[] uiToHideWhenHeld;

    [Header("Inventory")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private string itemName = "Document";
    [SerializeField] private Sprite itemIcon;

    [Header("Return To Scene (ลากกลับไปวางที่เดิม)")]
    [Tooltip("พื้นที่ที่ปล่อยแล้วนับว่าวางคืน ถ้าเว้นว่างจะใช้พื้นที่ของ Collect Target")]
    [SerializeField] private RectTransform returnZone;
    public UnityEvent OnDocumentReturned;

    [Header("Scene Symbol")]
    [SerializeField] private Canvas sceneSymbol;

    [Header("Horror Events")]
    public UnityEvent OnDocumentCollected;
    public UnityEvent OnDocumentClosed;
    public UnityEvent OnDocumentReclosed;

    private readonly List<System.Action> undoHide = new List<System.Action>();

    private bool isOpen;
    private bool collected;
    private bool firstCloseDone;
    private Vector2 originalPosition;
    private bool hasSavedPosition;
    private Coroutine watchRoutine;

    public void ShowSceneSymbol()
    {
        if (sceneSymbol == null) return;
        sceneSymbol.gameObject.SetActive(true);
        sceneSymbol.enabled = true;
    }

    // ---------- Press ----------

    protected override bool OnPressed(Vector2 screenPosition)
    {
        if (debugLogHits)
            Debug.Log($"[{name}] OnPressed isOpen={isOpen} collected={collected} firstCloseDone={firstCloseDone}");

        // 1) กำลังอ่านอยู่ ปิดได้เฉพาะตรงปุ่มปิด
        if (isOpen)
        {
            if (debugLogHits)
            {
                Debug.Log($"[{name}] OnPressed ตอนเปิดเอกสาร, pages={(pages == null ? 0 : pages.Length)}");
                foreach (var h in RaycastUI(screenPosition))
                    Debug.Log($"[{name}]   hit: {h.gameObject.name}");
            }

            // ปุ่มเปลี่ยนหน้า (เช็กก่อนปุ่มปิด เพราะอาจอยู่ในกระดาษเดียวกัน)
            if (IsTopHitInside(nextPageButton, screenPosition))
            {
                ChangePage(1);
                return true;
            }
            if (IsTopHitInside(prevPageButton, screenPosition))
            {
                ChangePage(-1);
                return true;
            }

            GameObject closeArea = closeButton != null ? closeButton : paperObject;

            if (IsTopHitInside(closeArea, screenPosition))
            {
                CloseDocument();
                return true;
            }
            return base.OnPressed(screenPosition);
        }

        // 2) ยังไม่เก็บ แตะของในฉาก = เข้า inventory ทันที
        if (!collected && IsPressingCollectTarget(screenPosition))
        {
            if (Collect()) return true;
            return false;
        }

        // 3) แตะช่องที่มีเอกสารนี้ ดูตอนปล่อยนิ้วว่าเป็นการแตะ หรือการลากไปวางคืน
        if (collected && IsPressingOwnSlot(screenPosition))
        {
            if (watchRoutine != null) StopCoroutine(watchRoutine);
            watchRoutine = StartCoroutine(WatchSlotPress(screenPosition));
            return false;   // ให้ InventoryPressAndDrag ลากต่อได้ตามปกติ
        }

        return base.OnPressed(screenPosition);
    }

    // ---------- Collect ----------

    private bool Collect()
    {
        if (inventoryUI == null || inventoryUI.inventory == null)
        {
            Debug.LogWarning($"[{name}] ยังไม่ได้ใส่ Inventory UI", this);
            return false;
        }

        if (!inventoryUI.inventory.HasItem(itemName, 1))
        {
            if (!inventoryUI.inventory.AddItem(itemName, itemIcon))
                return false;   // เต็ม
        }

        collected = true;
        undoHide.Clear();

        HideCollected(collectTarget);
        foreach (var go in hideWhenCollected)
            HideCollected(go);

        Debug.Log($"[{name}] เก็บเอกสารเข้า inventory");
        OnDocumentCollected?.Invoke();
        return true;
    }

    private void HideCollected(GameObject go)
    {
        if (go == null) return;

        // สคริปต์นี้อยู่ใต้ go ถ้า SetActive(false) input จะหยุดทำงาน
        // จึงปิดแค่ภาพ/collider แทน และจำไว้เพื่อเปิดคืนตอนวางกลับ
        if (transform.IsChildOf(go.transform))
        {
            foreach (var g in go.GetComponentsInChildren<Graphic>(true))
                if (g.enabled) { g.enabled = false; var x = g; undoHide.Add(() => x.enabled = true); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.enabled) { r.enabled = false; var x = r; undoHide.Add(() => x.enabled = true); }
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                if (c.enabled) { c.enabled = false; var x = c; undoHide.Add(() => x.enabled = true); }
            foreach (var c in go.GetComponentsInChildren<Collider2D>(true))
                if (c.enabled) { c.enabled = false; var x = c; undoHide.Add(() => x.enabled = true); }
            return;
        }

        if (go.activeSelf)
        {
            go.SetActive(false);
            undoHide.Add(() => go.SetActive(true));
        }
    }

    // ---------- Read / Return from inventory ----------

    private IEnumerator WatchSlotPress(Vector2 pressPos)
    {
        float pressTime = Time.unscaledTime;

        while (pressAction != null && pressAction.IsPressed())
            yield return null;

        Vector2 endPos = Pointer.current != null
            ? Pointer.current.position.ReadValue()
            : positionAction.ReadValue<Vector2>();

        // รอให้ระบบลากของ inventory ทำงานจบก่อน
        yield return null;
        yield return null;

        watchRoutine = null;

        // ถ้าถูกใช้กับตัวรับอื่นไปแล้ว (ไอเทมหายจากช่อง) ก็จบ
        if (isOpen || !HasOwnItem()) yield break;

        bool moved = Vector2.Distance(pressPos, endPos) > 16f
                     || Time.unscaledTime - pressTime > 0.5f;

        if (debugLogHits)
            Debug.Log($"[{name}] ปล่อยนิ้วจากช่อง moved={moved} -> {(moved ? "ไม่เปิด" : "เปิดอ่าน")}");

        if (!moved)
        {
            OpenPaper();   // แตะ = เปิดอ่าน
        }
        else if (CanReturnHere() && FindSlotIndexAt(endPos) < 0 && IsOverReturnZone(endPos))
        {
            ReturnToScene();   // ลากออกไปปล่อยตรงที่เดิม = วางคืน
        }
    }

    private void ReturnToScene()
    {
        int index = FindOwnSlotIndex();
        if (index < 0) return;

        // ลบออกจากช่อง (UseItem ลดจำนวนลง 1 และเคลียร์ช่องเมื่อหมด)
        inventoryUI.UseSlot(index);

        foreach (var undo in undoHide) undo();
        undoHide.Clear();

        collected = false;
        Debug.Log($"[{name}] วางเอกสารกลับที่เดิม");
        OnDocumentReturned?.Invoke();
    }

    private void OpenPaper()
    {
        if (isOpen) return;
        isOpen = true;
        ToggleEnvelopeAnimation(true);
        SetHeld(true);
        ShowPage(0);   // เปิดทีไรเริ่มที่หน้า 1 เสมอ
    }

    private void ChangePage(int delta)
    {
        ShowPage(currentPage + delta);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;

        currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].gameObject.SetActive(i == currentPage);

        // ซ่อนปุ่มที่ไม่มีหน้าให้ไปต่อ
        if (prevPageButton != null) prevPageButton.SetActive(currentPage > 0);
        if (nextPageButton != null) nextPageButton.SetActive(currentPage < pages.Length - 1);
    }

    public void CloseDocument()
    {
        if (!isOpen) return;
        isOpen = false;
        ToggleEnvelopeAnimation(false);
        SetHeld(false);
    }

    private void SetHeld(bool held)
    {
        foreach (var go in uiToHideWhenHeld)
            if (go != null) go.SetActive(!held);
    }

    private void ToggleEnvelopeAnimation(bool open)
    {
        if (paperObject == null) return;

        RectTransform paperRect = paperObject.GetComponent<RectTransform>();

        if (paperRect != null && !hasSavedPosition)
        {
            originalPosition = paperRect.anchoredPosition;
            hasSavedPosition = true;
        }

        if (open)
        {
            paperObject.SetActive(true);

            if (paperRect != null)
                paperRect.anchoredPosition = originalPosition + new Vector2(0, slideOffset);

            Debug.Log($"[{gameObject.name}] เปิดกระดาษเอกสาร");
        }
        else
        {
            if (paperRect != null)
                paperRect.anchoredPosition = originalPosition;

            paperObject.SetActive(false);

            Debug.Log($"[{gameObject.name}] ปิดกระดาษเอกสาร");

            if (!firstCloseDone)
            {
                firstCloseDone = true;
                OnDocumentClosed?.Invoke();     // ครั้งแรก = เปลี่ยนฉาก + เสียง
            }
            else
            {
                OnDocumentReclosed?.Invoke();   // ครั้งต่อไป = ปิดเฉย ๆ
            }
        }
    }

    // ---------- Helpers ----------

    // เช็กทุก hit ไม่ใช่แค่ตัวบนสุด กันปัญหาลำดับชั้น UI
    private bool IsTopHitInside(GameObject root, Vector2 screenPosition)
    {
        if (root == null) return false;
        foreach (var hit in RaycastUI(screenPosition))
            if (hit.gameObject.transform.IsChildOf(root.transform))
                return true;
        return false;
    }

    private bool IsPressingCollectTarget(Vector2 screenPosition)
    {
        if (collectTarget == null) return false;

        if (IsTopHitInside(collectTarget, screenPosition)) return true;

        var worldHit = PickWorld<Transform>(screenPosition);
        return worldHit != null && worldHit.IsChildOf(collectTarget.transform);
    }

    private bool HasOwnItem()
    {
        return inventoryUI != null && inventoryUI.inventory != null
               && inventoryUI.inventory.HasItem(itemName, 1);
    }

    private int FindOwnSlotIndex()
    {
        if (inventoryUI == null || inventoryUI.inventory == null) return -1;

        var slots = inventoryUI.inventory.slots;
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty() && slots[i].itemName == itemName)
                return i;
        return -1;
    }

    private bool IsPressingOwnSlot(Vector2 screenPosition)
    {
        int index = FindSlotIndexAt(screenPosition);
        return index >= 0
               && inventoryUI.HasItemInSlot(index)
               && inventoryUI.inventory.slots[index].itemName == itemName;
    }

    private int FindSlotIndexAt(Vector2 screenPosition)
    {
        if (inventoryUI == null) return -1;

        foreach (var hit in RaycastUI(screenPosition))
        {
            int index = inventoryUI.GetSlotIndex(hit.gameObject);
            if (index >= 0) return index;
        }
        return -1;
    }

    // วางคืนได้เฉพาะในฉากเดิม = ก่อนที่จะปิดเอกสารครั้งแรก
    // (ปิดครั้งแรกแล้ว lightEvent จะไฟดับและเปลี่ยนฉาก จึงวางคืนไม่ได้อีก)
    private bool CanReturnHere()
    {
        return !firstCloseDone;
    }

    private bool IsOverReturnZone(Vector2 screenPos)
    {
        // 1) พื้นที่ที่กำหนดเอง
        // 2) ถ้าไม่ได้กำหนด ใช้พื้นที่ของ collectTarget (แบบ UI)
        RectTransform zone = returnZone;
        if (zone == null && collectTarget != null)
            zone = collectTarget.GetComponent<RectTransform>();

        if (zone != null)
        {
            var canvas = zone.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera : null;

            return RectTransformUtility.RectangleContainsScreenPoint(zone, screenPos, cam);
        }

        // 3) collectTarget เป็นวัตถุในฉาก 3D/2D
        if (collectTarget != null)
        {
            var worldHit = PickWorld<Transform>(screenPos);
            return worldHit != null && worldHit.IsChildOf(collectTarget.transform);
        }

        return false;
    }
}