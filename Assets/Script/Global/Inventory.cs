using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryEntry
{
    public string objectName;
    public int quantity;
    public InventoryEntry(string amount, int objectPosseded)
    {
        this.objectName = amount;
        this.quantity = objectPosseded;
    }
}

[System.Serializable]
public class InventoryList
{
    public List<InventoryEntry> highScores = new List<InventoryEntry>();
}

public enum ObjectType
{
    Skin,
    Usable
}
