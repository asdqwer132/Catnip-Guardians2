using UnityEngine;

[CreateAssetMenu(fileName = "ReactiveGroundEffect", menuName = "GameData/Items/Effects/Attack/Reactive Ground")]
public class ReactiveGroundEffect : ItemEffectData
{
    [Tooltip("비워 두면 기존 장판 설정을 유지합니다. 설정하면 종류/스탯/생명주기 효과를 이 정의에서 가져옵니다.")]
    public AreaDefinition areaDefinition;
    [Tooltip("장판 종류는 유지하고 이 효과의 Ground Stat으로 범위/수명을 재정의합니다.")]
    public bool overrideDefinitionStat;
    public ReactiveGroundStat groundStat = new ReactiveGroundStat();
    [Tooltip("생성 시와 매 Tick마다 중심에서 실행합니다.")]
    public ItemEffectData[] defaultEffects;
    [Tooltip("아이템 착지 시 중심에서 실행합니다. Special Duration > 0이면 해당 시간 동안 기본 Tick도 이 효과로 대체합니다.")]
    public ItemEffectData[] specialEffects;
    [Tooltip("비워두면 모든 아이템에 반응합니다. 채우면 해당 아이템만 반응합니다.")]
    public ItemData[] acceptedItems;
    public bool onlySameOwner = true;
    [Tooltip("반응한 아이템 자체의 기본 효과를 실행하지 않습니다. 인벤토리 소비는 바꾸지 않습니다.")]
    public bool replaceIncomingItemEffects;
    [Tooltip("장판 수명 동안 표시할 프리팹. 단위 지름으로 제작하면 Radius에 맞춰 확대됩니다.")]
    public GameObject areaVisualPrefab;
    public bool scaleVisualByRadius = true;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, BaseStat);
        ItemEffectUtility.Prepare(context, defaultEffects);
        ItemEffectUtility.Prepare(context, specialEffects);
        if (areaDefinition != null)
        {
            ItemEffectUtility.Prepare(context, areaDefinition.onStart);
            ItemEffectUtility.Prepare(context, areaDefinition.onEnter);
            ItemEffectUtility.Prepare(context, areaDefinition.onTick);
            ItemEffectUtility.Prepare(context, areaDefinition.onExit);
            ItemEffectUtility.Prepare(context, areaDefinition.onNaturalEnd);
            ItemEffectUtility.Prepare(context, areaDefinition.onResidence);
        }
    }

    public ReactiveGroundStat BaseStat => areaDefinition != null && !overrideDefinitionStat ? areaDefinition.stat : groundStat;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || BaseStat == null) return;
        GameObject host = new GameObject("ReactiveGround");
        host.transform.position = context.targetPosition;
        ReactiveGroundArea area = host.AddComponent<ReactiveGroundArea>();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        area.Init(this, context);
    }

    protected override float GetImpactRadius(ItemEffectContext context)
    {
        ReactiveGroundStat stat = context.GetCurrentStat(this, BaseStat);
        return stat != null ? stat.groundRadius : 1f;
    }
}
