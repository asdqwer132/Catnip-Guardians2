using UnityEngine;

[CreateAssetMenu(fileName = "ReactiveGroundEffect", menuName = "GameData/Items/Effects/Attack/Reactive Ground")]
public sealed class ReactiveGroundEffect : ItemEffectData
{
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
        context.GetSnapshotStat(this, groundStat);
        ItemEffectUtility.Prepare(context, defaultEffects);
        ItemEffectUtility.Prepare(context, specialEffects);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || groundStat == null) return;
        GameObject host = new GameObject("ReactiveGround");
        host.transform.position = context.targetPosition;
        ReactiveGroundArea area = host.AddComponent<ReactiveGroundArea>();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        area.Init(this, context);
    }

    protected override float GetImpactRadius(ItemEffectContext context)
    {
        ReactiveGroundStat stat = context.GetCurrentStat(this, groundStat);
        return stat != null ? stat.groundRadius : 1f;
    }
}
