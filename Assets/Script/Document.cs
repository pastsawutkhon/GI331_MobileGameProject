using Solution;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class Document : PressAndDragBase
{
    [Header("Envelope Settings")]
    [SerializeField] private GameObject paperObject; 
    [SerializeField] private float slideOffset = 200f;

    [Header("Document UI")]
    [Tooltip("UI ส่วนที่ต้องซ่อนตอนหยิบมาอ่าน เช่น รูปซอง, ไอคอน, ป้ายชื่อ")]
    [SerializeField] private GameObject[] uiToHideWhenHeld;

    [Header("Scene Symbol")]
    [SerializeField] private Canvas sceneSymbol;


    [Header("Horror Events")]
    public UnityEvent OnDocumentClosed;

    private bool isOpen = false;
    private bool isHeld = false;
    private Vector2 originalPosition;
    private bool hasSavedPosition = false;

    public void ShowSceneSymbol()
    {
        if (sceneSymbol == null) return;

        sceneSymbol.gameObject.SetActive(true); 
        sceneSymbol.enabled = true;             
    }
    protected override bool OnPressed(Vector2 screenPosition)
    {
        var hitGraphic = PickUI<Graphic>(screenPosition);

        if (hitGraphic != null && hitGraphic.GetComponentInParent<Document>() == this)
        {
            isOpen = !isOpen;
            ToggleEnvelopeAnimation(isOpen);
            SetHeld(isOpen);   // เปิด = หยิบมาอ่าน -> ซ่อน UI
            return true;
        }

        return base.OnPressed(screenPosition);
    }
    private void SetHeld(bool held)
    {
        isHeld = held;
        foreach (var go in uiToHideWhenHeld)
        {
            if (go != null) go.SetActive(!held);
        }
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
            {
                paperRect.anchoredPosition = originalPosition + new Vector2(0, slideOffset);
            }

            Debug.Log($"[{gameObject.name}] เปิดกระดาษเอกสาร");
        }
        else
        {
            if (paperRect != null)
            {
                paperRect.anchoredPosition = originalPosition;
            }

            paperObject.SetActive(false);

            Debug.Log($"[{gameObject.name}] เก็บกระดาษเอกสาร");

            

            OnDocumentClosed?.Invoke();
        }
    }

   

    public void CloseDocument()
    {
        if (isOpen)
        {
            isOpen = false;
            ToggleEnvelopeAnimation(false);
        }
    }
}