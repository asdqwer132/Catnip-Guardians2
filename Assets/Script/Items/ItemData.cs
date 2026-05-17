using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;

    [TextArea]
    public string description;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Impact Visual")]
    [Tooltip("아이템이 도착했을 때 재생될 이펙트/애니메이션 프리팹")]
    public GameObject impactVfxPrefab;

    [Tooltip("이펙트가 자동으로 사라지는 시간")]
    [Min(0.01f)]
    public float impactVfxLifeTime = 1f;

    [Tooltip("이펙트 크기를 effectRadius에 맞출지 여부")]
    public bool scaleImpactVfxByRadius = true;

    [Header("Item Info")]
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;

    [Header("Weight")]
    [Min(0)]
    public float weight = 1f;

    [Header("Use Cooldown")]
    public float cooldown = 0.5f;

    [Header("Effects")]
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