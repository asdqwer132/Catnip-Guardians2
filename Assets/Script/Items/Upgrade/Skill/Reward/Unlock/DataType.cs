using System;

public enum DataType
{
    System = 0,
    Item = 1,
    ItemEffect = 10,
    ItemClass = 11,
    ItemCategory = 12,
    Bag = 2,
    BagSlot = 21,
    SkillLine = 6,
    Plant = 7,
}

[Serializable]
public class UnlockedDebugInfo
{
    public DataType unlockType;
    public string unlockId;
}