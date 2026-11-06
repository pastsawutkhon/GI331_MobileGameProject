using UnityEngine;
using UnityEngine.EventSystems;

namespace Solution
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HoleSceneExit : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Scene1PictureNavigation navigation;
        [SerializeField] private PalletBarricade pallet;
        [SerializeField] private string destinationScene = "Scene 2";
        public bool IsTransitioning { get; private set; }
        private CanvasGroup hitGroup;

        private void OnEnable()
        {
            hitGroup = GetComponent<CanvasGroup>();
            if (navigation != null) navigation.ViewChanged += ViewChanged;
            if (pallet != null) pallet.Moved += Refresh;
            Refresh();
        }

        private bool Ready => navigation != null && pallet != null &&
            navigation.IsExploring && navigation.CurrentView == 1 && !navigation.IsBusy &&
            !navigation.InteractionLocked && pallet.IsMoved && !pallet.IsMoving && !IsTransitioning;

        private void ViewChanged(int view) { Refresh(); }

        private void Refresh()
        {
            hitGroup.alpha = Ready ? 1f : 0f;
            hitGroup.blocksRaycasts = Ready;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                TryEnter();
        }

        public bool TryEnter()
        {
            if (!Ready) return false;
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError("Scene is not enabled in Build Settings: " + destinationScene, this);
                return false;
            }
            IsTransitioning = true;
            navigation.SetInteractionLocked(true);
            Refresh();
            var inventory = FindFirstObjectByType<Inventory>();
            var cameraSlot = FindFirstObjectByType<CameraSlot>();
            SceneEntranceFade.Begin(destinationScene, inventory,
                cameraSlot != null && cameraSlot.IsCameraActive);
            return true;
        }

        private void OnDisable()
        {
            if (navigation != null) navigation.ViewChanged -= ViewChanged;
            if (pallet != null) pallet.Moved -= Refresh;
        }
    }
}
