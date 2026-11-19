using UnityEngine;

namespace Solution
{
    public class OrganPiece : MonoBehaviour
    {
        // Centre and radii in the UV space of Zoom Room 1.
        [SerializeField] private Vector4 outline;
        public Vector4 Outline => outline;
        public Vector2 Center => new Vector2(outline.x, outline.y);

        public float HitDistance(Vector2 point)
        {
            Vector2 delta = point - Center;
            return new Vector2(delta.x / outline.z, delta.y / outline.w).sqrMagnitude;
        }
    }
}
