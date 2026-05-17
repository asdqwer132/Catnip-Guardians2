using System;

[Serializable]
public class SkillCost
{
    public CurrencyType currencyType;
    public int amount;
}
public enum MissileSkillType
{
    Damage,
    Radius,
    Cooldown,
    DropHeight
}
public enum MissileSkillTargetType
{
    DirectDatas,
    TypeDatas,
    AllDatas
}
public enum PlantSkillType
{
    MaxHP,
    GrowTime,
    RewardAmount,
    Open
}