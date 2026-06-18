using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatternRunner : MonoBehaviour
{
    [Header("Data")]
    public EnemyPatternData patternData;

    [Header("Components")]
    public Enemy enemy;
    public ActorTarget actorTarget;
    public ActorMover mover;
    public ActorAttack attack;
    public ActorVisual visual;

    [Header("Debug")]
    [SerializeField] private string currentPatternName = "None";
    [SerializeField] private bool isExecuting;
    [SerializeField] private bool isBlockingDefaultAI;

    private readonly List<EnemyPatternRuntime> runtimes = new List<EnemyPatternRuntime>();
    private Coroutine patternCoroutine;
    private float random1Timer;
    private float random2Timer;
    private bool initialized;

    private float moveSpeedMultiplier = 1f;
    private float attackDamageMultiplier = 1f;
    private float attackCooldownMultiplier = 1f;
    private float attackRangeMultiplier = 1f;
    private float incomingDamageMultiplier = 1f;

    private EnemyPatternRuntime queuedReactivePattern;

    public bool IsExecuting => isExecuting;
    public bool IsBlockingDefaultAI => isExecuting && isBlockingDefaultAI;

    private void Awake()
    {
        AutoBind();
        BuildRuntimeList();
    }

    private void AutoBind()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();
        if (actorTarget == null)
            actorTarget = GetComponent<ActorTarget>();
        if (mover == null)
            mover = GetComponent<ActorMover>();
        if (attack == null)
            attack = GetComponent<ActorAttack>();
        if (visual == null)
            visual = GetComponent<ActorVisual>();

        if (enemy != null)
        {
            if (actorTarget == null)
                actorTarget = enemy.actorTarget;
            if (mover == null)
                mover = enemy.mover;
            if (attack == null)
                attack = enemy.attack;
            if (visual == null)
                visual = enemy.visual;
        }
    }

    public void Init(Enemy owner)
    {
        enemy = owner;
        AutoBind();
        ResetRunner();
        initialized = true;
    }

    public void ResetRunner()
    {
        StopPattern();
        BuildRuntimeList();

        random1Timer = 0f;
        random2Timer = 0f;
        queuedReactivePattern = null;
        ResetRuntimeModifiers();
        initialized = false;
    }

    private void BuildRuntimeList()
    {
        runtimes.Clear();

        if (patternData == null || patternData.patterns == null)
            return;

        for (int i = 0; i < patternData.patterns.Count; i++)
        {
            EnemyPatternInfo info = patternData.patterns[i];
            if (info == null)
                continue;

            runtimes.Add(new EnemyPatternRuntime(info));
        }
    }

    public bool TickPattern()
    {
        if (!initialized)
            return false;

        if (patternData == null)
            return false;

        if (enemy == null || enemy.IsDead)
            return false;

        TickTimers();

        if (isExecuting)
            return IsBlockingDefaultAI;

        EnemyPatternRuntime nextPattern = ConsumeQueuedReactivePattern();

        if (nextPattern == null)
            nextPattern = PickAutoPattern();

        if (nextPattern == null)
            return false;

        StartPattern(nextPattern);
        return IsBlockingDefaultAI;
    }

    private void TickTimers()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < runtimes.Count; i++)
            runtimes[i].Tick(deltaTime);

        if (random1Timer > 0f)
            random1Timer -= deltaTime;

        if (random2Timer > 0f)
            random2Timer -= deltaTime;
    }

    private EnemyPatternRuntime ConsumeQueuedReactivePattern()
    {
        if (queuedReactivePattern == null)
            return null;

        EnemyPatternRuntime nextPattern = queuedReactivePattern;
        queuedReactivePattern = null;

        if (!CanRunPattern(nextPattern, true))
            return null;

        return nextPattern;
    }

    private EnemyPatternRuntime PickAutoPattern()
    {
        EnemyPatternRuntime random2Pattern = null;

        if (random2Timer <= 0f && CanUseRandom2Group())
            random2Pattern = PickWeightedPattern(EnemyPatternPickGroup.Random2);

        if (random2Pattern != null)
        {
            random2Timer = Mathf.Max(0.05f, patternData.random2Interval);
            return random2Pattern;
        }

        EnemyPatternRuntime random1Pattern = null;

        if (random1Timer <= 0f)
            random1Pattern = PickWeightedPattern(EnemyPatternPickGroup.Random1);

        if (random1Pattern != null)
        {
            random1Timer = Mathf.Max(0.05f, patternData.random1Interval);
            return random1Pattern;
        }

        return PickWeightedPattern(EnemyPatternPickGroup.None);
    }

    private bool CanUseRandom2Group()
    {
        if (patternData == null)
            return false;

        if (!patternData.useRandom2OnlyBelowHp)
            return true;

        return GetHpRatio() <= patternData.random2HpRatio;
    }

    private EnemyPatternRuntime PickWeightedPattern(EnemyPatternPickGroup group)
    {
        float totalWeight = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (!CanRunPattern(runtime, false))
                continue;

            if (runtime.info.pickGroup != group)
                continue;

            totalWeight += Mathf.Max(0f, runtime.info.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float pick = Random.Range(0f, totalWeight);
        float sum = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (!CanRunPattern(runtime, false))
                continue;

            if (runtime.info.pickGroup != group)
                continue;

            sum += Mathf.Max(0f, runtime.info.weight);
            if (pick <= sum)
                return Random.value <= runtime.info.chance ? runtime : null;
        }

        return null;
    }

    private bool CanRunPattern(EnemyPatternRuntime runtime, bool forceReactive)
    {
        if (runtime == null || runtime.info == null)
            return false;

        EnemyPatternInfo info = runtime.info;

        if (!info.enabled)
            return false;

        if (runtime.consumed)
            return false;

        if (runtime.cooldownTimer > 0f)
            return false;

        if (!forceReactive && (info.conditionType == EnemyPatternConditionType.AfterDamaged || info.conditionType == EnemyPatternConditionType.OnLethalDamage))
            return false;

        return CheckCondition(info);
    }

    private bool CheckCondition(EnemyPatternInfo info)
    {
        switch (info.conditionType)
        {
            case EnemyPatternConditionType.Always:
                return true;
            case EnemyPatternConditionType.HpRatioBelow:
                return GetHpRatio() <= info.hpRatio;
            case EnemyPatternConditionType.HpRatioAbove:
                return GetHpRatio() >= info.hpRatio;
            case EnemyPatternConditionType.DistanceToTargetLess:
                return GetDistanceToTarget() <= info.distance;
            case EnemyPatternConditionType.DistanceToTargetGreater:
                return GetDistanceToTarget() >= info.distance;
            case EnemyPatternConditionType.AfterDamaged:
                return true;
            case EnemyPatternConditionType.OnLethalDamage:
                return true;
            default:
                return false;
        }
    }

    private void StartPattern(EnemyPatternRuntime runtime)
    {
        if (runtime == null || runtime.info == null)
            return;

        if (patternCoroutine != null)
            StopCoroutine(patternCoroutine);

        patternCoroutine = StartCoroutine(RunPatternRoutine(runtime));
    }

    public void StopPattern()
    {
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
        }

        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
        ResetRuntimeModifiers();
    }

    private IEnumerator RunPatternRoutine(EnemyPatternRuntime runtime)
    {
        EnemyPatternInfo info = runtime.info;
        isExecuting = true;
        isBlockingDefaultAI = info.blockDefaultAI;
        currentPatternName = string.IsNullOrEmpty(info.patternName) ? info.actionType.ToString() : info.patternName;

        runtime.StartCooldown();

        if (patternData != null && patternData.showLog)
            Debug.Log($"[EnemyPatternRunner] {name} start pattern: {currentPatternName}");

        if (attack != null && attack.IsAttacking)
            attack.CancelAttack();

        yield return RunActionRoutine(info);

        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
        patternCoroutine = null;
    }

    private IEnumerator RunActionRoutine(EnemyPatternInfo info)
    {
        switch (info.actionType)
        {
            case EnemyPatternActionType.StatModifier:
                yield return StatModifierRoutine(info);
                break;
            case EnemyPatternActionType.DamageReductionStance:
                yield return DamageReductionStanceRoutine(info);
                break;
            case EnemyPatternActionType.ChargeToTarget:
                yield return ChargeToTargetRoutine(info);
                break;
            case EnemyPatternActionType.JumpToTarget:
                yield return JumpToTargetRoutine(info);
                break;
            case EnemyPatternActionType.RetreatThenJump:
                yield return RetreatThenJumpRoutine(info);
                break;
            case EnemyPatternActionType.CircleThenCharge:
                yield return CircleThenChargeRoutine(info);
                break;
            case EnemyPatternActionType.RangedAttack:
                yield return RangedAttackRoutine(info);
                break;
            case EnemyPatternActionType.AreaAttack:
                yield return AreaAttackRoutine(info);
                break;
            case EnemyPatternActionType.MultiAreaAttack:
                yield return MultiAreaAttackRoutine(info);
                break;
            case EnemyPatternActionType.ZigzagMoveToTarget:
                yield return ZigzagMoveRoutine(info);
                break;
            case EnemyPatternActionType.TeleportBehindTarget:
                yield return TeleportBehindTargetRoutine(info);
                break;
            case EnemyPatternActionType.SupportNearbyEnemies:
                yield return SupportNearbyEnemiesRoutine(info);
                break;
            case EnemyPatternActionType.SpawnPrefab:
                yield return SpawnPrefabRoutine(info);
                break;
            default:
                yield return TelegraphRoutine(info);
                break;
        }
    }

    private IEnumerator TelegraphRoutine(EnemyPatternInfo info)
    {
        if (mover != null)
            mover.Stop();

        if (visual != null)
            visual.StopMove();

        SpawnVisual(info.telegraphPrefab, transform.position, info.telegraphTime + 0.2f);

        if (info.telegraphTime > 0f)
            yield return new WaitForSeconds(info.telegraphTime);
    }

    private IEnumerator StatModifierRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        ApplyRuntimeModifier(info);
        SpawnVisual(info.effectPrefab, transform.position, info.duration);

        if (info.duration > 0f)
            yield return new WaitForSeconds(info.duration);

        ResetRuntimeModifiers();
    }

    private IEnumerator DamageReductionStanceRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        ApplyRuntimeModifier(info);
        SpawnVisual(info.effectPrefab, transform.position, info.duration);

        if (info.duration > 0f)
            yield return new WaitForSeconds(info.duration);

        ResetRuntimeModifiers();
    }

    private IEnumerator ChargeToTargetRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        Vector2 direction = GetDirectionToTarget();
        if (direction.sqrMagnitude <= 0.0001f)
            yield break;

        SpawnVisual(info.effectPrefab, transform.position, info.duration);

        float timer = 0f;
        bool damaged = false;
        float duration = Mathf.Max(0.01f, info.duration);

        while (timer < duration)
        {
            transform.position += (Vector3)(direction.normalized * info.speed * Time.deltaTime);

            if (!damaged && IsTargetInRadius(info.radius))
            {
                DamageTarget(info);
                damaged = true;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (mover != null)
            mover.Stop();
    }

    private IEnumerator JumpToTargetRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);
        yield return MoveToPointAndAreaDamageRoutine(info, GetTargetPosition());
    }

    private IEnumerator RetreatThenJumpRoutine(EnemyPatternInfo info)
    {
        Vector2 away = -GetDirectionToTarget();
        float retreatTime = Mathf.Max(0.01f, info.duration * 0.35f);
        float timer = 0f;

        while (timer < retreatTime)
        {
            transform.position += (Vector3)(away.normalized * info.speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return TelegraphRoutine(info);
        yield return MoveToPointAndAreaDamageRoutine(info, GetTargetPosition());
    }

    private IEnumerator MoveToPointAndAreaDamageRoutine(EnemyPatternInfo info, Vector3 endPosition)
    {
        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0.01f, info.duration);
        float timer = 0f;

        SpawnVisual(info.effectPrefab, endPosition, duration + 0.5f);

        while (timer < duration)
        {
            float t = timer / duration;
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        AreaDamage(info, endPosition);
    }

    private IEnumerator CircleThenChargeRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            yield break;

        Vector3 center = targetTransform.position;
        Vector3 offset = transform.position - center;
        if (offset.sqrMagnitude <= 0.0001f)
            offset = Vector3.right * Mathf.Max(0.1f, info.range);

        float circleTime = Mathf.Max(0.01f, info.duration * 0.5f);
        float timer = 0f;

        while (timer < circleTime && targetTransform != null)
        {
            center = targetTransform.position;
            float angle = info.circleAngle * (timer / circleTime);
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            transform.position = center + rotation * offset;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return ChargeToTargetRoutine(info);
    }

    private IEnumerator RangedAttackRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        int count = Mathf.Max(1, info.repeatCount);
        for (int i = 0; i < count; i++)
        {
            ShootProjectileOrDirectDamage(info, i, count);

            if (i < count - 1 && info.interval > 0f)
                yield return new WaitForSeconds(info.interval);
        }
    }

    private IEnumerator AreaAttackRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        int count = Mathf.Max(1, info.repeatCount);
        for (int i = 0; i < count; i++)
        {
            AreaDamage(info, transform.position);

            if (i < count - 1 && info.interval > 0f)
                yield return new WaitForSeconds(info.interval);
        }
    }

    private IEnumerator MultiAreaAttackRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        int count = Mathf.Max(1, info.repeatCount);
        Vector3 center = GetTargetPosition();

        for (int i = 0; i < count; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * Mathf.Max(0f, info.range);
            Vector3 areaPosition = center + (Vector3)randomOffset;
            SpawnVisual(info.effectPrefab, areaPosition, info.interval + 0.5f);
            AreaDamage(info, areaPosition);

            if (i < count - 1 && info.interval > 0f)
                yield return new WaitForSeconds(info.interval);
        }
    }

    private IEnumerator ZigzagMoveRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        float duration = Mathf.Max(0.01f, info.duration);
        float timer = 0f;

        while (timer < duration)
        {
            Vector2 forward = GetDirectionToTarget();
            if (forward.sqrMagnitude <= 0.0001f)
                break;

            Vector2 side = new Vector2(-forward.y, forward.x);
            float wave = Mathf.Sin(timer * info.zigzagFrequency) * info.zigzagAmplitude;
            Vector2 direction = (forward + side * wave).normalized;

            if (mover != null)
                mover.MoveDirection(direction);
            else
                transform.position += (Vector3)(direction * info.speed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TeleportBehindTargetRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            yield break;

        Vector2 fromTargetToEnemy = (transform.position - targetTransform.position).normalized;
        if (fromTargetToEnemy.sqrMagnitude <= 0.0001f)
            fromTargetToEnemy = Vector2.right;

        Vector3 targetPosition = targetTransform.position - (Vector3)(fromTargetToEnemy * Mathf.Max(0.1f, info.teleportDistanceFromTarget));
        transform.position = targetPosition;
        SpawnVisual(info.effectPrefab, transform.position, 1f);
    }

    private IEnumerator SupportNearbyEnemiesRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.01f, info.radius), info.targetLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy otherEnemy = hits[i].GetComponentInParent<Enemy>();
            if (otherEnemy == null || otherEnemy.IsDead)
                continue;

            if (info.healAmount > 0f && otherEnemy.health != null)
                otherEnemy.Heal(info.healAmount);

            EnemyPatternRunner otherRunner = otherEnemy.GetComponent<EnemyPatternRunner>();
            if (otherRunner != null)
                otherRunner.ApplyExternalModifier(info);
        }

        SpawnVisual(info.effectPrefab, transform.position, 1f);
    }

    private IEnumerator SpawnPrefabRoutine(EnemyPatternInfo info)
    {
        yield return TelegraphRoutine(info);

        if (info.spawnPrefab == null)
            yield break;

        int count = Mathf.Max(1, info.spawnCount);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * info.spawnSpreadRadius;
            Instantiate(info.spawnPrefab, transform.position + (Vector3)offset, Quaternion.identity);
        }
    }

    private void ShootProjectileOrDirectDamage(EnemyPatternInfo info, int index, int totalCount)
    {
        Vector2 direction = GetDirectionToTarget();
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        if (totalCount > 1 && info.circleAngle > 0f)
        {
            float half = info.circleAngle * 0.5f;
            float step = totalCount == 1 ? 0f : info.circleAngle / (totalCount - 1);
            float angle = -half + step * index;
            direction = Quaternion.Euler(0f, 0f, angle) * direction;
        }

        float damageValue = GetPatternDamage(info);

        if (info.projectilePrefab != null)
        {
            GameObject projectileObject = Instantiate(info.projectilePrefab, transform.position, Quaternion.identity);
            EnemySimpleProjectile projectile = projectileObject.GetComponent<EnemySimpleProjectile>();
            if (projectile == null)
                projectile = projectileObject.AddComponent<EnemySimpleProjectile>();

            projectile.Init(gameObject, direction, damageValue, Mathf.Max(0.1f, info.speed), Mathf.Max(0.1f, info.duration), info.targetLayerMask);
            return;
        }

        if (GetDistanceToTarget() <= Mathf.Max(info.range, 0.01f))
            DamageTarget(info);
    }

    private void AreaDamage(EnemyPatternInfo info, Vector3 center)
    {
        SpawnVisual(info.effectPrefab, center, 1f);

        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            return;

        float radius = Mathf.Max(0.01f, info.radius);
        float distance = Vector2.Distance(center, targetTransform.position);
        if (distance <= radius)
            DamageTarget(info);
    }

    private void DamageTarget(EnemyPatternInfo info)
    {
        if (actorTarget == null || !actorTarget.HasTarget)
            return;

        actorTarget.DamageTarget(GetPatternDamage(info));
    }

    private float GetPatternDamage(EnemyPatternInfo info)
    {
        float baseDamage = info.damage;

        if (baseDamage <= 0f && attack != null)
            baseDamage = attack.damage;

        return baseDamage * Mathf.Max(0f, info.damageMultiplier) + info.additionalDamage;
    }

    private bool IsTargetInRadius(float radius)
    {
        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            return false;

        return Vector2.Distance(transform.position, targetTransform.position) <= Mathf.Max(0.01f, radius);
    }

    private Transform GetTargetTransform()
    {
        if (actorTarget == null || !actorTarget.HasTarget)
            return null;

        return actorTarget.TargetTransform;
    }

    private Vector3 GetTargetPosition()
    {
        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            return transform.position;

        return targetTransform.position;
    }

    private float GetDistanceToTarget()
    {
        if (actorTarget == null)
            return float.MaxValue;

        return actorTarget.GetDistanceFrom(transform);
    }

    private Vector2 GetDirectionToTarget()
    {
        Transform targetTransform = GetTargetTransform();
        if (targetTransform == null)
            return Vector2.zero;

        return ((Vector2)targetTransform.position - (Vector2)transform.position).normalized;
    }

    private float GetHpRatio()
    {
        if (enemy == null || enemy.health == null || enemy.health.MaxHp <= 0f)
            return 1f;

        return enemy.health.Hp / enemy.health.MaxHp;
    }

    private void SpawnVisual(GameObject prefab, Vector3 position, float lifeTime)
    {
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);

        if (lifeTime > 0f)
            Destroy(instance, lifeTime);
    }

    private void ApplyRuntimeModifier(EnemyPatternInfo info)
    {
        moveSpeedMultiplier = Mathf.Max(0f, info.moveSpeedMultiplier);
        attackDamageMultiplier = Mathf.Max(0f, info.attackDamageMultiplier);
        attackCooldownMultiplier = Mathf.Max(0.01f, info.attackCooldownMultiplier);
        attackRangeMultiplier = Mathf.Max(0f, info.attackRangeMultiplier);
        incomingDamageMultiplier = Mathf.Max(0f, info.incomingDamageMultiplier);

        if (enemy != null)
            enemy.RefreshBuffedStat();
    }

    public void ApplyExternalModifier(EnemyPatternInfo info)
    {
        if (info == null || info.duration <= 0f)
            return;

        StartCoroutine(ExternalModifierRoutine(info));
    }

    private IEnumerator ExternalModifierRoutine(EnemyPatternInfo info)
    {
        ApplyRuntimeModifier(info);
        yield return new WaitForSeconds(info.duration);
        ResetRuntimeModifiers();
    }

    private void ResetRuntimeModifiers()
    {
        moveSpeedMultiplier = 1f;
        attackDamageMultiplier = 1f;
        attackCooldownMultiplier = 1f;
        attackRangeMultiplier = 1f;
        incomingDamageMultiplier = 1f;

        if (enemy != null)
            enemy.RefreshBuffedStat();
    }

    public float ModifyMoveSpeed(float value)
    {
        return value * moveSpeedMultiplier;
    }

    public float ModifyAttackDamage(float value)
    {
        return value * attackDamageMultiplier;
    }

    public float ModifyAttackCooldown(float value)
    {
        return value * attackCooldownMultiplier;
    }

    public float ModifyAttackRange(float value)
    {
        return value * attackRangeMultiplier;
    }

    public float ModifyIncomingDamage(float value)
    {
        return value * incomingDamageMultiplier;
    }

    public void NotifyDamaged(float damage)
    {
        if (isExecuting)
            return;

        EnemyPatternRuntime runtime = PickReactivePattern(EnemyPatternConditionType.AfterDamaged);
        if (runtime != null)
            queuedReactivePattern = runtime;
    }

    public bool TryHandleLethalDamage(float incomingDamage)
    {
        if (enemy == null || enemy.health == null)
            return false;

        if (enemy.health.Hp - incomingDamage > 0f)
            return false;

        EnemyPatternRuntime runtime = PickReactivePattern(EnemyPatternConditionType.OnLethalDamage);
        if (runtime == null)
            return false;

        runtime.StartCooldown();

        if (!IsRevivePattern(runtime.info))
        {
            ExecuteLethalInstantAction(runtime.info);
            return false;
        }

        StartCoroutine(LethalSurviveRoutine(runtime.info));
        return true;
    }

    private EnemyPatternRuntime PickReactivePattern(EnemyPatternConditionType conditionType)
    {
        List<EnemyPatternRuntime> candidates = new List<EnemyPatternRuntime>();
        float totalWeight = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (runtime.info == null || runtime.info.conditionType != conditionType)
                continue;

            if (!CanRunPattern(runtime, true))
                continue;

            candidates.Add(runtime);
            totalWeight += Mathf.Max(0f, runtime.info.weight);
        }

        if (candidates.Count == 0 || totalWeight <= 0f)
            return null;

        float pick = Random.Range(0f, totalWeight);
        float sum = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            sum += Mathf.Max(0f, candidates[i].info.weight);
            if (pick <= sum)
                return Random.value <= candidates[i].info.chance ? candidates[i] : null;
        }

        return null;
    }


    private bool IsRevivePattern(EnemyPatternInfo info)
    {
        if (info == null)
            return false;

        if (!string.IsNullOrEmpty(info.memo) && info.memo.Contains("부활"))
            return true;

        return info.actionType == EnemyPatternActionType.None;
    }

    private void ExecuteLethalInstantAction(EnemyPatternInfo info)
    {
        if (info == null)
            return;

        switch (info.actionType)
        {
            case EnemyPatternActionType.SupportNearbyEnemies:
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.01f, info.radius), info.targetLayerMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    Enemy otherEnemy = hits[i].GetComponentInParent<Enemy>();
                    if (otherEnemy == null || otherEnemy.IsDead)
                        continue;

                    if (info.healAmount > 0f && otherEnemy.health != null)
                        otherEnemy.Heal(info.healAmount);

                    EnemyPatternRunner otherRunner = otherEnemy.GetComponent<EnemyPatternRunner>();
                    if (otherRunner != null)
                        otherRunner.ApplyExternalModifier(info);
                }
                SpawnVisual(info.effectPrefab, transform.position, 1f);
                break;
            }
            case EnemyPatternActionType.SpawnPrefab:
            {
                if (info.spawnPrefab == null)
                    break;

                int count = Mathf.Max(1, info.spawnCount);
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = Random.insideUnitCircle * info.spawnSpreadRadius;
                    Instantiate(info.spawnPrefab, transform.position + (Vector3)offset, Quaternion.identity);
                }
                break;
            }
            default:
                SpawnVisual(info.effectPrefab, transform.position, 1f);
                break;
        }
    }

    private IEnumerator LethalSurviveRoutine(EnemyPatternInfo info)
    {
        isExecuting = true;
        isBlockingDefaultAI = true;
        currentPatternName = string.IsNullOrEmpty(info.patternName) ? "LethalSurvive" : info.patternName;

        if (enemy != null && enemy.health != null)
        {
            float leaveHpDamage = Mathf.Max(0f, enemy.health.Hp - 1f);
            if (leaveHpDamage > 0f)
                enemy.health.TakeDamage(leaveHpDamage);
        }

        yield return TelegraphRoutine(info);

        if (enemy != null && enemy.health != null)
        {
            float targetHp = enemy.health.MaxHp * Mathf.Clamp01(info.hpRatio);
            float healAmount = Mathf.Max(0f, targetHp - enemy.health.Hp);
            enemy.Heal(healAmount);
        }

        SpawnVisual(info.effectPrefab, transform.position, 1f);

        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (patternData == null || patternData.patterns == null)
            return;

        for (int i = 0; i < patternData.patterns.Count; i++)
        {
            EnemyPatternInfo info = patternData.patterns[i];
            if (info == null || !info.enabled)
                continue;

            if (info.radius > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, info.radius);
            }
        }
    }
#endif
}
