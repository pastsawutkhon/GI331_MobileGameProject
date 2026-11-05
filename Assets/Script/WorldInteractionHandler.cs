using UnityEngine;

namespace Solution
{
    public class WorldInteractionHandler : PressAndDragBase
    {
        protected override bool OnPressed(Vector2 screenPosition)
        {
            // แตะชิ้นส่วนอวัยวะ
            OrganPiece organ = PickWorld<OrganPiece>(screenPosition);
            if (organ != null)
            {
                organ.RemovePiece();
                return true;
            }

            // แตะลอกเทปกาว
            EvidenceBoardTape tape = PickWorld<EvidenceBoardTape>(screenPosition);
            if (tape != null)
            {
                tape.PeelTape();
                return true;
            }

            return false;
        }
    }
}