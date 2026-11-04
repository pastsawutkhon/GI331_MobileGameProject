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


    [Header("Horror Events")]
    public UnityEvent OnDocumentClosed;

    private bool isOpen = false;
    private Vector2 originalPosition;
    private bool hasSavedPosition = false;

    protected override bool OnPressed(Vector2 screenPosition)
    {
        var hitGraphic = PickUI<Graphic>(screenPosition);

        if (hitGraphic != null && hitGraphic.GetComponentInParent<Document>() == this)
        {
            isOpen = !isOpen;
            ToggleEnvelopeAnimation(isOpen);

            return true;
        }

        return base.OnPressed(screenPosition);
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