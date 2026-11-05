using UnityEngine;

namespace Solution
{
    [RequireComponent(typeof(Collider2D))]
    public class EvidenceBoardTape : MonoBehaviour
    {
        [SerializeField] private GameObject sdCardObject;
        [SerializeField] private Collider2D sdCardCollider;

        private void Start()
        {
            // ซ่อนการกด SD Card ไว้จนกว่าจะลอกเทป
            if (sdCardCollider != null)
                sdCardCollider.enabled = false;
        }

        public void PeelTape()
        {
            gameObject.SetActive(false);
            if (sdCardCollider != null)
                sdCardCollider.enabled = true;
        }
    }
}