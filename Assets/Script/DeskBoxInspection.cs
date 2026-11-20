using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Solution
{
    public class DeskBoxInspection : InventoryDropTarget, IPointerClickHandler
    {
        [SerializeField] private Sprite picture;
        [SerializeField] private Sprite closeIcon;
        [Header("Morse Note Placement")]
        [SerializeField] private bool useCustomNotePosition;
        [SerializeField, Range(0, 1)] private float noteX = 0.5f;
        [SerializeField, Range(0, 1)] private float noteY = 0.3f;
        [SerializeField] private Vector2 notePosition2 = new Vector2(0.39f, 0.29f);
        [SerializeField] private Vector2 notePosition3 = new Vector2(0.59f, 0.29f);
        [SerializeField] private Vector2[] referenceNoteSizes;
        internal const int NotePositionCount = 3;
        private GameObject viewer;
        public static bool IsOpen { get; private set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                ReturnableNote.IsInspecting || picture == null) return;

            viewer = new GameObject("Desk Box Inspection", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            IsOpen = true;
            var canvas = viewer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = viewer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(810, 1440);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = AddImage("Backdrop", viewer.transform, Vector2.zero, Vector2.one);
            backdrop.color = new Color(0, 0, 0, 0.82f);
            var box = AddImage("Open Box", backdrop.transform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.83f));
            box.sprite = picture;
            box.preserveAspect = true;
            box.raycastTarget = false;
            Canvas.ForceUpdateCanvases();
            var note = FindFirstObjectByType<ReturnableNote>();
            if (note != null && note.IsHiddenIn(this)) AddNote(box, note);
            var close = AddImage("Close", backdrop.transform, new Vector2(0.88f, 0.89f), new Vector2(0.88f, 0.89f));
            close.rectTransform.sizeDelta = new Vector2(76, 76);
            close.color = Color.clear;
            var icon = AddImage("Icon", close.transform, Vector2.one * 0.5f, Vector2.one * 0.5f);
            icon.sprite = closeIcon;
            icon.rectTransform.sizeDelta = new Vector2(44, 44);
            icon.raycastTarget = false;
            var button = close.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => viewer.GetComponent<InspectionTransition>().Dismiss(() =>
            {
                ClearNoteView(note);
                viewer = null;
                IsOpen = false;
            }));
            viewer.AddComponent<InspectionTransition>().Show(box.rectTransform);
        }

        public override bool TryUse(InventoryUI ui, int slotIndex)
        {
            var note = FindFirstObjectByType<ReturnableNote>();
            return note != null && note.ReturnToContainer(this, ui, slotIndex);
        }

        private void AddNote(Image box, ReturnableNote note)
        {
            // Anchor props to the fitted artwork, independent of phone aspect ratio.
            var size = box.rectTransform.rect.size;
            float aspect = picture.rect.width / picture.rect.height;
            float width = Mathf.Min(size.x, size.y * aspect);
            var art = AddImage("Interior", box.transform, Vector2.one * 0.5f, Vector2.one * 0.5f);
            art.rectTransform.sizeDelta = new Vector2(width, width / aspect);
            art.enabled = false;
            bool drawer = gameObject.name.StartsWith("Right Drawer");
            bool small = gameObject.name == "Small Desk Box";
            Vector2 centre = drawer ? new Vector2(0.47f, 0.415f)
                : new Vector2(small ? 0.50f : 0.44f, 0.295f);
            int position = note.ContainerPosition;
            if (useCustomNotePosition)
            {
                centre = position == 0 ? new Vector2(noteX, noteY)
                    : position == 1 ? notePosition2 : notePosition3;
                centre = new Vector2(Mathf.Clamp01(centre.x), Mathf.Clamp01(centre.y));
            }
            else if (position != 0)
            {
                float offset = drawer ? 0.085f : 0.12f;
                centre.x += position == 1 ? -offset : offset;
            }
            var sheet = AddImage("Morse Reference Note", art.transform, centre, centre);
            sheet.sprite = note.PaperImage.sprite;
            sheet.rectTransform.sizeDelta = new Vector2(width * (small ? 0.24f : 0.21f), width * 0.23f);
            if (referenceNoteSizes != null && referenceNoteSizes.Length == NotePositionCount)
                sheet.rectTransform.sizeDelta = Vector2.Scale(art.rectTransform.sizeDelta, referenceNoteSizes[position]);
            sheet.rectTransform.localRotation = Quaternion.Euler(0, 0, small ? 5f : -5f);
            sheet.color = new Color(0.78f, 0.81f, 0.84f);
            var shadow = sheet.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.27f);
            shadow.effectDistance = new Vector2(1, -1.5f);
            var pickup = sheet.gameObject.AddComponent<Button>();
            pickup.transition = Selectable.Transition.None;
            pickup.onClick.AddListener(() => note.CollectFromContainer(this));
            note.SetContainerView(sheet);
        }

        private static void ClearNoteView(ReturnableNote note)
        {
            if (note == null) return;
            note.SetContainerView(null);
            if (note.NightVisionMark == null) return;
            note.NightVisionMark.transform.SetParent(note.PaperImage.transform, false);
            note.NightVisionMark.GetComponent<PaperHandprintAttachment>()?.RefreshAttachment();
        }

        private void OnDisable()
        {
            if (viewer == null) return;
            ClearNoteView(FindFirstObjectByType<ReturnableNote>());
            Destroy(viewer);
            viewer = null;
            IsOpen = false;
        }

        private static Image AddImage(string label, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            var rect = image.rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }
    }
}
