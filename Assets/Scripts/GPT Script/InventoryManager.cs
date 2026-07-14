using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>인벤토리 데이터만 보관하며 UI나 입력을 처리하지 않습니다.</summary>
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private readonly List<InventoryItem> items = new List<InventoryItem>();
    public IReadOnlyList<InventoryItem> Items => items;
    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() => GameEvents.ItemPicked += HandleItemPicked;
    private void OnDisable() => GameEvents.ItemPicked -= HandleItemPicked;

    private void HandleItemPicked(InventoryItem item) => AddItem(item);

    public bool AddItem(InventoryItem item)
    {
        if (item == null || HasItem(item.itemID)) return false;
        items.Add(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemId)
    {
        InventoryItem item = GetItem(itemId);
        if (item == null) return false;
        items.Remove(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId) => GetItem(itemId) != null;
    public InventoryItem GetItem(string itemId) => items.Find(item => item.itemID == itemId);
}
