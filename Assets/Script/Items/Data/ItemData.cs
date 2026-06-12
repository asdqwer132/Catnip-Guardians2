using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item/ItemData")]
public class ItemData : DefaultData
{
    [Header("Item Class")]
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;

    [Header("Effects")]
    public float weight = 1f;
    public float cooldown = 0.5f;

    public float Cooldown => cooldown;


    public ItemEffectData[] effectDatas;
}
public enum ItemGrade
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
}
public enum ItemCategory
{
    Attack,
    Heal, 
    Buff,
    Debuff,
    Utility,
    Resource,
    Special
}
public enum ItemSeries
{
    None = 0,
    Weapon = 1,
    Potions = 2
}