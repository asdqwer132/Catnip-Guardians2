using System.Collections.Generic;
using UnityEngine;

public sealed class RepeatItemRunner : AttackObject<RepeatItemStat>
{
    private ItemEffectContext context;
    private RepeatItemStat stat;
    private RepeatItemStep[] steps;
    private RepeatItemSelection selection;
    private AttackPlacementMode placement;
    private AttackSpreadDistribution shotgunDistribution;
    private AttackSpreadStartMode spreadStartMode;
    private bool clockwiseSpread;
    private RepeatItemStartPosition startPosition;
    private ItemThrowMover prefab;
    private Sprite projectileSprite;
    private TargetRangeIndicator rangePrefab;
    private ItemEffectData[] finalEffects;
    private LayerMask enemyMask;
    private Vector3 origin, direction, lastImpact;
    private bool throwItems, triggerSpecial, overrideMotion, firstAtOrigin, showRange;
    private bool initialized, finished, waitingForFirstUse, waitForImpact, launching;
    private int total, index, pending, serialPosition;
    private float waitStartedAt, stepInterval;
    private readonly List<RepeatItemProjectile> projectiles = new List<RepeatItemProjectile>();

    public void Init(RepeatItemEffect effect, ItemEffectContext execution)
        => Init(effect, execution, effect != null ? effect.CreateRuntimeSteps() : null);

    internal void Init(RepeatItemEffect effect, ItemEffectContext execution, RepeatItemStep[] runtimeSteps)
    {
        if (effect == null || execution == null || effect.repeatStat == null ||
            runtimeSteps == null || runtimeSteps.Length == 0)
        { Destroy(gameObject); return; }
        context = execution;
        steps = runtimeSteps;
        selection = effect.selection;
        placement = effect.placement;
        shotgunDistribution = effect.shotgunDistribution;
        spreadStartMode = effect.spreadStartMode;
        clockwiseSpread = effect.clockwiseSpread;
        startPosition = effect.startPosition;
        throwItems = effect.throwItems;
        triggerSpecial = effect.triggerSpecialItemsForChildren;
        overrideMotion = effect.overrideProjectileMotion;
        firstAtOrigin = effect.firstStepAtOrigin;
        prefab = effect.projectilePrefab;
        projectileSprite = effect.projectileSprite;
        showRange = effect.showTargetRange;
        rangePrefab = effect.targetRangeIndicatorPrefab;
        enemyMask = effect.enemyLayerMask;
        finalEffects = ItemEffectUtility.Copy(effect.afterLastImpactEffects);
        origin = lastImpact = context.targetPosition;
        transform.position = origin;
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetUnscaledSnapshotStat(effect, effect.repeatStat),
            context.sourceItemData, context.sourceBag, context.buffManager, context.owner);
        total = EffectStatUtility.Count(stat.itemRepeatCount);
        direction = AttackPlacement.Direction(context, effect.directionMode, effect.fixedWorldDirection, stat.itemRepeatDirectionAngle);
        initialized = waitingForFirstUse = true;
        waitStartedAt = Time.time;
        ItemEffectUtility.Execute(effect.onStartEffects, context.Copy(origin, direction));
        if (stat.itemRepeatInterval <= 0f) UseNext();
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
        if (index >= total) { if (pending == 0) Finish(); return; }
        if (waitForImpact && pending > 0) return;
        float interval = waitingForFirstUse ? stat.itemRepeatInterval :
            EffectStatUtility.Safe(stepInterval * stat.itemRepeatStepIntervalMultiplier, 0f, 60f, 0.2f);
        if (Time.time - waitStartedAt + 0.00001f >= interval) UseNext();
    }

    private void UseNext()
    {
        if (finished || !context.CanContinue) return;
        RepeatItemStep step = steps[selection == RepeatItemSelection.Random ? Random.Range(0, steps.Length) : index % steps.Length];
        List<ItemData> payloads = new List<ItemData>();
        foreach (RepeatItemEntry entry in step.entries)
        {
            int copies = Copies(entry.count);
            for (int i = 0; i < copies && payloads.Count < 128; i++) payloads.Add(entry.item);
        }
        if (step.entries.Length == 0)
            for (int i = 0; i < Copies(step.effectOnlyCount); i++) payloads.Add(null);
        AttackPlacementMode mode = step.overridePlacement ? step.placement : placement;
        bool bounce = step.travelMode == RepeatItemTravelMode.Bounce;
        bool fly = step.travelMode == RepeatItemTravelMode.Throw || bounce ||
            (step.travelMode == RepeatItemTravelMode.Inherit && throwItems);
        Vector3 start = bounce ? lastImpact :
            startPosition == RepeatItemStartPosition.OwnerPosition && owner != null ? owner.transform.position : origin;
        int useIndex = index++;
        waitingForFirstUse = false;
        waitForImpact = bounce;
        stepInterval = step.intervalAfter;
        waitStartedAt = Time.time;
        launching = true;
        for (int shot = 0; shot < payloads.Count; shot++)
        {
            if (finished || !context.CanContinue) return;
            // 한 발은 전체 반복에 분산하고, 다중 발사는 스텝 안에서 각각 배치한다.
            int positionIndex = mode == AttackPlacementMode.Forward ? serialPosition + (firstAtOrigin ? 0 : 1) :
                payloads.Count > 1 ? shot : useIndex;
            int positionCount = payloads.Count > 1 ? payloads.Count : total;
            Vector3 target = firstAtOrigin && useIndex == 0 ? origin :
                AttackPlacement.Position(mode, origin, direction, positionIndex, positionCount,
                    stat.itemRepeatForwardOffset, stat.itemRepeatSideOffset, stat.itemRepeatRadius, stat.itemRepeatSpreadAngle,
                    stat.itemRepeatShotgunRadiusOffset, shotgunDistribution, spreadStartMode, clockwiseSpread);
            serialPosition++;
            ItemData item = payloads[shot];
            Vector3 shotDirection = (target - start).sqrMagnitude > 0.000001f ? (target - start).normalized : direction;
            ItemEffectContext shotContext = context.Copy(target, shotDirection);
            shotContext.usePosition = start;
            context.targetPosition = target;
            bool firstBounce = bounce && firstAtOrigin && useIndex == 0;
            if ((!fly || (target - start).sqrMagnitude <= 0.0001f || firstBounce) && step.impactTrigger == ItemImpactTrigger.OnArrival)
            {
                Impact(item, step.onImpactEffects, shotContext, fly && !firstBounce);
                continue;
            }
            ItemThrowMover mover = prefab != null ? Instantiate(prefab, start, Quaternion.identity) :
                new GameObject("RepeatItemProjectile").AddComponent<ItemThrowMover>();
            mover.destroyOnArrive = false;
            Sprite sprite = projectileSprite != null ? projectileSprite :
                item != null ? item.icon : sourceItemData != null ? sourceItemData.icon : null;
            mover.SetSprite(sprite);
            if (overrideMotion || bounce)
            {
                mover.autoArcHeightByDistance = false;
                mover.arcHeight = stat.itemRepeatArcHeight;
                mover.maxMoveTime = stat.itemRepeatFlightTime;
            }
            float flight = fly && !firstBounce ?
                (overrideMotion || bounce ? stat.itemRepeatFlightTime : mover.ResolveArrivalTime(item != null ? item.weight : 0f)) : 0f;
            TargetRangeIndicator indicator = null;
            if (showRange && rangePrefab != null)
                indicator = Instantiate(rangePrefab, target, Quaternion.identity);
            RepeatItemProjectile projectile = mover.gameObject.AddComponent<RepeatItemProjectile>();
            projectiles.Add(projectile);
            pending++;
            if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(mover);
            projectile.Init(mover, shotContext, step.impactTrigger, stat.Clone(), enemyMask, flight, indicator,
                () => Impact(item, step.onImpactEffects, shotContext, fly && !firstBounce),
                succeeded => Resolved(projectile, succeeded));
        }
        launching = false;
        if (index >= total && pending == 0) Finish();
    }

    private int Copies(int count) => Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(count, 1, 128) * stat.itemRepeatProjectileCountMultiplier), 0, 128);

    private void Impact(ItemData item, ItemEffectData[] effects, ItemEffectContext execution, bool thrown)
    {
        if (finished || !execution.CanContinue) return;
        lastImpact = execution.targetPosition;
        context.targetPosition = lastImpact;
        if (item != null)
            ItemEffectExecutor.ExecuteItem(item, execution.usePosition, lastImpact, execution.direction,
                owner, sourceBag, buffManager, execution, triggerSpecial, thrown);
        ItemEffectUtility.Execute(effects, execution);
    }

    private void Resolved(RepeatItemProjectile projectile, bool succeeded)
    {
        projectiles.Remove(projectile);
        pending = Mathf.Max(0, pending - 1);
        if (finished) return;
        if (!succeeded) { Clear(); return; }
        if (waitForImpact && pending == 0) waitStartedAt = Time.time;
        if (!launching && index >= total && pending == 0) Finish();
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        ItemEffectUtility.Execute(finalEffects, context.Copy(lastImpact, direction));
        CompleteLifetime();
        Destroy(gameObject);
    }

    protected override void OnDisable()
    {
        finished = true;
        foreach (RepeatItemProjectile projectile in projectiles.ToArray())
            if (projectile != null) Destroy(projectile.gameObject);
        projectiles.Clear();
        base.OnDisable();
    }
}
