using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Solution
{
    public class WorldInteractionHandler : PressAndDragBase
    {
        [SerializeField] private Image background;
        [SerializeField] private Sprite mannequinCloseUp;
        [SerializeField] private MannequinController mannequin;
        [SerializeField] private RectTransform stairsTouchArea;
        [SerializeField] private RectTransform mannequinTouchArea;
        [SerializeField] private RectTransform undo;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(.05f)] private float fadeDuration = .2f;
        [SerializeField] private DoorLockReceiver door;
        [SerializeField] private CameraSlot nightCamera;
        [SerializeField] private Sprite fingerprint;
        [SerializeField] private Shader fingerprintShader;
        private Material goldInk;
        private Image bodyPrint;
        private Image ladderPrint;
        internal bool CanUnlockDoor => background.isActiveAndEnabled && !zoomed && !changingView && !SceneEntranceFade.InProgress;
        private bool changingView;
        private Sprite roomSprite;

        public void CloseZoom()
        {
            if (zoomed && !mannequin.Releasing && !changingView && !SceneEntranceFade.InProgress)
                StartCoroutine(ChangeView(false));
        }

        private IEnumerator ChangeView(bool open)
        {
            changingView = true;
            fadeOverlay.blocksRaycasts = true;
            yield return FadeTo(1f);
            SetZoom(open);
            yield return FadeTo(0f);
            fadeOverlay.blocksRaycasts = false;
            changingView = false;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = fadeOverlay.alpha;
            for (float time = 0; time < fadeDuration; time += Time.unscaledDeltaTime)
            {
                fadeOverlay.alpha = Mathf.SmoothStep(start, target, time / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = target;
        }

        private Pointer dragPointer;

        private void Update()
        {
            if (!mannequin || !mannequin.Dragging || dragPointer == null) return;
            if (!dragPointer.added || (dragPointer is Touchscreen screen &&
                screen.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled))
            {
                mannequin.CancelDrag();
                return;
            }
            mannequin.MoveDrag(dragPointer.position.ReadValue(), !dragPointer.press.isPressed);
        }

        protected override void OnDisable()
        {
            if (mannequin) mannequin.CancelDrag();
            base.OnDisable();
        }

        private bool zoomed;
        internal System.Action AfterStairs { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            if (!background) return;
            roomSprite = background.sprite;
            goldInk = new Material(fingerprintShader);
            bodyPrint = CreatePrint("Mannequin fingerprint");
            ladderPrint = CreatePrint("Ladder fingerprint");
        }

        private Image CreatePrint(string label)
        {
            // The HUD is drawn after camera grading so the ridges retain their gold color.
            var mark = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            mark.transform.SetParent(undo.parent, false);
            mark.rectTransform.SetAsFirstSibling();
            mark.sprite = fingerprint;
            mark.material = goldInk;
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            mark.enabled = false;
            return mark;
        }

        private void LateUpdate()
        {
            if (!bodyPrint) return;
            bool show = CanUnlockDoor && nightCamera.IsCameraActive && nightCamera.NightVisionOpacity > 0;
            PlacePrint(bodyPrint, mannequinTouchArea, show);
            PlacePrint(ladderPrint, stairsTouchArea, show);
        }

        private void PlacePrint(Image mark, RectTransform zone, bool show)
        {
            mark.enabled = show && zone.gameObject.activeInHierarchy;
            if (!mark.enabled) return;
            Camera cam = background.canvas.rootCanvas.worldCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(cam, zone.TransformPoint(zone.rect.center));
            RectTransform hud = (RectTransform)mark.transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, point, null, out var local);
            mark.rectTransform.localPosition = local;
            mark.rectTransform.sizeDelta = Vector2.one * Mathf.Min(hud.rect.width, hud.rect.height) * .105f;
            mark.color = new Color(1f, .78f, .24f, nightCamera.NightVisionOpacity);
        }

        protected override void OnDestroy()
        {
            if (bodyPrint) Destroy(bodyPrint.gameObject);
            if (ladderPrint) Destroy(ladderPrint.gameObject);
            if (goldInk) Destroy(goldInk);
            base.OnDestroy();
        }

        private bool Hit(RectTransform zone, Vector2 point)
        {
            if (!zone || !zone.gameObject.activeInHierarchy) return false;
            var canvas = zone.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(zone, point, camera);
        }

        private void SetZoom(bool open)
        {
            zoomed = open;
            background.sprite = open ? mannequinCloseUp : roomSprite;
            mannequinTouchArea.gameObject.SetActive(!open);
            stairsTouchArea.gameObject.SetActive(!open);
            undo.gameObject.SetActive(open);
            door.gameObject.SetActive(!open);
            mannequin.SetCloseUp(open);
        }

        protected override bool OnPressed(Vector2 screenPosition)
        {
            if (SceneEntranceFade.InProgress || changingView) return true;
            if (background && background.isActiveAndEnabled)
            {
                var pointer = pressAction.activeControl?.device as Pointer;
                if (pointer != null) screenPosition = pointer.position.ReadValue();
                if (zoomed && Hit(undo, screenPosition))
                {
                    // The Undo Button handles release through its normal OnClick event.
                    return true;
                }
                if (zoomed)
                {
                    bool handled = mannequin.PressChest(screenPosition);
                    if (mannequin.Dragging) dragPointer = pointer;
                    return handled;
                }
                if (!zoomed && Hit(mannequinTouchArea, screenPosition))
                {
                    StartCoroutine(ChangeView(true));
                    return true;
                }
                if (!zoomed && Hit(stairsTouchArea, screenPosition))
                {
                    var camera = FindAnyObjectByType<CameraSlot>();
                    SceneEntranceFade.Begin("Scene 4", inventoryUI.inventory,
                        camera && camera.IsCameraActive, AfterStairs ?? (() =>
                            HatchDoorInteract.RestoreAtDoor(false, false, null)));
                    return true;
                }
            }
            // แตะลอกเทปกาว
            EvidenceBoardTape tape = PickWorld<EvidenceBoardTape>(screenPosition);
            if (tape != null)
            {
                tape.PeelTape();
                return true;
            }

            return false;
        }
    }
}