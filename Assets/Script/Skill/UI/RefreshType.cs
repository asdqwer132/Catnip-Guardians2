using System;

[Flags]
public enum RefreshType
{
    None = 0,

    SkillTree = 1 << 0,
    Unlock = 1 << 1,
    Currency = 1 << 2,
    Inventory = 1 << 3,
    Equipment = 1 << 4,
    Bag = 1 << 5,
    Buff = 1 << 6,
    Shop = 1 << 7,
    Item = 1 << 8,

    All = ~0
}