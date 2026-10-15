using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SafeAreaUIToolkit : VisualElement
{
    public SafeAreaUIToolkit()
    {
        style.flexGrow = 1;
        style.flexShrink = 1;

        RegisterCallback<GeometryChangedEvent>(UpdateGeometry);
    }

    private void UpdateGeometry(GeometryChangedEvent evt)
    {
        if (panel == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (panel.contextType == ContextType.Editor)
        {
            return;
        }
#endif 
        Rect safeArea = Screen.safeArea;

        Vector2 screenTopLeft = RuntimePanelUtils.ScreenToPanel(
            panel,
            Vector2.zero);

        Vector2 screenBottomRight = RuntimePanelUtils.ScreenToPanel(
            panel,
            new Vector2(Screen.width, Screen.height));

        Vector2 safeAreaTopLeft = RuntimePanelUtils.ScreenToPanel(
            panel,
            new Vector2(
                safeArea.xMin,
                Screen.height - safeArea.yMax));

        Vector2 safeAreaBottomRight = RuntimePanelUtils.ScreenToPanel(
            panel,
            new Vector2(
                safeArea.xMax,
                Screen.height - safeArea.yMin));

        float left = safeAreaTopLeft.x - screenTopLeft.x;
        float top = safeAreaTopLeft.y - screenTopLeft.y;
        float right = screenBottomRight.x - safeAreaBottomRight.x;
        float bottom = screenBottomRight.y - safeAreaBottomRight.y;

        style.marginLeft = left;
        style.marginTop = top;
        style.marginRight = right;
        style.marginBottom = bottom;
    }
}
