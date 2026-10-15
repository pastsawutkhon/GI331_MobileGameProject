using Solution;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    public int slotCount = 5;
    public List<InventorySlot> slots = new List<InventorySlot>();

    public Dictionary<string, int> inventory =
        new Dictionary<string, int>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        while (slots.Count < slotCount)
            slots.Add(new InventorySlot());

        if (slots.Count > slotCount)
            slots.RemoveRange(slotCount, slots.Count - slotCount);
    }

    public bool AddItem(string item, Sprite icon, int amount = 1)
    {
        if (amount <= 0) return false;

        // ถ้ามีไอเท็มชนิดเดิม ให้เพิ่มจำนวนในช่องเดิม
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty() && slot.itemName == item)
            {
                slot.amount += amount;
                UpdateDictionary();
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // หา slot ว่างจากซ้ายไปขวา
        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.itemName = item;
                slot.icon = icon;
                slot.amount = amount;

                UpdateDictionary();
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    // ใช้ของจากช่องที่กด
    public bool UseItem(int index)
    {
        if (index < 0 || index >= slots.Count)
            return false;

        InventorySlot slot = slots[index];

        if (slot.IsEmpty())
            return false;

        slot.amount--;

        if (slot.amount <= 0)
            slot.Clear();

        UpdateDictionary();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string item, int amount)
    {
        return GetItemCount(item) >= amount;
    }

    public int GetItemCount(string item)
    {
        int total = 0;

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty() && slot.itemName == item)
                total += slot.amount;
        }

        return total;
    }

    private void UpdateDictionary()
    {
        inventory.Clear();

        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty()) continue;

            if (inventory.ContainsKey(slot.itemName))
                inventory[slot.itemName] += slot.amount;
            else
                inventory.Add(slot.itemName, slot.amount);
        }
    }
}
