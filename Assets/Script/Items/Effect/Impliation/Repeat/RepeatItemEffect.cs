using System.Collections.Generic;
using UnityEngine;

public enum RepeatItemSelection { InOrder, Random }
public enum RepeatItemStartPosition { EffectPosition, OwnerPosition }

[CreateAssetMenu(fileName = "RepeatItemEffect", menuName = "GameData/Items/Effects/Repeat Item")]
public class RepeatItemEffect : ItemEffectData
{
    [Header("Repeat Steps")]
    [Tooltip("묶음마다 동시에 사용할 아이템과 다음 묶음까지의 간격을 설정합니다.")]
    public RepeatItemStep[] steps;
    [Tooltip("InOrder: 묶음을 순서대로 선택 / Random: 사용할 묶음을 무작위 선택")]
    public RepeatItemSelection selection;
    public RepeatItemStat repeatStat = new RepeatItemStat();
    [Header("Placement")]
    public AttackPlacementMode placement;
    [Tooltip("ThrownDirection: 원본 아이템을 던진 방향 / FixedWorldDirection: 아래 고정 방향")]
    public AttackDirectionMode directionMode;
    [Tooltip("고정 기준 방향입니다. (1, 0)은 오른쪽, (0, 1)은 위쪽입니다.")]
    public Vector2 fixedWorldDirection = Vector2.right;
    [Tooltip("Shotgun에서 각도를 무작위로 뽑거나 균등하게 나눕니다. 거리 오프셋은 두 방식 모두 독립적으로 적용됩니다.")]
    public AttackSpreadDistribution shotgunDistribution;
    [Tooltip("CenteredOnDirection: 기준 방향의 양옆에 배치 / FromDirection: 기준 방향에서 시작. Shotgun·CircleEven·CircleRandom에 적용됩니다.")]
    public AttackSpreadStartMode spreadStartMode;
    [Tooltip("켜면 기준 방향에서 시계 방향으로 배치합니다. 끄면 반시계 방향입니다.")]
    public bool clockwiseSpread;
    public RepeatItemStartPosition startPosition;
    [Header("Throw")]
    [Tooltip("끄면 목표 위치에서 효과를 바로 실행합니다. 켜면 도착할 때 실행합니다.")]
    public bool throwItems = true;
    public ItemThrowMover projectilePrefab;
    [Tooltip("공통 발사체 이미지. 비워두면 투척 아이템/원본 아이템의 아이콘을 사용합니다.")]
    public Sprite projectileSprite;
    public bool showTargetRange;
    public TargetRangeIndicator targetRangeIndicatorPrefab;
    [Tooltip("추가 공격 상태를 하위 아이템마다 발동할지 설정합니다. 기본은 최초 아이템만 발동합니다.")]
    public bool triggerSpecialItemsForChildren;
    [Tooltip("켜면 Flight Time / Arc Height를 사용합니다. 끄면 기존 투척 프리팹의 비행 설정을 유지합니다.")]
    public bool overrideProjectileMotion;
    public LayerMask enemyLayerMask = ~0;
    [Tooltip("바운스의 첫 공격을 원래 착지점에서 즉시 실행합니다.")]
    public bool firstStepAtOrigin;
    [Header("Sequence Effects")]
    public ItemEffectData[] onStartEffects;
    public ItemEffectData[] afterLastImpactEffects;

    // 기존 에셋과 호출 코드를 위한 필드. 에디터에서는 steps로 자동 변환한다.
    [HideInInspector] public ItemData[] items;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, repeatStat);
        ItemEffectUtility.Prepare(context, onStartEffects);
        ItemEffectUtility.Prepare(context, afterLastImpactEffects);
        if (steps != null)
            foreach (RepeatItemStep step in steps)
                if (step != null) ItemEffectUtility.Prepare(context, step.onImpactEffects);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || repeatStat == null) return;
        RepeatItemStep[] runtimeSteps = CreateRuntimeSteps();
        if (runtimeSteps.Length == 0) return;
        GameObject host = new GameObject("RepeatItems");
        RepeatItemRunner runner = host.AddComponent<RepeatItemRunner>();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        runner.Init(this, context, runtimeSteps);
    }

    private void OnValidate()
    {
        ConvertLegacyItemsToSteps();
        if (steps != null)
            foreach (RepeatItemStep step in steps)
                if (step != null) step.ConvertLegacyItems();
    }

    [ContextMenu("Convert Legacy Items To Steps")]
    public void ConvertLegacyItemsToSteps()
    {
        if (steps != null && steps.Length > 0)
        {
            items = null;
            return;
        }
        if (items == null || items.Length == 0)
            return;

        float interval = GetLegacyInterval();
        steps = new RepeatItemStep[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            steps[i] = new RepeatItemStep
            {
                items = new[] { items[i] },
                intervalAfter = interval
            };
        }
        items = null;
    }

    public RepeatItemStep[] CreateRuntimeSteps()
    {
        List<RepeatItemStep> validSteps = new List<RepeatItemStep>();
        if (steps != null && steps.Length > 0)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                RepeatItemStep step = steps[i];
                if (step != null)
                    AddValidStep(validSteps, step);
            }
        }
        else if (items != null)
        {
            // 변환되지 않은 기존 에셋도 플레이 중에는 정상적으로 사용할 수 있다.
            float interval = GetLegacyInterval();
            for (int i = 0; i < items.Length; i++)
                AddValidStep(validSteps, new RepeatItemStep { items = new[] { items[i] }, intervalAfter = interval });
        }
        return validSteps.ToArray();
    }

    private float GetLegacyInterval()
    {
        return EffectStatUtility.Safe(repeatStat != null ? repeatStat.itemRepeatInterval : 0.2f,
            0f, 60f, 0.2f);
    }

    private static void AddValidStep(List<RepeatItemStep> output, RepeatItemStep source)
    {
        List<RepeatItemEntry> entries = new List<RepeatItemEntry>();
        if (source.entries != null && source.entries.Length > 0)
        {
            foreach (RepeatItemEntry entry in source.entries)
                if (entry != null && entry.item != null)
                    entries.Add(new RepeatItemEntry { item = entry.item, count = Mathf.Clamp(entry.count, 1, 128) });
        }
        else if (source.items != null)
        {
            foreach (ItemData item in source.items)
                if (item != null)
                    entries.Add(new RepeatItemEntry { item = item, count = 1 });
        }
        bool hasEffects = false;
        if (source.onImpactEffects != null)
            foreach (ItemEffectData effect in source.onImpactEffects) hasEffects |= effect != null;
        if (entries.Count == 0 && !hasEffects) return;
        output.Add(new RepeatItemStep
        {
            entries = entries.ToArray(),
            onImpactEffects = ItemEffectUtility.Copy(source.onImpactEffects),
            effectOnlyCount = Mathf.Clamp(source.effectOnlyCount, 1, 128),
            overridePlacement = source.overridePlacement,
            placement = source.placement,
            travelMode = source.travelMode,
            impactTrigger = source.impactTrigger,
            intervalAfter = EffectStatUtility.Safe(source.intervalAfter, 0f, 60f, 0.2f)
        });
    }
}
