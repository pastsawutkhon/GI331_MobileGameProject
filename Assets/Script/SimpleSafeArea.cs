using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SimpleSafeArea : MonoBehaviour
{
    private RectTransform rectTransform;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new(Screen.width, Screen.height);

        if (safeArea == lastSafeArea &&
            screenSize == lastScreenSize)
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = new(
            safeArea.xMin / Screen.width,
            safeArea.yMin / Screen.height);

        Vector2 anchorMax = new(
            safeArea.xMax / Screen.width,
            safeArea.yMax / Screen.height);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}