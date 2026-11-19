using System;

public enum DataType
{
    None = -1,
    System = 0,
    Item = 1,
    Skill = 2,
    Map = 3,
    Character = 4,
    Enemy = 5,
    Chest = 6,
    ItemMaterial = 7,
}

[Serializable]
public class UnlockedDebugInfo
{
    public DataType unlockType;
    public string unlockId;
}