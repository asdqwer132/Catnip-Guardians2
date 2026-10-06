using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatternRunner : MonoBehaviour
{
    [Header("Data")]
    public EnemyPatternSetData patternData;
    public float patternCooldown;

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

    [Header("Pattern Cooldown Debug")]
    [SerializeField] private float nextPatternRemainingTime;
    [SerializeField] private string nextPatternState = "Ready";

    [SerializeField] private int runtimeModifierCount;

    private readonly List<EnemyPatternRuntime> runtimes = new List<EnemyPatternRuntime>();
    private readonly List<EnemyPatternRuntimeModifier> runtimeModifiers = new List<EnemyPatternRuntimeModifier>();

    private EnemyPatternContext context;
    private Coroutine patternCoroutine;
    private EnemyPatternRuntime queuedReactivePattern;
    private EnemyPatternRuntime queuedDeathPattern;

    private readonly List<EnemyPatternRuntime> candidates = new List<EnemyPatternRuntime>();
    private readonly Stack<IEnumerator> executionStack = new Stack<IEnumerator>();
    private EnemyPatternRuntime activeRuntime;
    private EnemyPatternAction activeAction;
    private int executionVersion;
    private bool steppingAction;
    private bool cleanupPending;
    private bool moveStoppedBeforePattern;
    private bool attackStoppedBeforePattern;

    private float patternCooldownTimer;
    private bool initialized;
    private bool isHandlingLethalDamage;
    private float pendingLethalDamage;

    public bool IsExecuting => isExecuting;
    public bool IsHandlingLethalDamage => isHandlingLethalDamage;
    public bool IsBlockingDefaultAI => isExecuting;
    public bool CanAdvancePattern => initialized && isActiveAndEnabled && enemy != null && !enemy.IsDead &&
        (isHandlingLethalDamage || (!enemy.IsFullyStopped && !enemy.IsHitReacting &&
         (mover == null || !mover.IsBaseMovementBlocked)));

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

        if (context == null)
            context = new EnemyPatternContext(enemy, this);
        else
            context.Bind(enemy, this);
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
        ClearRuntimeModifiers();
        patternCooldown = GetRandomPatternCooldown();
        patternCooldownTimer = 0f;
        nextPatternRemainingTime = 0f;
        nextPatternState = "Ready";
        queuedReactivePattern = null;
        queuedDeathPattern = null;
        isHandlingLethalDamage = false;
        pendingLethalDamage = 0f;
        initialized = false;
    }

    private void BuildRuntimeList()
    {
        runtimes.Clear();

        if (patternData == null || patternData.patterns == null)
            return;

        for (int i = 0; i < patternData.patterns.Count; i++)
        {
            EnemyPatternEntry entry = patternData.patterns[i];
            if (entry == null)
                continue;

            runtimes.Add(new EnemyPatternRuntime(entry));
        }
    }

    private void OnDisable()
    {
        ForceStopPattern();
        ClearRuntimeModifiers();
    }

    public bool TickPattern(bool allowStart = true)
    {
        if (!initialized || !isActiveAndEnabled || enemy == null || enemy.IsDead) return false;
        TickTimers();
        if (isExecuting) return true;
        if (!allowStart || !CanAdvancePattern || patternData == null || Time.deltaTime <= 0f) return false;

        EnemyPatternRuntime next = ConsumeQueuedDeathPattern();
        if (next == null && isHandlingLethalDamage)
        {
            // 예약 후 조건이 바뀌거나 외부에서 중단해도 사망 처리가 영원히 보류되지 않는다.
            FinishLethalDamagePattern();
            return true;
        }
        if (next == null) next = ConsumeQueuedReactivePattern();
        if (next == null) next = PickAutoPattern();
        if (next == null) return false;
        StartPattern(next);
        // 즉시 끝나는 패턴도 이번 프레임에는 기본 AI가 덮어쓰지 않는다.
        return true;
    }

    private void TickTimers()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < runtimes.Count; i++)
            runtimes[i].Tick(deltaTime);

        if (patternCooldownTimer > 0f)
            patternCooldownTimer -= deltaTime;

        if (patternCooldownTimer < 0f)
            patternCooldownTimer = 0f;

        TickRuntimeModifiers(deltaTime);
        UpdatePatternCooldownDebug();
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

    private EnemyPatternRuntime ConsumeQueuedDeathPattern()
    {
        if (queuedDeathPattern == null)
            return null;

        EnemyPatternRuntime nextPattern = queuedDeathPattern;
        queuedDeathPattern = null;

        if (!CanRunPattern(nextPattern, true))
            return null;

        return nextPattern;
    }

    private EnemyPatternRuntime PickAutoPattern()
    {
        if (patternCooldownTimer > 0f)
            return null;

        EnemyPatternRuntime nextPattern = PickWeightedAutoPattern();

        if (nextPattern == null)
            return null;

        // 여기서 쿨타임 걸면 안 됨.
        // 패턴 실행 시간이 긴 경우 실행 중에 쿨타임이 다 닳아서
        // 두 번째 패턴이 또 같이 나감.

        return nextPattern;
    }
    private EnemyPatternRuntime PickWeightedAutoPattern()
    {
        return PickWeightedPattern(EnemyPatternPickGroup.Random1, EnemyPatternConditionType.Always, false);
    }

    // 확률 판정은 후보마다 한 번만 한다. 추첨 도중 후보가 바뀌지 않는다.
    private EnemyPatternRuntime PickWeightedPattern(EnemyPatternPickGroup group,
        EnemyPatternConditionType requiredCondition, bool requireCondition)
    {
        candidates.Clear();
        float totalWeight = 0f;
        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (runtime.Entry.pickGroup != group ||
                (requireCondition && !runtime.Entry.HasCondition(requiredCondition)) ||
                !CanRunPattern(runtime, false)) continue;
            float weight = Mathf.Max(0f, runtime.Entry.weight);
            if (weight <= 0f || float.IsNaN(weight) || float.IsInfinity(weight)) continue;
            candidates.Add(runtime);
            totalWeight += weight;
        }
        if (candidates.Count == 0) return null;
        float pick = Random.Range(0f, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            pick -= Mathf.Max(0f, candidates[i].Entry.weight);
            if (pick <= 0f) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private bool IsAutoPickGroup(EnemyPatternPickGroup group)
    {
        if (group == EnemyPatternPickGroup.Random1)
            return true;


        return false;
    }
    private void UpdatePatternCooldownDebug()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        if (!initialized || patternData == null)
        {
            nextPatternRemainingTime = 0f;
            nextPatternState = "Not Initialized";
            return;
        }

        if (enemy == null || enemy.IsDead)
        {
            nextPatternRemainingTime = 0f;
            nextPatternState = "Dead";
            return;
        }

        float remainingTime = Mathf.Max(0f, patternCooldownTimer);

        if (remainingTime <= 0f)
        {
            float entryCooldown = GetMinAvailableAutoPatternCooldown();

            if (entryCooldown > 0f)
                remainingTime = entryCooldown;
        }

        nextPatternRemainingTime = remainingTime;

        if (isExecuting)
        {
            nextPatternState = remainingTime > 0f
                ? $"Executing / Next {remainingTime:0.0}s"
                : "Executing";
        }
        else
        {
            nextPatternState = remainingTime > 0f
                ? $"Next {remainingTime:0.0}s"
                : "Ready";
        }
#endif
    }

    private float GetMinAvailableAutoPatternCooldown()
    {
        float minCooldown = float.MaxValue;
        bool found = false;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];

            if (runtime == null || runtime.Entry == null)
                continue;

            EnemyPatternEntry entry = runtime.Entry;

            if (!entry.enabled)
                continue;

            if (runtime.Consumed)
                continue;

            if (!IsAutoPickGroup(entry.pickGroup))
                continue;

            if (!CheckConditions(entry))
                continue;

            found = true;
            minCooldown = Mathf.Min(minCooldown, runtime.CooldownTimer);
        }

        return found ? Mathf.Max(0f, minCooldown) : 0f;
    }

    private bool CanRunPattern(EnemyPatternRuntime runtime, bool ignoreChance)
    {
        if (runtime == null || runtime.Entry == null)
            return false;

        EnemyPatternEntry entry = runtime.Entry;

        if (!entry.enabled)
            return false;

        if (runtime.Consumed)
            return false;

        if (runtime.CooldownTimer > 0f)
            return false;

        if (!ignoreChance && Random.value > entry.chance)
            return false;

        return CheckConditions(entry);
    }

    private bool CheckConditions(EnemyPatternEntry entry)
    {
        if (entry.conditions == null || entry.conditions.Count == 0)
            return true;

        for (int i = 0; i < entry.conditions.Count; i++)
        {
            EnemyPatternCondition condition = entry.conditions[i];
            if (condition == null)
                continue;

            if (!condition.Check(context))
                return false;
        }

        return true;
    }

    private void StartPattern(EnemyPatternRuntime runtime)
    {
        if (runtime == null || runtime.Entry == null) return;
        StopPattern();
        activeRuntime = runtime;
        isExecuting = true;
        isBlockingDefaultAI = true;
        currentPatternName = string.IsNullOrEmpty(runtime.Entry.patternName)
            ? runtime.Entry.pickGroup.ToString() : runtime.Entry.patternName;
        moveStoppedBeforePattern = mover != null && mover.IsMoveStopped;
        attackStoppedBeforePattern = attack != null && attack.IsAttackStopped;
        unchecked { executionVersion++; }
        int version = executionVersion;

        // 패턴 설정과 관계없이 기본 이동/공격을 패턴 시작 시 인계한다.
        context.CancelDefaultAttack();
        context.StopMove();
        Coroutine started = StartCoroutine(RunPattern(runtime, version));
        // 첫 yield 전에 끝난 패턴의 낡은 코루틴 핸들을 보관하지 않는다.
        if (isExecuting && version == executionVersion) patternCoroutine = started;
    }

    private IEnumerator RunPattern(EnemyPatternRuntime runtime, int version)
    {
        EnemyPatternEntry entry = runtime.Entry;
        bool completed = false;
        try
        {
            if (patternData != null && patternData.showLog)
                Debug.Log($"[EnemyPatternRunner] Start Pattern: {currentPatternName}", this);
            if (entry.actions != null)
            {
                for (int i = 0; i < entry.actions.Count; i++)
                {
                    while (!CanAdvancePattern && version == executionVersion) yield return null;
                    if (version != executionVersion || enemy == null || enemy.IsDead) yield break;
                    EnemyPatternAction action = entry.actions[i];
                    if (action == null) continue;
                    activeAction = action;
                    action.OnPatternStart(context, entry);
                    if (version != executionVersion) yield break;
                    yield return ExecuteAction(action.Execute(context, entry), version);
                    if (version != executionVersion) yield break;
                    // 즉시 끝나는 액션도 지정한 애니메이션을 끝까지 보여준다.
                    context.StopMove();
                    while (visual != null && visual.IsCustomAnimationLocked && version == executionVersion)
                        yield return null;
                    if (version != executionVersion) yield break;
                    action.OnPatternEnd(context, entry);
                    activeAction = null;
                }
            }
            completed = true;
        }
        finally
        {
            if (version == executionVersion) FinishPattern(runtime, completed);
        }
    }

    // 중첩 IEnumerator를 한곳에서 진행한다. 상위 상태가 개입하면 진행을 멈춘다.
    private IEnumerator ExecuteAction(IEnumerator routine, int version)
    {
        if (routine == null) yield break;
        executionStack.Push(routine);
        while (executionStack.Count > 0 && version == executionVersion)
        {
            if (!CanAdvancePattern || Time.deltaTime <= 0f) { yield return null; continue; }
            IEnumerator current = executionStack.Peek();
            bool hasNext;
            steppingAction = true;
            try { hasNext = current.MoveNext(); }
            finally
            {
                steppingAction = false;
                if (cleanupPending) DisposeActionStack();
            }
            if (version != executionVersion) yield break;
            if (!hasNext)
            {
                executionStack.Pop();
                DisposeEnumerator(current);
                continue;
            }
            IEnumerator nested = current.Current as IEnumerator;
            if (nested != null) executionStack.Push(nested);
            else yield return current.Current;
        }
    }

    private static void DisposeEnumerator(IEnumerator routine)
    {
        System.IDisposable disposable = routine as System.IDisposable;
        if (disposable != null) disposable.Dispose();
    }

    private void DisposeActionStack()
    {
        cleanupPending = false;
        while (executionStack.Count > 0)
        {
            try { DisposeEnumerator(executionStack.Pop()); }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void RestorePatternState(bool cancelVisual)
    {
        if (attack != null) attack.CancelAttack();
        if (mover != null) mover.Stop();
        if (visual != null)
        {
            visual.SetPatternAnimationPaused(false);
            if (cancelVisual)
            {
                visual.CancelCustomAnimation();
                visual.ForceIdle(Vector2.zero, false, false);
            }
        }
        // 패턴 종료가 수동 전체 정지나 기절 상태를 해제하지 않게 한다.
        if (enemy != null && !enemy.IsDead && (!enemy.IsFullyStopped || isHandlingLethalDamage))
        {
            if (mover != null) mover.SetMoveStopped(moveStoppedBeforePattern);
            if (attack != null) attack.SetAttackStopped(attackStoppedBeforePattern);
        }
    }

    private void FinishPattern(EnemyPatternRuntime runtime, bool completed)
    {
        if (patternData != null && patternData.showLog)
            Debug.Log($"[EnemyPatternRunner] End Pattern: {currentPatternName}", this);
        runtime.StartCooldown(completed);
        if (steppingAction) cleanupPending = true;
        else DisposeActionStack();
        RestorePatternState(true);
        activeAction = null;
        activeRuntime = null;
        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
        patternCoroutine = null;
        SetNextPatternCooldown();
        if (isHandlingLethalDamage) FinishLethalDamagePattern();
    }

    private void SetNextPatternCooldown()
    {
        patternCooldown = GetRandomPatternCooldown();
        patternCooldownTimer = Mathf.Max(0.05f, patternCooldown);
        UpdatePatternCooldownDebug();
    }

    private float GetRandomPatternCooldown()
    {
        if (patternData == null)
            return 0.05f;

        float min = Mathf.Min(patternData.minPatternCooldown, patternData.maxPatternCooldown);
        float max = Mathf.Max(patternData.minPatternCooldown, patternData.maxPatternCooldown);

        return Random.Range(min, max);
    }
    public void ForceStopPattern()
    {
        StopPattern();

        queuedReactivePattern = null;
        queuedDeathPattern = null;

        isHandlingLethalDamage = false;
        pendingLethalDamage = 0f;
    }
    public void StopPattern()
    {
        if (!isExecuting && patternCoroutine == null) return;
        unchecked { executionVersion++; }
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
        }
        if (steppingAction) cleanupPending = true;
        else DisposeActionStack();
        if (activeAction != null && activeRuntime != null)
            activeAction.OnPatternInterrupted(context, activeRuntime.Entry);
        if (activeRuntime != null) activeRuntime.StartCooldown(false);
        RestorePatternState(true);
        activeAction = null;
        activeRuntime = null;
        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
        SetNextPatternCooldown();
    }

    public void NotifyDamaged(float damage)
    {
        if (!initialized || isExecuting)
            return;

        EnemyPatternRuntime reactive = PickReactivePattern(EnemyPatternPickGroup.Reactive, EnemyPatternConditionType.AfterDamaged);
        if (reactive != null)
            queuedReactivePattern = reactive;
    }

    public bool TryHandleLethalDamage(float damage)
    {
        if (!initialized)
            return false;

        if (enemy == null || enemy.health == null)
            return false;

        if (isHandlingLethalDamage)
        {
            // 사망 패턴 중 추가 타격이 사망 연출을 먼저 끝내지 않게 한다.
            pendingLethalDamage += Mathf.Min(damage, float.MaxValue - pendingLethalDamage);
            return true;
        }

        if (enemy.health.Hp - damage > 0f)
            return false;

        EnemyPatternRuntime deathPattern = PickReactivePattern(EnemyPatternPickGroup.Death, EnemyPatternConditionType.OnLethalDamage);
        if (deathPattern == null)
            return false;

        StopPattern();
        isHandlingLethalDamage = true;
        pendingLethalDamage = damage;
        queuedDeathPattern = deathPattern;
        return true;
    }

    private EnemyPatternRuntime PickReactivePattern(EnemyPatternPickGroup group,
        EnemyPatternConditionType requiredCondition)
    {
        return PickWeightedPattern(group, requiredCondition, true);
    }

    private void FinishLethalDamagePattern()
    {
        float damage = pendingLethalDamage;
        isHandlingLethalDamage = false;
        pendingLethalDamage = 0f;
        if (enemy != null && !enemy.IsDead) enemy.ApplyDamageWithoutPattern(damage);
    }

    #region Runtime Modifier

    public void AddRuntimeModifier(EnemyPatternRuntimeModifier modifier)
    {
        if (modifier == null)
            return;

        if (modifier.remainingTime <= 0f)
            return;

        runtimeModifiers.Add(modifier);
        runtimeModifierCount = runtimeModifiers.Count;

        if (enemy != null)
            enemy.RefreshBuffedStat();
    }

    public void ClearRuntimeModifiers()
    {
        bool hadModifier = runtimeModifiers.Count > 0;
        runtimeModifiers.Clear();
        runtimeModifierCount = 0;

        if (hadModifier && enemy != null)
            enemy.RefreshBuffedStat();
    }

    private void TickRuntimeModifiers(float deltaTime)
    {
        if (runtimeModifiers.Count == 0)
            return;

        bool removed = false;

        for (int i = runtimeModifiers.Count - 1; i >= 0; i--)
        {
            EnemyPatternRuntimeModifier modifier = runtimeModifiers[i];
            modifier.Tick(deltaTime);

            if (modifier.IsExpired)
            {
                runtimeModifiers.RemoveAt(i);
                removed = true;
            }
        }

        runtimeModifierCount = runtimeModifiers.Count;

        if (removed && enemy != null)
            enemy.RefreshBuffedStat();
    }

    public float ModifyMoveSpeed(float value)
    {
        for (int i = 0; i < runtimeModifiers.Count; i++)
            value *= runtimeModifiers[i].moveSpeedMultiplier;

        return value;
    }

    public float ModifyAttackDamage(float value)
    {
        for (int i = 0; i < runtimeModifiers.Count; i++)
            value *= runtimeModifiers[i].attackDamageMultiplier;

        return value;
    }

    public float ModifyAttackRange(float value)
    {
        for (int i = 0; i < runtimeModifiers.Count; i++)
            value *= runtimeModifiers[i].attackRangeMultiplier;

        return value;
    }

    public float ModifyAttackCooldown(float value)
    {
        for (int i = 0; i < runtimeModifiers.Count; i++)
            value *= runtimeModifiers[i].attackCooldownMultiplier;

        return Mathf.Max(0.01f, value);
    }

    public float ModifyIncomingDamage(float value)
    {
        for (int i = 0; i < runtimeModifiers.Count; i++)
            value *= runtimeModifiers[i].incomingDamageMultiplier;

        return Mathf.Max(0f, value);
    }

    #endregion
}
