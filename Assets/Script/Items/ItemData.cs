using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public float weight = 1f;

    [Header("Item Class")]
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;

    [Header("Effects")]
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

    Equipment,
    IceCream,
    Food,
    Hammer,
    Bomb,
    Potion,
    Magic,
    Nature,
    Machine,
    Toy,
    Curse,
    Treasure
}