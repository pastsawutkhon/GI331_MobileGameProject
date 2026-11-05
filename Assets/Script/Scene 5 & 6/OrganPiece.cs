using UnityEngine;

namespace Solution
{
    [RequireComponent(typeof(Collider2D))]
    public class OrganPiece : MonoBehaviour
    {
        [SerializeField] private MannequinController mannequin;

        public void RemovePiece()
        {
            gameObject.SetActive(false);
            if (mannequin != null)
            {
                mannequin.OnOrganRemoved();
            }
        }
    }
}