using System;
using System.Collections.Generic;
using UnityEngine;

namespace Solution
{
    // วางไว้ในแต่ละซีนของเกม (ซีนที่มีของและ puzzle) เพื่อเก็บตารางคำใบ้ของซีนนั้น
    // HintSystem ที่อยู่ในซีน UI จะดึงตารางจากตัวนี้เอง
    public class SceneHints : MonoBehaviour
    {
        [Tooltip("เรียงจากบนลงล่าง ข้อแรกที่เงื่อนไขตรงจะถูกใช้")]
        public List<HintEntry> hints = new List<HintEntry>();

        [TextArea] public string fallbackHint;

        private static readonly List<SceneHints> active = new List<SceneHints>();
        public static event Action OnChanged;

        public static SceneHints Current
        {
            get { return active.Count > 0 ? active[active.Count - 1] : null; }
        }

        private void OnEnable()
        {
            active.Add(this);
            OnChanged?.Invoke();
        }

        private void OnDisable()
        {
            active.Remove(this);
            OnChanged?.Invoke();
        }
    }
}
