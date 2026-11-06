using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PalletDustGraphic : MaskableGraphic
    {
        private struct Puff
        {
            public Vector2 position, velocity;
            public float age, life, radius;
        }
        private readonly Puff[] puffs = new Puff[48];
        private int next;

        public void Emit(Vector2 origin, int count)
        {
            for (int i = 0; i < count; i++)
            {
                puffs[next] = new Puff
                {
                    position = origin + new Vector2(Random.Range(-38f, 38f), Random.Range(0f, 8f)),
                    velocity = new Vector2(Random.Range(-40f, 20f), Random.Range(16f, 54f)),
                    life = Random.Range(0.65f, 1.25f), radius = Random.Range(8f, 24f)
                };
                next = (next + 1) % puffs.Length;
            }
            SetVerticesDirty();
        }

        public void ClearDust()
        {
            System.Array.Clear(puffs, 0, puffs.Length);
            SetVerticesDirty();
        }

        private void Update()
        {
            bool changed = false;
            for (int i = 0; i < puffs.Length; i++)
            {
                if (puffs[i].life <= 0f) continue;
                var puff = puffs[i];
                puff.age += Time.unscaledDeltaTime;
                puff.position += puff.velocity * Time.unscaledDeltaTime;
                puff.velocity *= Mathf.Exp(-1.3f * Time.unscaledDeltaTime);
                if (puff.age >= puff.life) puff.life = 0f;
                puffs[i] = puff;
                changed = true;
            }
            if (changed) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int segments = 16;
            foreach (var puff in puffs)
            {
                if (puff.life <= 0f) continue;
                float t = puff.age / puff.life;
                float alpha = Mathf.Min(t * 10f, 1f) * (1f - t) * 0.32f;
                float radius = puff.radius * (0.5f + t * 1.4f);
                int first = mesh.currentVertCount;
                mesh.AddVert(puff.position, new Color(0.30f, 0.28f, 0.22f, alpha), Vector2.zero);
                for (int ring = 0; ring < 2; ring++)
                    for (int j = 0; j <= segments; j++)
                    {
                        float a = j * Mathf.PI * 2f / segments;
                        var pos = puff.position + new Vector2(Mathf.Cos(a) * 1.35f, Mathf.Sin(a))
                            * radius * (ring == 0 ? 0.5f : 1f);
                        mesh.AddVert(pos, new Color(0.30f, 0.28f, 0.22f, ring == 0 ? alpha * 0.45f : 0), Vector2.zero);
                    }
                for (int j = 0; j < segments; j++)
                {
                    int inner = first + 1 + j, outer = inner + segments + 1;
                    mesh.AddTriangle(first, inner, inner + 1);
                    mesh.AddTriangle(inner, outer, inner + 1);
                    mesh.AddTriangle(inner + 1, outer, outer + 1);
                }
            }
        }
    }
}
