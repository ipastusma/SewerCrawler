using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemID;
    public string displayName;

    public InventoryItem(string id, string name)
    {
        itemID = id;
        displayName = name;
    }
}