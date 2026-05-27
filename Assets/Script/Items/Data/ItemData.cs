using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemData : DefaultData
{
    [Header("Item Class")]
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;

    [Header("Effects")]
    public float weight = 1f;
    public float cooldown = 0.5f;
    public ItemEffectData[] effectDatas;
}
public enum ItemGrade
{
    Common,
    Rare,
    Epic,
    Legendary
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
    None,
    Weapon
}