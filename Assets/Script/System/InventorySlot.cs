
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    [Serializable]
    public class InventorySlot
    {
        public string itemName = "";
        public Sprite icon;
        public int amount = 0;

        public Image IconImage { get; internal set; }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(itemName) || amount <= 0;
        }

        public void Clear()
        {
            itemName = "";
            icon = null;
            amount = 0;
        }
    }
}
