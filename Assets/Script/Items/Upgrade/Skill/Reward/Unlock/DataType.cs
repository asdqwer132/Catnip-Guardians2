using System;

public enum DataType
{
    System = 0,
    Item = 1,
    Skill = 2,
    Map = 3,
    Character = 4,
    Enemy = 5,
    Box = 6,
}

[Serializable]
public class UnlockedDebugInfo
{
    public DataType unlockType;
    public string unlockId;
}