using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public sealed class DocumentNightVision : MonoBehaviour
    {
        [SerializeField] private CameraSlot cameraSlot;
        private Graphic[] graphics;
        private Color[] original;
        private float applied;

        private void OnEnable()
        {
            graphics = GetComponentsInChildren<Graphic>(true);
            original = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                original[i] = graphics[i].color;
            applied = -1f;
            LateUpdate();
        }

        private void LateUpdate()
        {
            float weight = cameraSlot ? cameraSlot.NightVisionOpacity : 0f;
            if (Mathf.Approximately(weight, applied)) return;
            Color filter = Color.Lerp(Color.white, new Color(.22f, 1f, .28f), weight);
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i]) graphics[i].color = original[i] * filter;
            applied = weight;
        }

        private void OnDisable()
        {
            if (graphics == null) return;
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i]) graphics[i].color = original[i];
        }
    }
}
