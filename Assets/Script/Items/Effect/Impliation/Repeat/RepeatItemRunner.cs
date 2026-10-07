using UnityEngine;

public sealed class RepeatItemRunner : AttackObject<RepeatItemStat>
{
    private ItemEffectContext context;
    private RepeatItemStat stat;
    private RepeatItemStep[] steps;
    private ItemThrowExecutor thrower;
    private RepeatItemSelection selection;
    private AttackPlacementMode placement;
    private RepeatItemStartPosition startPosition;
    private Vector3 origin, direction;
    private bool throwItems, triggerSpecial, initialized, finished, waitingForFirstUse;
    private int total, index;
    private float waitStartedAt, stepInterval;

    public void Init(RepeatItemEffect effect, ItemEffectContext execution)
    {
        Init(effect, execution, effect != null ? effect.CreateRuntimeSteps() : null);
    }

    internal void Init(RepeatItemEffect effect, ItemEffectContext execution, RepeatItemStep[] runtimeSteps)
    {
        if (effect == null || execution == null || effect.repeatStat == null ||
            runtimeSteps == null || runtimeSteps.Length == 0)
        {
            finished = true;
            Destroy(gameObject);
            return;
        }
        context = execution;
        steps = runtimeSteps;
        selection = effect.selection;
        placement = effect.placement;
        startPosition = effect.startPosition;
        throwItems = effect.throwItems;
        triggerSpecial = effect.triggerSpecialItemsForChildren;
        origin = context.targetPosition;
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetSnapshotStat(effect, effect.repeatStat),
            context.sourceItemData, context.sourceBag, context.buffManager, context.owner);
        total = EffectStatUtility.Count(stat.itemRepeatCount);
        direction = AttackPlacement.Direction(context, effect.directionMode, effect.fixedWorldDirection, stat.itemRepeatDirectionAngle);
        if (throwItems)
        {
            ItemEffectExecutor executor = gameObject.AddComponent<ItemEffectExecutor>();
            executor.buffManager = context.buffManager;
            thrower = gameObject.AddComponent<ItemThrowExecutor>();
            thrower.itemEffectExecutor = executor;
            thrower.throwMoverPrefab = effect.projectilePrefab;
            thrower.showTargetRange = effect.showTargetRange;
            thrower.targetRangeIndicatorPrefab = effect.targetRangeIndicatorPrefab;
        }
        initialized = true;
        waitingForFirstUse = true;
        waitStartedAt = Time.time;
        if (stat.itemRepeatInterval <= 0f)
            UseNext();
    }

    protected override void ApplyStat(RepeatItemStat current)
    {
        stat = current.Clone();
        stat.Clamp();
    }

    private void Update()
    {
        if (!initialized || finished) return;
        if (!context.CanContinue) { Clear(); return; }
        // 실행 중 바뀐 Dynamic 버프도 현재 대기 중인 간격에 반영한다.
        float interval = waitingForFirstUse ? stat.itemRepeatInterval :
            EffectStatUtility.Safe(stepInterval * stat.itemRepeatStepIntervalMultiplier, 0f, 60f, 0.2f);
        if (Time.time - waitStartedAt + 0.00001f >= interval)
            UseNext();
    }

    private void UseNext()
    {
        if (finished || !context.CanContinue) return;
        RepeatItemStep step = steps[selection == RepeatItemSelection.Random ? Random.Range(0, steps.Length) : index % steps.Length];
        // 반복 시작 위치에는 원래 아이템이 이미 도착했다. 첫 재투척부터 한 칸 앞으로 보낸다.
        int placementIndex = placement == AttackPlacementMode.Forward ? index + 1 : index;
        Vector3 target = AttackPlacement.Position(placement, origin, direction, placementIndex, total,
            stat.itemRepeatForwardOffset, stat.itemRepeatSideOffset, stat.itemRepeatRadius, stat.itemRepeatSpreadAngle);
        Vector3 start = startPosition == RepeatItemStartPosition.OwnerPosition && owner != null ? owner.transform.position : origin;
        context.targetPosition = target;
        // 한 묶음은 같은 프레임, 같은 목표 위치에서 전부 사용한다.
        for (int i = 0; i < step.items.Length; i++)
        {
            if (finished || !context.CanContinue) return;
            ItemData item = step.items[i];
            if (!ItemEffectExecutor.CanExecuteItemEffect(item)) continue;
            if (throwItems && (target - start).sqrMagnitude > 0.0001f)
                thrower.Throw(item, start, target, owner, sourceBag, 0, context, triggerSpecial);
            else
                ItemEffectExecutor.ExecuteItem(item, start, target, direction, owner, sourceBag, buffManager, context, triggerSpecial);
        }
        index++;
        waitingForFirstUse = false;
        stepInterval = step.intervalAfter;
        waitStartedAt = Time.time;
        if (index >= total)
        {
            finished = true;
            CompleteLifetime();
            Destroy(gameObject);
        }
    }

    protected override void OnDisable()
    {
        finished = true;
        base.OnDisable();
    }
}
