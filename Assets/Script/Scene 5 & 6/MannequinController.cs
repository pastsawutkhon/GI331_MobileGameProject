using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Solution
{
    public class MannequinController : MonoBehaviour
    {
        [SerializeField] private GameObject hiddenKeyObject;
        [SerializeField] private Transform organContainer;
        [SerializeField] private CameraSlot cameraSlot;
        [SerializeField] private Image background;
        [SerializeField] private Shader removalShader;
        [SerializeField] private Sprite chestPicture;
        [SerializeField] private Sprite releasedCloseUp;
        [SerializeField] private Sprite releasedRoom;
        [SerializeField] private AudioClip cuffBang;

        // These positions stay with this playthrough, including a trip up the stairs.
        private static readonly Vector2[] bedPositions = new Vector2[3];
        private static bool discovered;
        private static bool awarded;
        private static readonly Rect KeyRect = new Rect(.438f, .373f, .12f, .077f);
        private readonly OrganPiece[] pieces = new OrganPiece[3];
        private readonly int[] offsetIds = new int[3];
        private PickupItem key;
        private Material effect;
        private AudioSource speaker;
        private bool closeUp;
        private float lastVisibility = -1;
        private int dragged = -1;
        private Vector2 grabOffset;

        public bool Dragging => dragged >= 0;
        public bool Releasing { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeSession()
        {
            ClearProgress();
            SceneManager.sceneLoaded -= CheckNewGame;
            SceneManager.sceneLoaded += CheckNewGame;
        }

        private static void CheckNewGame(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Scene 1" && mode == LoadSceneMode.Single) ClearProgress();
        }

        private static void ClearProgress()
        {
            System.Array.Clear(bedPositions, 0, bedPositions.Length);
            discovered = false;
            awarded = false;
            DoorLockReceiver.ResetLock();
        }

        private void Awake()
        {
            key = hiddenKeyObject.GetComponent<PickupItem>();
            hiddenKeyObject.SetActive(false);
            organContainer.gameObject.SetActive(false);
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0;
            effect = new Material(removalShader);
            effect.SetTexture("_Chest", chestPicture.texture);
            effect.SetTexture("_KeyTex", key.itemIcon.texture);
            effect.SetVector("_KeyRect", new Vector4(KeyRect.x, KeyRect.y, KeyRect.width, KeyRect.height));
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = organContainer.GetChild(i).GetComponent<OrganPiece>();
                effect.SetVector("_Organ" + i, pieces[i].Outline);
                offsetIds[i] = Shader.PropertyToID("_Move" + i);
                ShowPosition(i, bedPositions[i]);
            }
            background.material = effect;
            SetCloseUp(false);
        }

        public void SetCloseUp(bool open)
        {
            CancelDrag();
            closeUp = open;
            effect.SetFloat("_CloseUp", open ? 1 : 0);
            effect.SetTexture("_Released", open ? releasedCloseUp.texture : releasedRoom.texture);
            effect.SetFloat("_Free", awarded ? 1 : 0);
            UpdateVisibility();
        }

        private void LateUpdate() => UpdateVisibility();

        private void UpdateVisibility()
        {
            effect.SetFloat("_NightVision", cameraSlot.NightVisionOpacity);
            float visible = closeUp && !awarded && cameraSlot.IsCameraActive ? cameraSlot.NightVisionOpacity : 0;
            if (visible >= .5f) discovered = true;
            if (Mathf.Approximately(visible, lastVisibility)) return;
            lastVisibility = visible;
            effect.SetFloat("_KeyVisibility", visible);
        }

        private Vector2 PicturePoint(Vector2 screen)
        {
            var picture = background.rectTransform;
            Canvas canvas = background.canvas.rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(picture, screen, cam, out var local);
            Rect r = picture.rect;
            return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        }

        public bool PressChest(Vector2 screen)
        {
            if (!closeUp || !discovered || awarded || Dragging) return false;
            Vector2 point = PicturePoint(screen);
            float nearest = 1.7f;
            bool exposed = true;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (bedPositions[i] != Vector2.zero) continue;
                exposed = false;
                float distance = pieces[i].HitDistance(point);
                if (distance < nearest) { nearest = distance; dragged = i; }
            }
            if (Dragging)
            {
                grabOffset = pieces[dragged].Center - point;
                return true;
            }
            if (!exposed || lastVisibility < .2f || !KeyRect.Contains(point)) return false;
            if (key.inventory.AddItem(key.itemName, key.itemIcon, key.amount))
            {
                awarded = true;
                UpdateVisibility();
                StartCoroutine(ReleaseWrist());
            }
            return true;
        }

        public void MoveDrag(Vector2 screen, bool release)
        {
            if (!Dragging) return;
            Vector2 center = PicturePoint(screen) + grabOffset;
            if (!release) { ShowPosition(dragged, center); return; }
            if (FitsOnBed(center)) bedPositions[dragged] = center;
            CancelDrag();
        }

        private bool FitsOnBed(Vector2 center)
        {
            // Exposed wooden planks beside the torso in the close-up artwork.
            if (center.y < .20f || center.y > .53f) return false;
            bool left = center.x >= .16f && center.x <= .28f;
            bool right = center.x >= .73f && center.x <= .84f;
            if (!left && !right) return false;
            foreach (var placed in bedPositions)
                if (placed != Vector2.zero && Vector2.Distance(center, placed) < .075f) return false;
            return true;
        }

        public void CancelDrag()
        {
            if (!Dragging) return;
            ShowPosition(dragged, bedPositions[dragged]);
            dragged = -1;
        }

        private void ShowPosition(int index, Vector2 position)
        {
            Vector2 shift = position - pieces[index].Center;
            effect.SetVector(offsetIds[index], position == Vector2.zero ? Vector4.zero :
                new Vector4(shift.x, shift.y, 1, 0));
        }

        private IEnumerator ReleaseWrist()
        {
            Releasing = true;
            effect.SetFloat("_Free", 1);
            speaker.PlayOneShot(cuffBang, .84f);
            for (float time = 0; time < .38f; time += Time.unscaledDeltaTime)
            {
                float kick = Mathf.Sin(time * 57f + .9f) * Mathf.Exp(-time * 12f);
                effect.SetFloat("_Kick", kick);
                yield return null;
            }
            effect.SetFloat("_Kick", 0);
            Releasing = false;
        }

        private void OnDestroy()
        {
            if (effect) Destroy(effect);
        }
    }
}
