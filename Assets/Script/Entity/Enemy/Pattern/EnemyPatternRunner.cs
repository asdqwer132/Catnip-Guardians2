using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatternRunner : MonoBehaviour
{
    [Header("Data")]
    public EnemyPatternSetData patternData;

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

    private float patternCooldownTimer;
    private bool initialized;
    private bool isHandlingLethalDamage;
    private float pendingLethalDamage;

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

        EnemyPatternRuntime nextPattern = ConsumeQueuedDeathPattern();

        if (nextPattern == null)
            nextPattern = ConsumeQueuedReactivePattern();

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

        patternCooldownTimer = Mathf.Max(0.05f, patternData.patternCooldown);
        UpdatePatternCooldownDebug();

        return nextPattern;
    }
    private EnemyPatternRuntime PickWeightedAutoPattern()
    {
        float totalWeight = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];

            if (!CanRunPattern(runtime, false))
                continue;

            if (!IsAutoPickGroup(runtime.Entry.pickGroup))
                continue;

            totalWeight += Mathf.Max(0f, runtime.Entry.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float random = Random.Range(0f, totalWeight);
        float current = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];

            if (!CanRunPattern(runtime, false))
                continue;

            if (!IsAutoPickGroup(runtime.Entry.pickGroup))
                continue;

            current += Mathf.Max(0f, runtime.Entry.weight);

            if (random <= current)
                return runtime;
        }

        return null;
    }

    private bool IsAutoPickGroup(EnemyPatternPickGroup group)
    {
        if (group == EnemyPatternPickGroup.Random1)
            return true;

        if (group == EnemyPatternPickGroup.Random2)
            return CanUseRandom2Group();

        return false;
    }
    private bool CanUseRandom2Group()
    {
        if (patternData == null)
            return false;

        if (!patternData.useRandom2OnlyBelowHp)
            return true;

        if (context == null)
            return false;

        return context.GetHpRatio() <= patternData.random2HpRatio;
    }
    private void UpdatePatternCooldownDebug()
    {
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
        if (runtime == null || runtime.Entry == null)
            return;

        if (patternCoroutine != null)
            StopCoroutine(patternCoroutine);

        patternCoroutine = StartCoroutine(RunPattern(runtime));
    }

    private IEnumerator RunPattern(EnemyPatternRuntime runtime)
    {
        EnemyPatternEntry entry = runtime.Entry;

        isExecuting = true;
        isBlockingDefaultAI = entry.blockDefaultAI;
        currentPatternName = string.IsNullOrEmpty(entry.patternName) ? entry.pickGroup.ToString() : entry.patternName;

        if (patternData != null)
        {
            if (patternData.cancelDefaultAttackOnPatternStart)
                context.CancelDefaultAttack();

            if (patternData.stopMoveOnPatternStart)
                context.StopMove();
        }

        if (patternData != null && patternData.showLog)
            Debug.Log($"[EnemyPatternRunner] Start Pattern: {currentPatternName}", this);

        if (entry.actions != null)
        {
            for (int i = 0; i < entry.actions.Count; i++)
                entry.actions[i]?.OnPatternStart(context, entry);

            for (int i = 0; i < entry.actions.Count; i++)
            {
                EnemyPatternAction action = entry.actions[i];
                if (action == null)
                    continue;

                yield return action.Execute(context, entry);

                if (enemy == null || enemy.IsDead)
                    break;
            }

            for (int i = entry.actions.Count - 1; i >= 0; i--)
                entry.actions[i]?.OnPatternEnd(context, entry);
        }

        runtime.StartCooldown();

        if (patternData != null && patternData.showLog)
            Debug.Log($"[EnemyPatternRunner] End Pattern: {currentPatternName}", this);

        isExecuting = false;
        isBlockingDefaultAI = false;
        currentPatternName = "None";
        patternCoroutine = null;

        if (isHandlingLethalDamage)
            FinishLethalDamagePattern();
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
            return false;

        if (enemy.health.Hp - damage > 0f)
            return false;

        EnemyPatternRuntime deathPattern = PickReactivePattern(EnemyPatternPickGroup.Death, EnemyPatternConditionType.OnLethalDamage);
        if (deathPattern == null)
            return false;

        isHandlingLethalDamage = true;
        pendingLethalDamage = damage;
        queuedDeathPattern = deathPattern;
        return true;
    }

    private EnemyPatternRuntime PickReactivePattern(EnemyPatternPickGroup group, EnemyPatternConditionType requiredCondition)
    {
        float totalWeight = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (!CanRunPattern(runtime, false))
                continue;

            if (runtime.Entry.pickGroup != group)
                continue;

            if (!runtime.Entry.HasCondition(requiredCondition))
                continue;

            totalWeight += Mathf.Max(0f, runtime.Entry.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float random = Random.Range(0f, totalWeight);
        float current = 0f;

        for (int i = 0; i < runtimes.Count; i++)
        {
            EnemyPatternRuntime runtime = runtimes[i];
            if (!CanRunPattern(runtime, false))
                continue;

            if (runtime.Entry.pickGroup != group)
                continue;

            if (!runtime.Entry.HasCondition(requiredCondition))
                continue;

            current += Mathf.Max(0f, runtime.Entry.weight);
            if (random <= current)
                return runtime;
        }

        return null;
    }

    private void FinishLethalDamagePattern()
    {
        if (enemy != null && !enemy.IsDead)
            enemy.ApplyDamageWithoutPattern(pendingLethalDamage);

        isHandlingLethalDamage = false;
        pendingLethalDamage = 0f;
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
