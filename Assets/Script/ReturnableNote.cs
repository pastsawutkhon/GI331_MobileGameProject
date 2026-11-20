using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Solution
{
    public class ReturnableNote : InventoryDropTarget, IPointerClickHandler
    {
        public const string ItemName = "Morse Reference Note";
        [SerializeField] private Image paper;
        [SerializeField] private Sprite closeIcon;
        [SerializeField] private GameObject nightVisionMark;
        public Image PaperImage => paper;
        public GameObject NightVisionMark => nightVisionMark;
        private InventoryUI inventoryView;
        private bool collected;
        private DeskBoxInspection hidingPlace;
        private Image containerView;
        internal int ContainerPosition { get; private set; }

        private void Start()
        {
            var points = System.Array.FindAll(FindObjectsByType<DeskBoxInspection>(FindObjectsSortMode.None),
                point => point.gameObject.scene == gameObject.scene);
            if (points.Length == 0) return;
            int selection = Random.Range(0, points.Length * DeskBoxInspection.NotePositionCount);
            hidingPlace = points[selection / DeskBoxInspection.NotePositionCount];
            ContainerPosition = selection % DeskBoxInspection.NotePositionCount;
            paper.color = Color.clear;
            paper.raycastTarget = false;
            if (paperShadow != null) paperShadow.enabled = false;
        }

        internal bool IsHiddenIn(DeskBoxInspection box) => hidingPlace == box && !collected;
        internal void SetContainerView(Image view) => containerView = view;

        internal void CollectFromContainer(DeskBoxInspection box)
        {
            if (!IsHiddenIn(box) || !TryCollect()) return;
            if (containerView != null) containerView.gameObject.SetActive(false);
        }

        public Image CurrentPaperView()
        {
            if (viewer != null)
            {
                var page = viewer.transform.Find("Inspection Backdrop/Paper");
                if (page != null) return page.GetComponent<Image>();
            }
            if (!collected) return hidingPlace != null ? containerView : paper;
            if (inventoryView == null) inventoryView = FindFirstObjectByType<InventoryUI>();
            if (inventoryView == null || inventoryView.inventory == null) return null;
            for (int i = 0; i < inventoryView.inventory.slots.Count && i < inventoryView.slotImages.Length; i++)
                if (inventoryView.inventory.slots[i].itemName == ItemName)
                    return inventoryView.slotImages[i];
            return null;
        }
        private Color originalColor;
        private Shadow paperShadow;
        private static GameObject viewer;
        public static bool IsInspecting => viewer != null || MorseKeypadLock.IsOpen || BinderInspection.IsOpen || DeskBoxInspection.IsOpen;

        private void Awake()
        {
            if (paper == null) paper = GetComponent<Image>();
            originalColor = paper.color;
            paperShadow = paper.GetComponent<Shadow>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (collected || IsInspecting || eventData.button != PointerEventData.InputButton.Left) return;
            TryCollect();
        }

        private bool TryCollect()
        {
            if (inventoryView == null) inventoryView = FindFirstObjectByType<InventoryUI>();
            if (inventoryView == null || inventoryView.inventory == null ||
                !inventoryView.inventory.AddItem(ItemName, paper.sprite)) return false;
            SetCollected(true);
            return true;
        }

        private void SetCollected(bool value)
        {
            collected = value;
            if (nightVisionMark != null) nightVisionMark.SetActive(true);
            paper.color = value || hidingPlace != null ? Color.clear : originalColor;
            if (paperShadow != null) paperShadow.enabled = !value && hidingPlace == null;
        }

        public override bool TryUse(InventoryUI ui, int index)
        {
            return hidingPlace == null && TryReturn(ui, index);
        }

        internal bool ReturnToContainer(DeskBoxInspection box, InventoryUI ui, int index)
        {
            return hidingPlace == box && TryReturn(ui, index);
        }

        private bool TryReturn(InventoryUI ui, int index)
        {
            if (!collected || IsInspecting || !ui.HasItemInSlot(index)
                || ui.inventory.slots[index].itemName != ItemName) return false;
            ui.UseSlot(index);
            SetCollected(false);
            return true;
        }

        public static bool TryInspect(string itemName)
        {
            if (itemName != ItemName || IsInspecting) return false;
            var note = FindFirstObjectByType<ReturnableNote>();
            if (note == null || note.paper == null) return false;
            viewer = new GameObject("Note Inspection", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = viewer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = viewer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(810, 1440);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = new GameObject("Inspection Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(viewer.transform, false);
            var bg = (RectTransform)backdrop.transform;
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
            var close = backdrop.GetComponent<Button>();
            close.transition = Selectable.Transition.None;

            var page = new GameObject("Paper", typeof(RectTransform), typeof(Image), typeof(Button));
            page.transform.SetParent(backdrop.transform, false);
            var rect = (RectTransform)page.transform;
            rect.anchorMin = new Vector2(0.07f, 0.15f);
            rect.anchorMax = new Vector2(0.93f, 0.85f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = page.GetComponent<Image>();
            image.sprite = note.paper.sprite;
            image.preserveAspect = true;
            page.GetComponent<Button>().transition = Selectable.Transition.None;
            var exit = new GameObject("Close Inspection", typeof(RectTransform), typeof(Image), typeof(Button));
            exit.transform.SetParent(backdrop.transform, false);
            var exitRect = (RectTransform)exit.transform;
            exitRect.anchorMin = exitRect.anchorMax = new Vector2(0.87f, 0.74f);
            exitRect.sizeDelta = new Vector2(72f, 72f);
            exit.GetComponent<Image>().color = Color.clear;
            var exitButton = exit.GetComponent<Button>();
            exitButton.transition = Selectable.Transition.None;
            exitButton.onClick.AddListener(Close);
            var iconObject = new GameObject("Close Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(exit.transform, false);
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(44f, 44f);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = note.closeIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            viewer.AddComponent<InspectionTransition>().Show(rect);
            return true;
        }

        public static void Close()
        {
            if (viewer == null) return;
            var current = viewer;
            current.GetComponent<InspectionTransition>().Dismiss(() =>
            {
                if (viewer == current)
                {
                    viewer = null;
                    var note = FindFirstObjectByType<ReturnableNote>();
                    if (note != null && note.nightVisionMark != null)
                        note.nightVisionMark.GetComponent<PaperHandprintAttachment>()?.RefreshAttachment();
                }
            });
        }

        private void OnDestroy()
        {
            if (viewer != null) Destroy(viewer);
            viewer = null;
        }
    }
}
