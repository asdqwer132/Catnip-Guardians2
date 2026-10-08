using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "GameData/Items/Item")]
public class ItemData : DefaultData
{
    [Header("Item Class")]
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;
    [Tooltip("하위 분류 에셋. 예: 뼈, 깃털. 이름이나 Monster 시리즈 전체로 대상을 추정하지 않습니다.")]
    public ItemTagDefinition[] tags;

    [Header("Effects")]
    public float weight = 1f;
    public float cooldown = 0.5f;

    public float Cooldown => cooldown;


    public ItemEffectData[] effectDatas;

    [Header("Optional Item Completion")]
    [Tooltip("비워두면 효과별 End Visual만 사용합니다. 지정하면 투척/소환/버프/하위 공격이 모두 끝난 뒤 한 번 재생합니다.")]
    public EffectVisualData endVisualData;
    [Tooltip("끄면 최초 도착 위치, 켜면 종료 순간 소유자의 위치에서 재생합니다.")]
    public bool endVisualAtOwner;

    [Header("Items After Completion")]
    [Tooltip("모든 효과가 자연 종료하면 지정한 아이템의 효과를 배열 순서로 같은 시점에 자동 사용합니다. 투척/재고/가방 슬롯/쿨다운은 소비하지 않습니다. 자기 자신 또는 상위 실행 아이템은 건너뜁니다.")]
    public ItemData[] afterCompletionItems;
    [Tooltip("끄면 최초 도착 위치, 켜면 종료 순간 소유자의 위치에서 후속 아이템을 사용합니다.")]
    public bool afterCompletionItemsAtOwner;
    [Tooltip("후속 자동 사용이 횟수제 버프를 소비할지 설정합니다.")]
    public bool afterCompletionConsumeUseBuffs;
    [Tooltip("후속 자동 사용으로 특수 아이템 사용 반응을 호출할지 설정합니다.")]
    public bool afterCompletionTriggerSpecialItems;


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
    Fishing = 5,
    Plant = 6,
    Machine = 7,
    Magic = 8,
}
