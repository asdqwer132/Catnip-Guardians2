using System;

public enum DataType
{
    Item,
    ItemEffect,
    Bag,
    System,
    ItemClass,
    ItemCategory,
    SkillLine
}

[Serializable]
public class UnlockedDebugInfo
{
    public DataType unlockType;
    public string unlockId;
}