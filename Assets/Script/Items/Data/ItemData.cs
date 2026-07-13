using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "GameData/Item/ItemData")]
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
    None = -1,
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
}
public enum ItemCategory
{
    None = -1,
    Attack = 0,
    Heal = 1, 
    Buff = 2,
    Debuff = 3,
    Utility = 4,
    Resource = 5,
    Special = 6
}
public enum ItemSeries
{
    Potions = -1,
    None = 0,
    Weapon = 1,
    Food = 2,
    Mineral = 3,
    Monster = 4,
    Present = 5,
    Plant = 6,
    Machine = 7,
    Magic = 8,
}