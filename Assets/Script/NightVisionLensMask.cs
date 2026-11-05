using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    // Geometry leaves the centre transparent without textures or extra materials.
    [RequireComponent(typeof(CanvasRenderer))]
    public class NightVisionLensMask : MaskableGraphic
    {
        private float progress;
        private const int Segments = 96;

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (progress <= 0f || progress >= 1f)
                return;

            Rect bounds = rectTransform.rect;
            float raised = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.65f, progress));
            Vector2 centre = new Vector2(bounds.center.x,
                Mathf.Lerp(bounds.yMin - bounds.height * 0.25f, bounds.center.y, raised));
            float zoom = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, progress));
            float scale = Mathf.Lerp(0.68f, 2.1f, zoom);
            Vector2 radius = new Vector2(bounds.width * 0.49f, bounds.height * 0.45f) * scale;
            float opacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.2f, progress))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, progress)));
            float[] rings = { 0.965f, 1f, 1.035f, 8f };

            for (int ring = 0; ring < rings.Length; ring++)
            {
                Color tint = ring == 1 ? new Color(0.025f, 0.035f, 0.025f, opacity)
                    : new Color(0f, 0f, 0f, ring == 0 ? 0f : opacity);
                for (int i = 0; i <= Segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / Segments;
                    Vector2 point = centre + new Vector2(Mathf.Cos(angle) * radius.x,
                        Mathf.Sin(angle) * radius.y) * rings[ring];
                    mesh.AddVert(point, tint, Vector2.zero);
                }
            }

            for (int ring = 0; ring < rings.Length - 1; ring++)
            {
                for (int i = 0; i < Segments; i++)
                {
                    int a = ring * (Segments + 1) + i;
                    int b = a + Segments + 1;
                    mesh.AddTriangle(a, b, a + 1);
                    mesh.AddTriangle(a + 1, b, b + 1);
                }
            }
        }
    }
}
