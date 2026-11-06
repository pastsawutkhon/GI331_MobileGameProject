using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class LampGlowGraphic : MaskableGraphic
    {
        [SerializeField] private bool bulbOnly;
        [SerializeField] private Vector2 bulbPosition = new Vector2(0.27f, 0.665f);
        private float intensity;
        private Rect meshBounds;

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (intensity <= 0f) return;
            Rect r = rectTransform.rect;
            meshBounds = r;
            Vector2 bulb = r.min + Vector2.Scale(r.size, bulbPosition);
            if (bulbOnly)
            {
                Ellipse(mesh, bulb, Vector2.Scale(r.size, new Vector2(0.14f, 0.12f)),
                    new Color(1f, 0.66f, 0.25f, 0.20f * intensity));
            }
            else
            {
                Vector2 pool = r.min + Vector2.Scale(r.size, new Vector2(0.30f, 0.025f));
                Ellipse(mesh, pool, Vector2.Scale(r.size, new Vector2(0.60f, 0.075f)),
                    new Color(0.94f, 0.68f, 0.33f, 0.36f * intensity));
                Ellipse(mesh, bulb, Vector2.Scale(r.size, new Vector2(0.24f, 0.20f)),
                    new Color(1f, 0.64f, 0.26f, 0.12f * intensity));
            }
        }

        private void AddVertex(VertexHelper mesh, Vector2 point, Color tint)
        {
            var uv = new Vector2((point.x - meshBounds.xMin) / Mathf.Max(1f, meshBounds.width),
                (point.y - meshBounds.yMin) / Mathf.Max(1f, meshBounds.height));
            mesh.AddVert(point, tint, uv);
        }

        private void Ellipse(VertexHelper mesh, Vector2 centre, Vector2 radius, Color tint)
        {
            const int segments = 32;
            int first = mesh.currentVertCount;
            AddVertex(mesh, centre, tint);
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float scale = ring == 0 ? 0.3f : ring == 1 ? 0.65f : 1f;
                    var point = centre + Vector2.Scale(radius, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * scale;
                    Color c = tint;
                    c.a *= ring == 0 ? 0.65f : ring == 1 ? 0.18f : 0f;
                    AddVertex(mesh, point, c);
                }
            for (int i = 0; i < segments; i++) mesh.AddTriangle(first, first + 1 + i, first + 2 + i);
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < segments; i++)
                {
                    int a = first + 1 + ring * (segments + 1) + i;
                    int b = a + segments + 1;
                    mesh.AddTriangle(a, b, a + 1);
                    mesh.AddTriangle(a + 1, b, b + 1);
                }
        }
    }
}
