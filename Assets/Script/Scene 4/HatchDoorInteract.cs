using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Solution
{
    public class HatchDoorInteract : PressAndDragBase
    {

        [Header("Navigation Reference")]
        [SerializeField] private Scene4PictureNavigation sceneNavigation;

        [Header("Background Image")]
        [SerializeField] private Image roomBackgroundImage;

        [Header("Hatch Sprites")]
        [SerializeField] private Sprite closedHatchSprite;
        [SerializeField] private Sprite openHatchSprite;
        [SerializeField] private Graphic hatchTouchZone;

        [Header("Audio & Events")]
        [SerializeField] private AudioSource openSound;
        public UnityEvent OnHatchOpened;

        [Header("Center View")]
        [SerializeField] private Image centerBackgroundImage;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.2f;

        [Header("Symbol")]
        [SerializeField] private RectTransform symbolTouchZone;
        [SerializeField] private Image symbolCloseUp;

        [Header("Candles")]
        [SerializeField] private RectTransform candleTouchZone;
        [SerializeField] private Image candleCloseUp;

        [Header("Right View - Board")]
        [SerializeField] private RectTransform boardTouchZone;
        [SerializeField] private Image boardCloseUp;

        [Header("Paper on Board")]
        [SerializeField] private RectTransform paperTouchZone;
        [SerializeField] private Image paperCloseUp;
        [SerializeField] private CameraSlot nightVisionCamera;
        [SerializeField] private Sprite nightVisionPaper;

        [Header("Close-up Controls")]
        [SerializeField] private GameObject undoButton;
        [SerializeField] private GameObject lookControls;

        [Header("Night Vision Touch Hints")]
        [SerializeField] private RectTransform documentFold;
        [SerializeField] private Sprite fingerprintSprite;
        [SerializeField] private Shader fingerprintShader;
        private Canvas hintCanvas;
        private Material hintInk;
        private readonly Image[] hints = new Image[4];

        [Header("Board apparition")]
        [SerializeField] private Sprite boardGhostSprite;
        [SerializeField] private AudioClip boardHandImpact;
        private Canvas scareCanvas;
        private bool readBoardPaper;
        private bool boardScareSeen;
        private Image boardGhost;
        private Color roomLightBeforeGhost;

        private Sprite originalSprite;
        private enum ZoomView { Room, CenterDetail, Candles, Board, Paper }
        private ZoomView zoomView;
        private bool fading;

        private bool isOpened = false;

        private void LateUpdate()
        {
            UpdateTouchHints();
            UpdateCandleNumbers();
            if (zoomView != ZoomView.Paper || !centerBackgroundImage || !paperCloseUp) return;

            bool showClue = nightVisionCamera && nightVisionCamera.IsCameraActive && nightVisionPaper;
            Sprite paper = showClue ? nightVisionPaper : paperCloseUp.sprite;
            if (centerBackgroundImage.sprite != paper)
                centerBackgroundImage.sprite = paper;
        }

        // Positions above the wicks in Candles.png, measured from its bottom left.
        private static readonly Vector2[] candlePositions = {
            new Vector2(.500f, .822f), new Vector2(.855f, .654f),
            new Vector2(.797f, .439f), new Vector2(.500f, .334f),
            new Vector2(.203f, .439f), new Vector2(.140f, .654f)
        };
        // Wick tips in the original 941 x 1672 candle artwork, clockwise from north.
        private static readonly Vector2[] wickPixels = {
            new Vector2(471,349), new Vector2(809,623), new Vector2(751,987),
            new Vector2(472,1155), new Vector2(188,984), new Vector2(130,621)
        };
        // Wick tips measured on the 480 x 860 preview of Background 2 Main.
        private static readonly Vector2[] mainWicks = {
            new Vector2(239f/480f, 1f-539f/860f), new Vector2(368f/480f, 1f-573f/860f),
            new Vector2(370f/480f, 1f-642f/860f), new Vector2(245f/480f, 1f-679f/860f),
            new Vector2(120f/480f, 1f-641f/860f), new Vector2(117f/480f, 1f-573f/860f)
        };
        private const float ArtworkWidth = 941f;
        private const float ArtworkHeight = 1672f;
        private static readonly Vector2 flamePivot = new Vector2(.5f, .48f);

        [Header("Candle Puzzle")]
        public UnityEvent onCandlePuzzleSolved = new UnityEvent();
        public bool CandlePuzzleSolved => progress == lit.Length;

        private static bool MatchesDirection(int step, int candle)
        {
            switch (step)
            {
                case 0:
                case 1: return candle == 1 || candle == 2;
                case 2:
                case 3: return candle == 4 || candle == 5;
                case 4: return candle == 0;
                case 5: return candle == 3;
                default: return false;
            }
        }
        private readonly bool[] lit = new bool[6];
        private readonly Image[] flames = new Image[6];
        private Sprite flameSprite;
        private Texture2D flameTexture;
        private int progress;

        private static readonly string[] digitText = { "", "1", "2", "3", "4", "5", "6" };
        private readonly int[] digits = { 1, 2, 3, 4, 5, 6 };
        private readonly Text[] numberLabels = new Text[6];
        private Canvas numberCanvas;
        private bool cameraWasActive;

        private void UpdateCandleNumbers()
        {
            bool active = nightVisionCamera && nightVisionCamera.IsCameraActive;
            if (active && !cameraWasActive)
            {
                // A random single cycle moves every digit to a different candle.
                for (int i = digits.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i);
                    (digits[i], digits[j]) = (digits[j], digits[i]);
                }
                if (!CandlePuzzleSolved) ResetCandles();
            }
            cameraWasActive = active;
            bool wide = CandlePuzzleSolved && zoomView == ZoomView.Room && sceneNavigation &&
                sceneNavigation.IsExploring && sceneNavigation.CurrentView == 0;
            bool show = (wide || (zoomView == ZoomView.Candles && (active || progress > 0))) && centerBackgroundImage &&
                centerBackgroundImage.isActiveAndEnabled && centerBackgroundImage.canvas;
            if (!show)
            {
                if (numberCanvas && numberCanvas.gameObject.activeSelf)
                    numberCanvas.gameObject.SetActive(false);
                return;
            }
            if (!numberCanvas) CreateNumberCanvas();
            if (!numberCanvas.gameObject.activeSelf) numberCanvas.gameObject.SetActive(true);

            Canvas root = centerBackgroundImage.canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null :
                (root.worldCamera ? root.worldCamera : Cam);
            Rect rect = PictureRect();
            RectTransform picture = centerBackgroundImage.rectTransform;
            Vector2 left = RectTransformUtility.WorldToScreenPoint(camera, picture.TransformPoint(rect.min));
            Vector2 right = RectTransformUtility.WorldToScreenPoint(camera,
                picture.TransformPoint(new Vector2(rect.xMax, rect.yMin)));
            int fontSize = Mathf.Max(12, Mathf.RoundToInt(Vector2.Distance(left, right) * .065f));
            float alpha = (active ? nightVisionCamera.NightVisionOpacity : 0f) * (1f - fadeOverlay.alpha);
            for (int i = 0; i < numberLabels.Length; i++)
            {
                Text label = numberLabels[i];
                int value = digits[i];
                if (label.text != digitText[value]) label.text = digitText[value];
                label.enabled = active && !wide;
                Vector2 point = candlePositions[i];
                Vector2 local = Rect.NormalizedToPoint(rect, point);
                label.rectTransform.position = RectTransformUtility.WorldToScreenPoint(camera, picture.TransformPoint(local));
                Image flame = flames[i];
                flame.enabled = lit[i];
                if (flame.enabled)
                {
                    Vector2 uv = wide ? mainWicks[i] : new Vector2(wickPixels[i].x / ArtworkWidth, 1f - wickPixels[i].y / ArtworkHeight);
                    Vector2 wick = Rect.NormalizedToPoint(rect, uv);
                    Vector2 screenWick = RectTransformUtility.WorldToScreenPoint(camera, picture.TransformPoint(wick));
                    Vector2 xAxis = RectTransformUtility.WorldToScreenPoint(camera,
                        picture.TransformPoint(wick + Vector2.right * (rect.width * (wide ? 43f / 480f : 124f / ArtworkWidth)))) - screenWick;
                    Vector2 yAxis = RectTransformUtility.WorldToScreenPoint(camera,
                        picture.TransformPoint(wick + Vector2.up * (rect.height * (wide ? 43f / 860f : 124f / ArtworkHeight)))) - screenWick;
                    float t = Time.unscaledTime;
                    float sway = Mathf.PerlinNoise(i * 8.7f + 1f, t * 2.3f) * 2f - 1f;
                    float pulse = Mathf.Sin(t * (5.1f + i * .27f) + i * 2.4f);
                    RectTransform rectFlame = flame.rectTransform;
                    rectFlame.position = screenWick;
                    rectFlame.sizeDelta = new Vector2(xAxis.magnitude * (1f + .035f * pulse),
                        yAxis.magnitude * (1f + .09f * pulse + .06f * sway));
                    rectFlame.localRotation = Quaternion.Euler(0, 0,
                        Mathf.Atan2(yAxis.y, yAxis.x) * Mathf.Rad2Deg - 90f + sway * 8f);
                    flame.color = new Color(1f, 1f, 1f, (1f - fadeOverlay.alpha) * (.95f + .05f * pulse));
                    label.rectTransform.position += Vector3.up * fontSize * .8f;
                }
                label.fontSize = fontSize;
                label.color = new Color(1f, .80f, .28f, alpha);
            }
        }

        private void CreateNumberCanvas()
        {
            // Draw after camera grading so the clues stay gold in night vision.
            var root = new GameObject("Candle Numbers", typeof(RectTransform), typeof(Canvas));
            numberCanvas = root.GetComponent<Canvas>();
            numberCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            numberCanvas.sortingOrder = -2;
            CreateFlameSprite();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < numberLabels.Length; i++)
            {
                var go = new GameObject("Candle " + (i + 1), typeof(RectTransform), typeof(Text));
                go.transform.SetParent(root.transform, false);
                Text label = go.GetComponent<Text>();
                label.font = font;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                numberLabels[i] = label;
                var flame = new GameObject("Flame " + (i + 1), typeof(RectTransform), typeof(Image));
                flame.transform.SetParent(root.transform, false);
                flames[i] = flame.GetComponent<Image>();
                flames[i].sprite = flameSprite;
                flames[i].rectTransform.pivot = flamePivot;
                flames[i].raycastTarget = false;
                flames[i].enabled = false;
            }
        }

        private Rect PictureRect()
        {
            Rect rect = centerBackgroundImage.GetPixelAdjustedRect();
            if (centerBackgroundImage.preserveAspect && centerBackgroundImage.sprite)
            {
                Vector2 size = centerBackgroundImage.sprite.rect.size;
                float scale = Mathf.Min(rect.width / size.x, rect.height / size.y);
                Vector2 fitted = size * scale;
                rect.position += Vector2.Scale(rect.size - fitted, centerBackgroundImage.rectTransform.pivot);
                rect.size = fitted;
            }

            return rect;
        }

        private void CreateFlameSprite()
        {
            const int size = 128;
            flameTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            flameTexture.wrapMode = TextureWrapMode.Clamp;
            flameTexture.filterMode = FilterMode.Bilinear;
            Color glowColor = new Color(1f, .37f, .12f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size - flamePivot.x;
                float dy = (y + .5f) / size - flamePivot.y;
                float radiusSquared = dx * dx + (dy + .012f) * (dy + .012f);
                float glow = .32f * Mathf.Exp(-radiusSquared * 32f)
                    + .35f * Mathf.Exp(-radiusSquared * 220f);
                float t = dy / .235f;
                float core = 0f;
                Color coreColor = Color.white;
                if (t > 0f && t < 1f)
                {
                    float width = .032f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, .48f)) * (1f - .46f * t);
                    float distance = Mathf.Abs(dx - .012f * t * t) / Mathf.Max(width, .001f);
                    core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.72f, 1.18f, distance));
                    coreColor = Color.Lerp(new Color(1f, .98f, .83f), new Color(1f, .61f, .26f),
                        Mathf.Clamp01(distance * distance + .25f * t));
                }
                float alpha = core + glow * (1f - core);
                Color tint = Color.Lerp(glowColor, coreColor, core / Mathf.Max(alpha, .0001f));
                tint.a = alpha;
                flameTexture.SetPixel(x, y, tint);
            }
            flameTexture.Apply(false, true);
            flameSprite = Sprite.Create(flameTexture, new Rect(0, 0, size, size), flamePivot,
                100f, 0, SpriteMeshType.FullRect);
        }

        private void ResetCandles()
        {
            System.Array.Clear(lit, 0, lit.Length);
            progress = 0;
        }

        private bool PressCandle(Vector2 position)
        {
            if (CandlePuzzleSolved || Contains(undoButton.transform as RectTransform, position)) return false;
            Canvas root = centerBackgroundImage.canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null :
                (root.worldCamera ? root.worldCamera : Cam);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                centerBackgroundImage.rectTransform, position, camera, out Vector2 local)) return false;
            Rect rect = PictureRect();
            if (!rect.Contains(local)) return false;
            Vector2 point = Rect.PointToNormalized(rect, local);
            for (int i = 0; i < digits.Length; i++)
            {
                Vector2 offset = point - candlePositions[i];
                if (Mathf.Abs(offset.x) > .083f || offset.y > .03f || offset.y < -.11f) continue;
                if (lit[i]) return true;
                if (!MatchesDirection(progress, i))
                {
                    ResetCandles();
                    return true;
                }
                lit[i] = true;
                progress++;
                if (CandlePuzzleSolved)
                {
                    OpenHatch();
                    onCandlePuzzleSolved.Invoke();
                }
                return true;
            }
            return false;
        }

        protected override void OnDestroy()
        {
            if (hintCanvas) Destroy(hintCanvas.gameObject);
            if (hintInk) Destroy(hintInk);
            if (numberCanvas) Destroy(numberCanvas.gameObject);
            if (flameSprite) Destroy(flameSprite);
            if (flameTexture) Destroy(flameTexture);
            base.OnDestroy();
        }

        protected override void Initialize()
        {
            base.Initialize();

            // ตั้งค่าเริ่มต้นให้เป็นรูป Sprite ฝาปิด
            if (roomBackgroundImage != null && closedHatchSprite != null)
            {
                roomBackgroundImage.sprite = closedHatchSprite;
            }
        }

        protected override bool OnPressed(Vector2 screenPosition)
        {
            if (sceneNavigation != null && sceneNavigation.CurrentView >= 0)
                return HandleZoomPress(screenPosition);

            if (!CandlePuzzleSolved || !isOpened || !sceneNavigation || !sceneNavigation.IsExploring ||
                sceneNavigation.CurrentView != -1 || sceneNavigation.IsBusy || sceneNavigation.InteractionLocked ||
                !hatchTouchZone || !Contains(hatchTouchZone.rectTransform, screenPosition))
                return false;

            if (SceneEntranceFade.InProgress) return false;
            const string destination = "Scene 5";
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError("Cannot load " + destination, this);
                return false;
            }
            sceneNavigation.SetInteractionLocked(true);
            bool sawHand = boardScareSeen;
            bool readPaper = readBoardPaper;
            int[] savedDigits = (int[])digits.Clone();
            SceneEntranceFade.Begin(destination, FindAnyObjectByType<InventoryUI>().inventory,
                nightVisionCamera && nightVisionCamera.IsCameraActive, () =>
                {
                    var room = FindAnyObjectByType<WorldInteractionHandler>();
                    if (room) room.AfterStairs = () => RestoreAtDoor(readPaper, sawHand, savedDigits);
                });
            return true;
        }

        internal static void RestoreAtDoor(bool readPaper, bool sawHand, int[] savedDigits)
        {
            var hatch = FindAnyObjectByType<HatchDoorInteract>();
            if (!hatch) return;
            hatch.progress = hatch.lit.Length;
            for (int i = 0; i < hatch.lit.Length; i++) hatch.lit[i] = true;
            hatch.isOpened = true;
            hatch.readBoardPaper = readPaper;
            hatch.boardScareSeen = sawHand;
            if (savedDigits != null) savedDigits.CopyTo(hatch.digits, 0);
            hatch.cameraWasActive = hatch.nightVisionCamera && hatch.nightVisionCamera.IsCameraActive;
            if (hatch.roomBackgroundImage) hatch.roomBackgroundImage.sprite = hatch.openHatchSprite;
            hatch.ShowZoomControls(false);
            hatch.sceneNavigation.ReturnToOpenDoor(hatch.openHatchSprite);
            foreach (var document in FindObjectsByType<Document>())
            {
                if (document is lightEvent lights) lights.ShowSceneSymbol();
                else document.RestoreAfterHatchReturn();
            }
        }

        private bool HandleZoomPress(Vector2 screenPosition)
        {
            if (!centerBackgroundImage || !fadeOverlay || !undoButton || !lookControls ||
                !sceneNavigation.IsExploring || sceneNavigation.IsBusy || fading)
                return false;

            if (zoomView == ZoomView.Candles) return PressCandle(screenPosition);

            if (zoomView == ZoomView.Board)
            {
                if (Contains(undoButton.transform as RectTransform, screenPosition) ||
                    !Contains(paperTouchZone, screenPosition)) return false;
                return OpenDetail(paperCloseUp, ZoomView.Paper);
            }

            if (zoomView != ZoomView.Room || sceneNavigation.InteractionLocked) return false;

            if (sceneNavigation.CurrentView == 1)
                return Contains(boardTouchZone, screenPosition) && OpenDetail(boardCloseUp, ZoomView.Board);

            if (Contains(symbolTouchZone, screenPosition))
                return OpenDetail(symbolCloseUp, ZoomView.CenterDetail);
            return Contains(candleTouchZone, screenPosition) && OpenDetail(candleCloseUp, ZoomView.Candles);
        }

        private bool OpenDetail(Image source, ZoomView nextView)
        {
            if (!source || !source.sprite) return false;
            if (zoomView == ZoomView.Room)
            {
                originalSprite = centerBackgroundImage.sprite;
                sceneNavigation.SetInteractionLocked(true);
            }
            StartCoroutine(ChangePicture(source.sprite, nextView));
            return true;
        }

        public void CloseZoom()
        {
            if (!isActiveAndEnabled || fading || zoomView == ZoomView.Room) return;
            if (zoomView == ZoomView.Paper)
                StartCoroutine(ChangePicture(boardCloseUp.sprite, ZoomView.Board));
            else
                StartCoroutine(ChangePicture(originalSprite, ZoomView.Room));
        }

        private void ShowZoomControls(bool zoomed)
        {
            lookControls.SetActive(!zoomed);
            undoButton.SetActive(zoomed);
        }

        private bool Contains(RectTransform zone, Vector2 position)
        {
            if (!zone || !zone.gameObject.activeInHierarchy) return false;
            Canvas canvas = zone.GetComponentInParent<Canvas>();
            if (!canvas) return false;
            Canvas root = canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null :
                (root.worldCamera != null ? root.worldCamera : Cam);
            return RectTransformUtility.RectangleContainsScreenPoint(zone, position, camera);
        }

        private IEnumerator ChangePicture(Sprite sprite, ZoomView nextView)
        {
            fading = true;
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.blocksRaycasts = true;
            yield return FadeTo(1f);
            centerBackgroundImage.sprite = sprite;
            bool returnedFromBoard = zoomView == ZoomView.Board && nextView == ZoomView.Room;
            zoomView = nextView;
            if (nextView == ZoomView.Paper) readBoardPaper = true;
            ShowZoomControls(nextView != ZoomView.Room);
            yield return FadeTo(0f);
            if (returnedFromBoard && readBoardPaper && !boardScareSeen && boardGhostSprite &&
                sceneNavigation.CurrentView == 1)
                yield return BoardApparition();
            fadeOverlay.blocksRaycasts = false;
            fading = false;
            if (nextView == ZoomView.Room) sceneNavigation.SetInteractionLocked(false);
        }

        private void UpdateTouchHints()
        {
            bool night = nightVisionCamera && nightVisionCamera.IsCameraActive &&
                nightVisionCamera.NightVisionOpacity > 0f;
            bool document = documentFold && documentFold.gameObject.activeInHierarchy;
            bool room = !document && sceneNavigation && sceneNavigation.IsExploring &&
                !sceneNavigation.InteractionLocked && !sceneNavigation.IsBusy && !fading &&
                zoomView == ZoomView.Room && centerBackgroundImage && centerBackgroundImage.isActiveAndEnabled;
            bool center = room && sceneNavigation.CurrentView == 0;
            bool right = room && sceneNavigation.CurrentView == 1;
            bool visible = night && (document || center || right) && fingerprintSprite && fingerprintShader;
            if (!visible)
            {
                if (hintCanvas && hintCanvas.gameObject.activeSelf) hintCanvas.gameObject.SetActive(false);
                return;
            }
            if (!hintCanvas)
            {
                var root = new GameObject("Night Vision Touch Hints", typeof(RectTransform), typeof(Canvas));
                hintCanvas = root.GetComponent<Canvas>();
                hintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hintCanvas.sortingOrder = -2;
                hintInk = new Material(fingerprintShader);
                string[] names = { "Document fold", "Symbol", "Candles", "Board" };
                for (int i = 0; i < hints.Length; i++)
                {
                    var hint = new GameObject(names[i], typeof(RectTransform), typeof(Image));
                    hint.transform.SetParent(root.transform, false);
                    Image image = hints[i] = hint.GetComponent<Image>();
                    image.sprite = fingerprintSprite;
                    image.material = hintInk;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
                }
            }
            if (!hintCanvas.gameObject.activeSelf) hintCanvas.gameObject.SetActive(true);
            PlaceHint(hints[0], documentFold, document);
            PlaceHint(hints[1], symbolTouchZone, center);
            PlaceHint(hints[2], candleTouchZone, center);
            PlaceHint(hints[3], boardTouchZone, right);
        }

        private void PlaceHint(Image image, RectTransform target, bool visible)
        {
            image.enabled = visible && target && target.gameObject.activeInHierarchy;
            if (!image.enabled) return;
            Canvas root = target.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null :
                (root.worldCamera ? root.worldCamera : Cam);
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
            RectTransform canvasRect = (RectTransform)hintCanvas.transform;
            image.rectTransform.anchoredPosition = point / hintCanvas.scaleFactor;
            image.rectTransform.sizeDelta = Vector2.one * Mathf.Min(canvasRect.rect.width, canvasRect.rect.height) * .105f;
            image.color = new Color(1f, .78f, .24f, nightVisionCamera.NightVisionOpacity);
        }

        private IEnumerator BoardApparition()
        {
            boardScareSeen = true;
            roomLightBeforeGhost = centerBackgroundImage.color;
            GameObject layer = new GameObject("Board Hand Scare", typeof(RectTransform), typeof(Canvas), typeof(AudioSource));
            scareCanvas = layer.GetComponent<Canvas>();
            scareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scareCanvas.sortingOrder = 30000;
            AudioSource speaker = layer.GetComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            var scratches = new GameObject("Claw scratches on glass", typeof(RectTransform),
                typeof(GlassClawScratches)).GetComponent<GlassClawScratches>();
            scratches.transform.SetParent(layer.transform, false);
            scratches.raycastTarget = false;
            var hand = new GameObject("Hand", typeof(RectTransform), typeof(Image));
            hand.transform.SetParent(layer.transform, false);
            boardGhost = hand.GetComponent<Image>();
            boardGhost.sprite = boardGhostSprite;
            boardGhost.preserveAspect = true;
            boardGhost.raycastTarget = false;
            RectTransform rt = boardGhost.rectTransform;
            rt.anchorMin = new Vector2(-.05f, -.08f);
            rt.anchorMax = new Vector2(1.05f, .94f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            boardGhost.enabled = false;

            yield return new WaitForSecondsRealtime(.30f);
            SetBoardLight(.24f);
            yield return new WaitForSecondsRealtime(.11f);
            SetBoardLight(.78f);
            yield return new WaitForSecondsRealtime(.14f);
            SetBoardLight(.045f);
            yield return new WaitForSecondsRealtime(.15f);

            boardGhost.enabled = true;
            if (boardHandImpact) speaker.PlayOneShot(boardHandImpact, .82f);
            float elapsed = 0f;
            while (elapsed < .12f)
            {
                float t = Mathf.Clamp01(elapsed / .12f);
                rt.localScale = Vector3.one * Mathf.Lerp(.55f, 1.06f, 1f - (1f-t)*(1f-t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            rt.localScale = Vector3.one * 1.06f;
            float handWidth = Mathf.Min(rt.rect.width, rt.rect.height *
                boardGhostSprite.rect.width / boardGhostSprite.rect.height) * 1.06f;
            scratches.rectTransform.position = rt.position;
            scratches.rectTransform.sizeDelta = new Vector2(handWidth, handWidth);
            SetBoardLight(.73f);
            yield return new WaitForSecondsRealtime(.22f);
            SetBoardLight(.10f);
            yield return new WaitForSecondsRealtime(.20f);

            elapsed = 0f;
            while (elapsed < 1.18f)
            {
                float t = Mathf.Clamp01(elapsed / 1.18f);
                SetBoardLight(Mathf.Lerp(.10f, 1f, Mathf.SmoothStep(0f, 1f, t / .70f)));
                float slide = Mathf.SmoothStep(0f, 1f, t);
                scratches.Reveal(Mathf.Clamp01(slide * 2.8f));
                Rect screen = ((RectTransform)layer.transform).rect;
                rt.anchoredPosition = new Vector2(screen.width * .28f, -screen.height * 1.18f) * slide;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            scratches.Reveal(1f);
            yield return new WaitForSecondsRealtime(.45f);
            for (float time = 0f; time < .4f; time += Time.unscaledDeltaTime)
            {
                scratches.canvasRenderer.SetAlpha(1f - time / .4f);
                yield return null;
            }
            FinishBoardApparition();
        }

        private void SetBoardLight(float brightness)
        {
            Color room = roomLightBeforeGhost * brightness;
            room.a = roomLightBeforeGhost.a;
            centerBackgroundImage.color = room;
        }

        private void FinishBoardApparition()
        {
            if (!boardGhost) return;
            if (centerBackgroundImage) centerBackgroundImage.color = roomLightBeforeGhost;
            boardGhost.enabled = false;
            Destroy(scareCanvas.gameObject);
            scareCanvas = null;
            boardGhost = null;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = fadeOverlay.alpha;
            float duration = Mathf.Max(0.05f, fadeDuration);
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                fadeOverlay.alpha = Mathf.SmoothStep(start, target, time / duration);
                yield return null;
            }
            fadeOverlay.alpha = target;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (hintCanvas) hintCanvas.gameObject.SetActive(false);
            FinishBoardApparition();
            if (numberCanvas) numberCanvas.gameObject.SetActive(false);
            if (!fading && zoomView == ZoomView.Room) return;
            StopAllCoroutines();
            if (centerBackgroundImage) centerBackgroundImage.sprite = originalSprite;
            if (undoButton && lookControls) ShowZoomControls(false);
            if (fadeOverlay)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            if (sceneNavigation) sceneNavigation.SetInteractionLocked(false);
            fading = false;
            zoomView = ZoomView.Room;
        }
        public void OpenHatch()
        {
            if (!CandlePuzzleSolved || isOpened) return;
            isOpened = true;

            // สลับ Sprite ของพื้นหลังเป็นรูปฝาเปิดทันที
            if (roomBackgroundImage != null && openHatchSprite != null)
            {
                roomBackgroundImage.sprite = openHatchSprite;
            }

            // เล่นเสียงเปิดฝา (ถ้ามี)
            if (openSound != null)
            {
                openSound.Play();
            }

            // ปิดการรับการแตะตรงฝา ไม่ให้แตะซ้ำ
            if (hatchTouchZone != null)
            {
                hatchTouchZone.raycastTarget = false;
            }

            Debug.Log("แตะเปิดฝาทางลงใต้ดินแล้ว!");
            OnHatchOpened?.Invoke();
        }
    }
}