using UnityEngine;

public sealed class SequenceAttackRunner : AttackObject<SequenceAttackStat>
{
    private ItemEffectContext context;
    private SequenceAttackStat stat;
    private SequenceAttackMode mode;
    private AttackPlacementMode placement;
    private BombTriggerMode bombTrigger;
    private ItemEffectData[] startEffects, impactEffects, finalEffects;
    private ItemThrowMover prefab, bounceMover;
    private Sprite sprite;
    private LayerMask enemyMask;
    private Vector3 origin, forward;
    private int count, index, pendingBombs;
    private float remainingWait;
    private bool waitingForBounce, finished, initialized;

    public void Init(SequenceAttackEffect effect, ItemEffectContext execution)
    {
        context = execution;
        origin = context.targetPosition;
        mode = effect.mode;
        placement = effect.randomScatter ? AttackPlacementMode.CircleRandom : AttackPlacementMode.CircleEven;
        bombTrigger = effect.bombTrigger;
        startEffects = Copy(effect.onStartEffects);
        impactEffects = Copy(effect.onImpactEffects);
        finalEffects = Copy(effect.afterLastImpactEffects);
        prefab = effect.projectilePrefab;
        sprite = effect.projectileSprite != null ? effect.projectileSprite :
            (execution.sourceItemData != null ? execution.sourceItemData.icon : null);
        enemyMask = effect.enemyLayerMask;
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetSnapshotStat(effect, effect.sequenceStat),
            context.sourceItemData, context.sourceBag, context.buffManager, context.owner);
        count = EffectStatUtility.Count(stat.attackCount);
        forward = AttackPlacement.Direction(context, effect.directionMode, effect.fixedWorldDirection, stat.directionAngle);
        initialized = true;
        ExecuteEffects(startEffects, origin, forward);
        if (mode == SequenceAttackMode.ScatterBombs) LaunchBomb();
        else Impact(origin);
    }

    protected override void ApplyStat(SequenceAttackStat current)
    {
        stat = current.Clone();
        stat.Clamp();
    }

    private void Update()
    {
        if (!initialized || finished) return;
        if (!context.CanContinue) { Clear(); return; }
        if (waitingForBounce) return;
        if (index >= count)
        {
            if (pendingBombs == 0) Finish();
            return;
        }
        remainingWait -= Time.deltaTime;
        if (remainingWait > 0f) return;
        // 0초 간격도 프레임당 한 번 진행해 한 프레임의 생성 폭증을 막는다.
        if (mode == SequenceAttackMode.ScatterBombs) LaunchBomb();
        else if (mode == SequenceAttackMode.RepeatAtPoint) Impact(origin);
        else StartBounce();
    }

    private void Impact(Vector3 position)
    {
        context.targetPosition = position;
        transform.position = position;
        ExecuteEffects(impactEffects, position, forward);
        index++;
        remainingWait = stat.attackInterval;
        waitingForBounce = false;
        if (index >= count && pendingBombs == 0) Finish();
    }

    private void StartBounce()
    {
        if (bounceMover == null)
            bounceMover = CreateMover(prefab, context.targetPosition, sprite);
        Vector3 target = AttackPlacement.Position(AttackPlacementMode.Forward, origin, forward,
            index, count, stat.forwardOffset, stat.sideOffset, 0f, 0f);
        waitingForBounce = true;
        ConfigureMover(bounceMover, stat.arcHeight, stat.flightTime);
        bounceMover.InitMove(context.targetPosition, target, stat.flightTime, () =>
        {
            if (!finished && context.CanContinue) Impact(target);
        });
    }

    private void LaunchBomb()
    {
        Vector3 position = AttackPlacement.Position(placement, origin, forward, index, count,
            0f, 0f, stat.scatterRadius, stat.spreadAngle);
        Vector3 direction = position - origin;
        if (direction.sqrMagnitude < 0.000001f) direction = forward;
        ItemEffectContext bombContext = context.Copy(position, direction.normalized);
        bombContext.usePosition = origin;
        ItemThrowMover mover = CreateMover(prefab, origin, sprite);
        ConfigureMover(mover, stat.arcHeight, stat.flightTime);
        SequenceAttackProjectile bomb = mover.gameObject.AddComponent<SequenceAttackProjectile>();
        pendingBombs++;
        index++;
        remainingWait = stat.attackInterval;
        bomb.Init(mover, bombContext, impactEffects, bombTrigger, stat.Clone(), enemyMask, () =>
        {
            pendingBombs--;
            context.targetPosition = position;
            if (!finished && index >= count && pendingBombs == 0) Finish();
        });
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        if (context.CanContinue) ExecuteEffects(finalEffects, context.targetPosition, forward);
        if (bounceMover != null) Destroy(bounceMover.gameObject);
        bounceMover = null;
        CompleteLifetime();
        Destroy(gameObject);
    }

    protected override void OnDisable()
    {
        finished = true;
        if (bounceMover != null) Destroy(bounceMover.gameObject);
        bounceMover = null;
        base.OnDisable();
    }

    private void ExecuteEffects(ItemEffectData[] effects, Vector3 position, Vector3 direction)
    {
        ExecuteEffectsAt(effects, context.Copy(position, direction));
    }

    internal static void ExecuteEffectsAt(ItemEffectData[] effects, ItemEffectContext execution)
    {
        if (effects == null) return;
        for (int i = 0; i < effects.Length && execution.CanContinue; i++)
            if (effects[i] != null) effects[i].Execute(execution);
    }

    internal static ItemEffectData[] Copy(ItemEffectData[] effects)
        => effects != null ? (ItemEffectData[])effects.Clone() : null;

    internal static ItemThrowMover CreateMover(ItemThrowMover prefab, Vector3 position, Sprite sprite)
    {
        ItemThrowMover mover = prefab != null ? Instantiate(prefab, position, Quaternion.identity) :
            new GameObject("AttackProjectile").AddComponent<ItemThrowMover>();
        mover.destroyOnArrive = false;
        mover.Init(position, position, sprite, null);
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(mover);
        return mover;
    }

    internal static void ConfigureMover(ItemThrowMover mover, float height, float flight)
    {
        mover.autoArcHeightByDistance = false;
        mover.arcHeight = height;
        mover.arriveTime = flight;
        mover.maxMoveTime = Mathf.Max(flight, 0.01f);
    }
}
