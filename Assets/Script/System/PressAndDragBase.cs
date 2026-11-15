using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Solution
{
    public class PressAndDragBase : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        public event Action<GameObject> DragStarted;
        public event Action<GameObject, Vector2> DragEnded;

        protected InputActionMap touchMap;
        protected InputAction pressAction;
        protected InputAction positionAction;

        protected Camera Cam
        {
            get
            {
                if (cam == null)
                    cam = Camera.main;

                return cam;
            }
        }

        private Camera cam;
        protected bool isDragging;

        private Coroutine dragCoroutine;
        private InputActionAsset runtimeInputActions;

        protected List<RaycastResult> uiHits = new List<RaycastResult>();

        private DragState currentDrag;

        private class DragState
        {
            public Image image;
            public RectTransform rect;
            public Transform originalParent;
            public int originalSiblingIndex;

            public Vector3 originalPosition;
            public Quaternion originalRotation;
            public Vector3 originalScale;

            public Vector2 originalAnchorMin;
            public Vector2 originalAnchorMax;
            public Vector2 originalPivot;
            public Vector2 originalSizeDelta;
            public Vector2 originalAnchoredPosition;

            public bool originalRaycastTarget;

            public readonly List<GraphicState> graphics =
                new List<GraphicState>();

            public Canvas canvas;
            public Camera canvasCamera;
            public Vector3 grabOffset;
        }

        private class GraphicState
        {
            public Graphic graphic;
            public bool raycastTarget;
        }

        // --------------------------------------------------
        // UNITY LIFECYCLE
        // --------------------------------------------------

        protected virtual void Awake()
        {
            cam = Camera.main;

            if (inputActions == null)
            {
                Debug.LogError(
                    $"{name}: InputActionAsset is not assigned.", this);

                enabled = false;
                return;
            }

            // แยก instance เพื่อไม่ให้ไปแก้ Action Asset ของ object อื่น
            runtimeInputActions = Instantiate(inputActions);

            touchMap = runtimeInputActions.FindActionMap(
                "Touch", false);

            if (touchMap == null)
            {
                Debug.LogError(
                    $"{name}: Cannot find Action Map 'Touch'.", this);

                enabled = false;
                return;
            }

            pressAction = touchMap.FindAction(
                "TouchPress", false);

            positionAction = touchMap.FindAction(
                "TouchPosition", false);

            if (pressAction == null || positionAction == null)
            {
                Debug.LogError(
                    $"{name}: Cannot find TouchPress or TouchPosition.",
                    this);

                enabled = false;
                return;
            }

            Initialize();
        }

        protected virtual void OnEnable()
        {
            if (pressAction == null || touchMap == null)
                return;

            pressAction.performed += HandlePress;
            touchMap.Enable();
        }

        protected virtual void OnDisable()
        {
            if (pressAction != null)
                pressAction.performed -= HandlePress;

            if (dragCoroutine != null)
            {
                StopCoroutine(dragCoroutine);
                dragCoroutine = null;
            }

            if (currentDrag != null)
                FinishDrag(true, Vector2.zero);

            isDragging = false;

            if (touchMap != null)
                touchMap.Disable();
        }

        protected virtual void OnDestroy()
        {
            if (runtimeInputActions != null)
            {
                Destroy(runtimeInputActions);
                runtimeInputActions = null;
            }
        }

        // --------------------------------------------------
        // EXTENSION POINTS FOR CHILD CLASSES
        // --------------------------------------------------

        /// <summary>
        /// ใช้ตั้งค่าเพิ่มเติมในคลาสลูก
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// คืน true ถ้าการกดนี้ถูกคลาสลูกจัดการแล้ว
        /// เช่น กดเพื่อเล่น Animation
        /// </summary>
        protected virtual bool OnPressed(Vector2 screenPosition)
        {
            return false;
        }

        /// <summary>
        /// เลือก Image ที่ต้องการลาก
        /// คลาสลูกที่ไม่รองรับการลากให้ใช้ค่าเริ่มต้นได้
        /// </summary>
        protected virtual bool TryGetDragTarget(
            Vector2 screenPosition, out Image image)
        {
            image = null;
            return false;
        }

        /// <summary>
        /// กำหนด Parent ระหว่างลาก
        /// คืน null เพื่อคง Parent เดิม
        /// </summary>
        protected virtual Transform GetDragParent(Image image)
        {
            return null;
        }

        protected virtual void OnDragStarted(GameObject draggedObject)
        {
        }

        /// <summary>
        /// คืน true ถ้าวางสำเร็จ
        /// คืน false ถ้าวางไม่สำเร็จและต้องคืนตำแหน่ง
        /// </summary>
        protected virtual bool OnDrop(
            GameObject draggedObject, Vector2 screenPosition)
        {
            return false;
        }

        /// <summary>
        /// กำหนดว่าต้องคืนตำแหน่งเดิมหรือไม่
        /// ค่าเริ่มต้นคือคืนเมื่อวางไม่สำเร็จ
        /// </summary>
        protected virtual bool ShouldRestorePosition(
            GameObject draggedObject, bool dropSucceeded)
        {
            return !dropSucceeded;
        }

        protected virtual void OnDragFinished(
            GameObject draggedObject,
            Vector2 screenPosition,
            bool dropSucceeded)
        {
        }

        /// <summary>
        /// เรียกเมื่อยกเลิกการลาก เช่น Component ถูกปิด
        /// </summary>
        protected virtual void OnDragCancelled(GameObject draggedObject)
        {
        }

        // --------------------------------------------------
        // INPUT
        // --------------------------------------------------

        private void HandlePress(InputAction.CallbackContext context)
        {
            if (isDragging || positionAction == null)
                return;

            Vector2 screenPosition = positionAction.ReadValue<Vector2>();

            // ให้การกดมีสิทธิ์ทำงานก่อนการลาก
            if (OnPressed(screenPosition))
                return;

            if (TryGetDragTarget(screenPosition, out Image image))
            {
                if (image == null)
                    return;

                dragCoroutine = StartCoroutine(
                    DragRoutine(image, screenPosition));
            }
        }

        // --------------------------------------------------
        // DRAG ROUTINE
        // --------------------------------------------------

        private IEnumerator DragRoutine(
            Image image, Vector2 startScreenPosition)
        {
            if (image == null)
                yield break;

            RectTransform rect = image.rectTransform;

            if (rect == null || rect.parent == null)
                yield break;

            Canvas canvas = image.GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning(
                    $"{name}: Drag target must be inside a Canvas.", this);

                yield break;
            }

            DragState state = CreateDragState(image, canvas);

            if (state == null)
                yield break;

            currentDrag = state;
            isDragging = true;

            // คำนวณตำแหน่งที่ผู้ใช้จับ เพื่อไม่ให้ภาพกระโดด
            if (!TryGetWorldPoint(
                state, startScreenPosition, out Vector3 startWorldPoint))
            {
                FinishDrag(true, startScreenPosition);
                yield break;
            }

            state.grabOffset = rect.position - startWorldPoint;

            // ย้าย Parent ได้ตามที่คลาสลูกกำหนด
            Transform dragParent = GetDragParent(image);

            if (dragParent != null && dragParent != rect.parent)
                rect.SetParent(dragParent, true);

            // ปิด Raycast ของ Graphic ลูกทั้งหมดระหว่างลาก
            SetDragRaycasts(state, false);

            DragStarted?.Invoke(image.gameObject);
            OnDragStarted(image.gameObject);

            while (pressAction != null && pressAction.IsPressed())
            {
                if (currentDrag != state || image == null)
                    break;

                Vector2 screenPosition =
                    positionAction.ReadValue<Vector2>();

                if (TryGetWorldPoint(
                    state, screenPosition, out Vector3 worldPoint))
                {
                    rect.position = worldPoint + state.grabOffset;
                }

                yield return null;
            }

            if (currentDrag == state)
            {
                Vector2 endPosition = Pointer.current != null
                 ? Pointer.current.position.ReadValue()
                 : positionAction.ReadValue<Vector2>();

                FinishDrag(false, endPosition);
            }
        }

        // --------------------------------------------------
        // DRAG STATE
        // --------------------------------------------------

        private DragState CreateDragState(Image image, Canvas canvas)
        {
            RectTransform rect = image.rectTransform;

            if (rect == null)
                return null;

            DragState state = new DragState
            {
                image = image,
                rect = rect,
                originalParent = rect.parent,
                originalSiblingIndex = rect.GetSiblingIndex(),

                originalPosition = rect.position,
                originalRotation = rect.rotation,
                originalScale = rect.localScale,

                originalAnchorMin = rect.anchorMin,
                originalAnchorMax = rect.anchorMax,
                originalPivot = rect.pivot,
                originalSizeDelta = rect.sizeDelta,
                originalAnchoredPosition = rect.anchoredPosition,

                originalRaycastTarget = image.raycastTarget,

                canvas = canvas,
                canvasCamera = GetCanvasCamera(canvas)
            };

            return state;
        }

        private Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            Canvas rootCanvas = canvas.rootCanvas;

            if (rootCanvas == null)
                rootCanvas = canvas;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return rootCanvas.worldCamera != null
                ? rootCanvas.worldCamera
                : Cam;
        }

        private bool TryGetWorldPoint(
            DragState state,
            Vector2 screenPosition,
            out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;

            if (state == null || state.rect == null)
                return false;

            RectTransform parentRect =
                state.rect.parent as RectTransform;

            if (parentRect == null)
                return false;

            return RectTransformUtility
                .ScreenPointToWorldPointInRectangle(
                    parentRect,
                    screenPosition,
                    state.canvasCamera,
                    out worldPoint);
        }

        // --------------------------------------------------
        // FINISH / CANCEL
        // --------------------------------------------------

        private void FinishDrag(
            bool cancelled, Vector2 screenPosition)
        {
            DragState state = currentDrag;

            if (state == null)
            {
                isDragging = false;
                dragCoroutine = null;
                return;
            }

            GameObject draggedObject = state.image != null
                ? state.image.gameObject
                : null;

            bool dropSucceeded = false;

            if (!cancelled && draggedObject != null)
            {
                dropSucceeded = OnDrop(
                    draggedObject, screenPosition);
            }

            bool restorePosition = cancelled;

            if (!cancelled && draggedObject != null)
            {
                restorePosition = ShouldRestorePosition(
                    draggedObject, dropSucceeded);
            }

            if (restorePosition)
                RestorePosition(state);

            RestoreDragRaycasts(state);

            currentDrag = null;
            isDragging = false;
            dragCoroutine = null;

            if (cancelled)
            {
                if (draggedObject != null)
                    OnDragCancelled(draggedObject);
            }
            else if (draggedObject != null)
            {
                OnDragFinished(
                    draggedObject, screenPosition, dropSucceeded);

                DragEnded?.Invoke(draggedObject, screenPosition);
            }
        }

        private void RestorePosition(DragState state)
        {
            if (state == null || state.rect == null)
                return;

            RectTransform rect = state.rect;

            if (state.originalParent != null)
            {
                rect.SetParent(state.originalParent, false);

                int maxIndex = state.originalParent.childCount - 1;

                rect.SetSiblingIndex(
                    Mathf.Clamp(state.originalSiblingIndex, 0, maxIndex));
            }

            rect.anchorMin = state.originalAnchorMin;
            rect.anchorMax = state.originalAnchorMax;
            rect.pivot = state.originalPivot;
            rect.sizeDelta = state.originalSizeDelta;
            rect.anchoredPosition = state.originalAnchoredPosition;

            rect.localScale = state.originalScale;
            rect.localRotation = state.originalRotation;
            rect.position = state.originalPosition;
        }

        private void SetDragRaycasts(DragState state, bool enabledState)
        {
            if (state == null || state.image == null)
                return;

            Graphic[] graphics =
                state.image.GetComponentsInChildren<Graphic>(true);

            foreach (Graphic graphic in graphics)
            {
                if (graphic == null)
                    continue;

                state.graphics.Add(new GraphicState
                {
                    graphic = graphic,
                    raycastTarget = graphic.raycastTarget
                });

                graphic.raycastTarget = enabledState;
            }
        }

        private void RestoreDragRaycasts(DragState state)
        {
            if (state == null)
                return;

            foreach (GraphicState graphicState in state.graphics)
            {
                if (graphicState.graphic != null)
                {
                    graphicState.graphic.raycastTarget =
                        graphicState.raycastTarget;
                }
            }

            state.graphics.Clear();
        }

        // --------------------------------------------------
        // UI RAYCAST HELPERS
        // --------------------------------------------------

        protected List<RaycastResult> RaycastUI(Vector2 screenPosition)
        {
            uiHits.Clear();

            if (EventSystem.current == null)
                return uiHits;

            PointerEventData pointerData =
                new PointerEventData(EventSystem.current)
                {
                    position = screenPosition
                };

            EventSystem.current.RaycastAll(pointerData, uiHits);

            return uiHits;
        }

        protected T PickUI<T>(Vector2 screenPosition)
            where T : Component
        {
            List<RaycastResult> hits = RaycastUI(screenPosition);

            foreach (RaycastResult hit in hits)
            {
                T component = hit.gameObject.GetComponentInParent<T>();

                if (component != null)
                    return component;
            }

            return null;
        }

        // --------------------------------------------------
        // WORLD RAYCAST HELPERS
        // --------------------------------------------------

        protected T PickWorld<T>(Vector2 screenPosition)
            where T : Component
        {
            if (Cam == null)
                return null;

            Ray ray = Cam.ScreenPointToRay(screenPosition);

            // ตรวจสอบ Collider 3D ทุกตัว เรียงตามระยะ
            RaycastHit[] hits3D = Physics.RaycastAll(ray);

            Array.Sort(hits3D, (a, b) =>
                a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits3D)
            {
                T component = hit.collider.GetComponentInParent<T>();

                if (component != null)
                    return component;
            }

            // ตรวจสอบ Collider 2D
            RaycastHit2D[] hits2D =
                Physics2D.GetRayIntersectionAll(ray);

            Array.Sort(hits2D, (a, b) =>
                a.distance.CompareTo(b.distance));

            foreach (RaycastHit2D hit in hits2D)
            {
                if (hit.collider == null)
                    continue;

                T component = hit.collider.GetComponentInParent<T>();

                if (component != null)
                    return component;
            }

            return null;
        }
    }
}