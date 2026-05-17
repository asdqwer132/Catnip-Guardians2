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

    [Header("Effect Stat")]
    public EffectStat effectStat = new EffectStat();

    [Header("Throw Setting")]
    [Tooltip("아이템마다 투척 설정을 따로 사용할지")]
    public bool overrideThrowSetting = false;

    [Tooltip("목표 지점까지 도착하는 시간")]
    [Min(0.01f)]
    public float throwArriveTime = 0.6f;

    [Tooltip("포물선 높이")]
    [Min(0f)]
    public float throwArcHeight = 1.2f;

    [Tooltip("거리 기반 자동 포물선 높이 사용")]
    public bool autoArcHeightByDistance = true;

    [Min(0f)]
    public float minArcHeight = 0.5f;

    [Min(0f)]
    public float maxArcHeight = 2.5f;

    [Min(0f)]
    public float arcHeightDistanceMultiplier = 0.25f;

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