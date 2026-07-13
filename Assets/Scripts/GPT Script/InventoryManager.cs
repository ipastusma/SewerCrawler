using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // 실제 인벤토리 데이터
    private readonly List<InventoryItem> items = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// 아이템 획득
    public bool AddItem(InventoryItem item)
    {
        if (HasItem(item.itemID))
            return false;

        items.Add(item);

        Debug.Log($"획득 : {item.displayName}");

        return true;
    }

    /// 아이템 제거
    public bool RemoveItem(string itemID)
    {
        InventoryItem item = GetItem(itemID);

        if (item == null)
            return false;

        items.Remove(item);

        return true;
    }

    /// 아이템 존재 여부
    public bool HasItem(string itemID)
    {
        foreach (InventoryItem item in items)
        {
            if (item.itemID == itemID)
                return true;
        }

        return false;
    }

    /// 아이템 가져오기
    public InventoryItem GetItem(string itemID)
    {
        foreach (InventoryItem item in items)
        {
            if (item.itemID == itemID)
                return item;
        }

        return null;
    }

    /// 인벤토리 전체 목록
    public IReadOnlyList<InventoryItem> GetItems()
    {
        return items;
    }
}