using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class PaperHandprintAttachment : MonoBehaviour
    {
        private ReturnableNote note;
        private CameraSlot cameraSlot;
        private Image image;
        private Image target;
        private Vector2 normalizedCentre;
        private Vector2 normalizedSize;
        private float angle;
        private Vector2 lastSize;

        private void Awake()
        {
            note = GetComponentInParent<ReturnableNote>();
            cameraSlot = FindFirstObjectByType<CameraSlot>();
            image = GetComponent<Image>();
            var rect = image.rectTransform;
            var paper = note.PaperImage.rectTransform;
            Rect bounds = FittedRect(note.PaperImage);
            Vector3 local = paper.InverseTransformPoint(rect.position);
            normalizedCentre = new Vector2((local.x-bounds.xMin)/bounds.width, (local.y-bounds.yMin)/bounds.height);
            normalizedSize = new Vector2(rect.rect.width*rect.localScale.x/bounds.width,
                rect.rect.height*rect.localScale.y/bounds.height);
            angle = rect.localEulerAngles.z;
            image.raycastTarget = false;
            LateUpdate();
        }

        private void LateUpdate() { RefreshAttachment(); }

        public void RefreshAttachment()
        {
            if (note == null || image == null) return;
            Image next = note.CurrentPaperView();
            if (next == null) { image.enabled = false; return; }
            Rect bounds = FittedRect(next);
            if (next != target || bounds.size != lastSize)
            {
                target = next;
                lastSize = bounds.size;
                var rect = image.rectTransform;
                rect.SetParent(target.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f);
                rect.pivot = new Vector2(0.5f,0.5f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.Euler(0,0,angle);
                rect.anchoredPosition = bounds.min + Vector2.Scale(bounds.size,normalizedCentre)
                    - target.rectTransform.rect.center;
                rect.sizeDelta = Vector2.Scale(bounds.size,normalizedSize);
                rect.SetAsLastSibling();
            }
            var canvas = target.canvas;
            bool overlay = canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
            float opacity = cameraSlot != null ? cameraSlot.NightVisionOpacity : 0f;
            image.enabled = overlay && opacity > 0f;
            image.color = new Color(1,1,1,opacity);
        }

        private static Rect FittedRect(Image image)
        {
            Rect bounds = image.rectTransform.rect;
            if (!image.preserveAspect || image.sprite == null) return bounds;
            float aspect = image.sprite.rect.width/image.sprite.rect.height;
            Vector2 centre = bounds.center;
            if(bounds.width/bounds.height>aspect) bounds.width=bounds.height*aspect;
            else bounds.height=bounds.width/aspect;
            bounds.center=centre;
            return bounds;
        }
    }
}
