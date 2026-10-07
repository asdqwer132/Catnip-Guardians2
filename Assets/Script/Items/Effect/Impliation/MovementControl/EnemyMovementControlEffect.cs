using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyMovementControlCenter
{
    TargetPosition = 0,
    UsePosition = 1,
    OwnerPosition = 2
}

public enum EnemyMovementControlExecutionMode
{
    Once = 0,
    PersistentArea = 1
}

[CreateAssetMenu(fileName = "EnemyMovementControlEffect", menuName = "GameData/Items/Effects/Movement/Control")]
public class EnemyMovementControlEffect : ItemEffectData
{
    [Header("Movement")]
    public ActorMovementControlMode mode = ActorMovementControlMode.PushAway;
    public EnemyMovementControlCenter centerSource = EnemyMovementControlCenter.TargetPosition;
    public EnemyMovementControlStat movementStat = new EnemyMovementControlStat();

    [Header("Execution")]
    public EnemyMovementControlExecutionMode executionMode = EnemyMovementControlExecutionMode.Once;
    [Tooltip("지속 당기기에서 중심에 도착해도 제어를 유지합니다. 기본 이동도 차단합니다.")]
    public bool holdAtCenter = true;
    [Tooltip("재탐색 시 영역 밖으로 나간 적에게 이 영역이 적용한 이동을 해제합니다.")]
    public bool releaseOnExit = true;
    [Tooltip("지속 영역의 연출 수명을 Area Duration에 맞춥니다. 반복 재생은 연출의 Animator에서 설정합니다.")]
    public bool matchAreaVisualLifetime = true;

    [Header("Area")]
    [Tooltip("비워 두면 기본 원형. 다른 모양 에셋을 연결해 탐색 방식만 바꿀 수 있습니다.")]
    public MovementControlAreaShape areaShape;
    public LayerMask enemyLayerMask = ~0;
    public bool includeTriggers = true;

    [Header("Movement Options")]
    public ActorMovementControlSpeedCurve speedCurve = ActorMovementControlSpeedCurve.EaseOut;
    public ActorMovementControlReapplyMode reapplyMode = ActorMovementControlReapplyMode.Replace;
    public bool suppressBaseMovement = true;
    public bool clearExternalVelocity = true;
    [Tooltip("이미 기절/정지한 적도 강제로 이동시킵니다. 기절/정지 상태 자체를 해제하지 않습니다.")]
    public bool allowWhileStopped;
    public bool interruptPattern = true;
    public bool interruptAttack = true;

    [Header("Distance Falloff")]
    public bool useDistanceFalloff;
    [Range(0f, 1f)] public float edgeStrengthMultiplier = 0.25f;

    private readonly Stack<QueryBuffer> queryPool = new Stack<QueryBuffer>();

    internal sealed class QueryBuffer
    {
        public Collider2D[] hits = new Collider2D[32];
        public readonly HashSet<Enemy> enemies = new HashSet<Enemy>();

        public void Clear()
        {
            Array.Clear(hits, 0, hits.Length);
            enemies.Clear();
        }
    }

    // 실행 시작 시 복사한다. 영역 유지 중 공유 에셋이나 버프를 다시 읽지 않는다.
    internal struct AreaSettings
    {
        public MovementControlAreaShape shape;
        public ContactFilter2D filter;
        public ActorMovementControlRequest request;
        public bool interruptPattern;
        public bool interruptAttack;
        public bool useDistanceFalloff;
        public float edgeStrengthMultiplier;
        public bool releaseOnExit;
    }

    protected override void ExecuteWithConditions(ItemEffectContext context)
    {
        if (AreConditionsSatisfied(context))
            ExecuteMovementControl(context, true);
    }

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, movementStat);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        ExecuteMovementControl(context, false);
    }

    private void ExecuteMovementControl(ItemEffectContext context, bool playVisual)
    {
        if (context == null)
            return;

        EnemyMovementControlStat stat = GetCurrentStat(context);
        if (stat == null || stat.range <= 0f || stat.strength <= 0f || stat.duration <= 0f)
            return;

        bool persistent = executionMode == EnemyMovementControlExecutionMode.PersistentArea;
        if ((!persistent && executionMode != EnemyMovementControlExecutionMode.Once) ||
            (persistent && stat.areaDuration <= 0f))
            return;

        MovementControlAreaContext area;
        if (!TryGetAreaContext(context, stat.range, out area))
            return;

        if (playVisual)
            PlayMovementImpact(context, area, persistent && matchAreaVisualLifetime ? stat.areaDuration : 0f);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayerMask);
        filter.useLayerMask = true;
        filter.useTriggers = includeTriggers;

        bool hold = persistent && mode == ActorMovementControlMode.PullTowards && holdAtCenter;
        AreaSettings settings = new AreaSettings
        {
            shape = areaShape,
            filter = filter,
            request = new ActorMovementControlRequest
            {
                mode = mode,
                speedCurve = speedCurve,
                reapplyMode = reapplyMode,
                center = area.center,
                fallbackDirection = area.forward,
                strength = stat.strength,
                duration = hold ? Mathf.Max(stat.duration, stat.tickInterval) : stat.duration,
                maxDistance = stat.maxDistance,
                pullStopDistance = stat.pullStopDistance,
                suppressBaseMovement = suppressBaseMovement || hold,
                clearExternalVelocity = clearExternalVelocity,
                allowWhileStopped = allowWhileStopped,
                holdAtCenter = hold
            },
            interruptPattern = interruptPattern,
            interruptAttack = interruptAttack,
            useDistanceFalloff = useDistanceFalloff,
            edgeStrengthMultiplier = edgeStrengthMultiplier,
            releaseOnExit = releaseOnExit
        };

        if (persistent)
        {
            EnemyMovementControlAreaRunner.StartArea(settings, area, stat.areaDuration, stat.tickInterval, context);
            return;
        }

        QueryBuffer buffer = queryPool.Count > 0 ? queryPool.Pop() : new QueryBuffer();
        try
        {
            ApplyMovementControl(settings, area, buffer, null, null);
            ItemEffectDelayRunner.Wait(context, stat.duration);
        }
        finally
        {
            buffer.Clear();
            queryPool.Push(buffer);
        }
    }

    internal static void ApplyMovementControl(
        AreaSettings settings,
        MovementControlAreaContext area,
        QueryBuffer buffer,
        object source,
        Dictionary<Enemy, int> affected)
    {
        EnemyMovementControlAreaRunner.AreaState persistentSource =
            source as EnemyMovementControlAreaRunner.AreaState;
        buffer.enemies.Clear();
        int count = QueryAllColliders(settings.shape, area, settings.filter, buffer);
        for (int i = 0; i < count; i++)
        {
            if (persistentSource != null && !persistentSource.IsActive)
                break;
            Collider2D hit = buffer.hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !buffer.enemies.Add(enemy))
                continue;

            float strengthMultiplier = 1f;
            if (settings.useDistanceFalloff)
            {
                float distance = settings.shape != null
                    ? settings.shape.GetNormalizedDistance(area, enemy.transform.position)
                    : Mathf.Clamp01(Vector2.Distance(area.center, enemy.transform.position) / area.range);
                strengthMultiplier = Mathf.Lerp(1f, Mathf.Clamp01(settings.edgeStrengthMultiplier), Mathf.Clamp01(distance));
            }

            ActorMovementControlRequest request = settings.request;
            request.strength *= strengthMultiplier;
            request.source = source;
            int lifeId = enemy.HitEffectLifeId;
            if (enemy.TryApplyMovementControl(request, settings.interruptPattern, settings.interruptAttack) &&
                affected != null)
            {
                if (persistentSource != null && !persistentSource.IsActive)
                {
                    if (enemy.mover != null && enemy.HitEffectLifeId == lifeId)
                        enemy.mover.CancelMovementControl(source);
                    break;
                }
                affected[enemy] = lifeId;
            }
        }
    }

    private static int QueryAllColliders(
        MovementControlAreaShape shape, MovementControlAreaContext area, ContactFilter2D filter, QueryBuffer buffer)
    {
        while (true)
        {
            int count = shape != null
                ? shape.Overlap(area, filter, buffer.hits)
                : Physics2D.OverlapCircle(area.center, area.range, filter, buffer.hits);
            if (count < 0 || count > buffer.hits.Length)
                throw new InvalidOperationException("MovementControlAreaShape.Overlap must return the number of filled results.");
            if (count < buffer.hits.Length)
                return count;

            // 고정 배열이 가득 차도 뒤쪽 적을 누락하지 않는다. 확장된 배열은 재사용한다.
            Array.Resize(ref buffer.hits, checked(buffer.hits.Length * 2));
        }
    }

    private EnemyMovementControlStat GetCurrentStat(ItemEffectContext context)
    {
        return context != null ? context.GetCurrentStat(this, movementStat) : movementStat;
    }

    private bool TryGetAreaContext(ItemEffectContext context, float range, out MovementControlAreaContext area)
    {
        area = new MovementControlAreaContext();
        Vector2 center;
        switch (centerSource)
        {
            case EnemyMovementControlCenter.TargetPosition:
                center = context.targetPosition;
                break;
            case EnemyMovementControlCenter.UsePosition:
                center = context.usePosition;
                break;
            case EnemyMovementControlCenter.OwnerPosition:
                if (context.owner == null)
                    return false;
                center = context.owner.transform.position;
                break;
            default:
                return false;
        }

        if (!Finite(center.x) || !Finite(center.y))
            return false;

        Vector3 direction;
        Vector2 forward = context.TryGetDirection(out direction) ? (Vector2)direction : Vector2.right;
        if (!Finite(forward.x) || !Finite(forward.y) || forward.sqrMagnitude < 0.000001f)
            forward = Vector2.right;
        area = new MovementControlAreaContext { center = center, forward = forward.normalized, range = range };
        return true;
    }

    private void PlayMovementImpact(ItemEffectContext context, MovementControlAreaContext area, float areaLifetime)
    {
        if (impactVfxPrefab == null)
            return;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(audioSource);

        Vector3 scale = impactBaseScale;
        if (scaleImpactVfxByRadius)
        {
            Vector2 size = areaShape != null ? areaShape.GetVisualSize(area) : Vector2.one * (area.range * 2f);
            scale = Vector3.Scale(scale, new Vector3(size.x, size.y, 1f));
        }
        scale = Vector3.Scale(scale, GetAdditionalImpactScale(context));

        Vector3 position = new Vector3(area.center.x, area.center.y, 0f);
        ItemEffectContext visualContext = new ItemEffectContext(context.owner, context.sourceItemData,
            context.usePosition, position, context.sourceBag, this, context.buffManager, context.direction, context.plan);
        Quaternion rotation = areaShape != null ? areaShape.GetVisualRotation(area) : Quaternion.identity;
        ImpactVfxInstance impact = Instantiate(impactVfxPrefab, position, rotation);
        impact.Init(
            effectData: this,
            context: visualContext,
            baseScale: scale,
            useRadiusScale: false,
            lifeTime: areaLifetime > 0f ? areaLifetime : impactVfxLifeTime,
            useAnimatorClipLifeTime: areaLifetime <= 0f && useAnimatorClipLifeTime
        );
    }

    protected override float GetImpactRadius(ItemEffectContext context)
    {
        EnemyMovementControlStat stat = GetCurrentStat(context);
        return stat != null ? stat.range : 0f;
    }

    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
