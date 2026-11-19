using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    // Draw the dark groove and its light-catching edge as the claws slide down the glass.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GlassClawScratches : MaskableGraphic
    {
        private float revealed;
        private const int Segments = 24;
        private static readonly Vector2[] Tips =
        {
            new Vector2(-.32f, .24f), new Vector2(-.13f, .40f), new Vector2(.12f, .31f)
        };
        private static readonly Color32 Groove = new Color32(32, 24, 24, 235);
        private static readonly Color32 Edge = new Color32(222, 214, 203, 218);

        public void Reveal(float amount)
        {
            if (Mathf.Approximately(revealed, amount)) return;
            revealed = amount;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (revealed <= 0f) return;
            float scale = rectTransform.rect.width;
            for (int claw = 0; claw < Tips.Length; claw++)
            {
                int start = mesh.currentVertCount;
                int steps = Mathf.CeilToInt(revealed * Segments);
                for (int i = 0; i <= steps; i++)
                {
                    float t = Mathf.Min(i / (float)Segments, revealed);
                    float nick = .003f * Mathf.Sin(49f * t + claw);
                    Vector2 point = (Tips[claw] + new Vector2(.19f * t + .08f * t * t + nick,
                        -(.66f + .05f * claw) * t)) * scale;
                    float width = (.0006f + .009f * Mathf.Sin(Mathf.PI * t)) * scale;
                    mesh.AddVert(point - Vector2.right * width, Groove, Vector2.zero);
                    mesh.AddVert(point, Groove, Vector2.zero);
                    mesh.AddVert(point, Edge, Vector2.zero);
                    mesh.AddVert(point + Vector2.right * width * .48f, Edge, Vector2.zero);
                    if (i == 0) continue;
                    int prev = start + (i - 1) * 4;
                    for (int side = prev; side <= prev + 2; side += 2)
                    {
                        mesh.AddTriangle(side, side + 1, side + 4);
                        mesh.AddTriangle(side + 1, side + 5, side + 4);
                    }
                }
            }
        }
    }
}
