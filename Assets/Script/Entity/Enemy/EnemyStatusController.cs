using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStatusController : MonoBehaviour
{
    [Header("Immunity")]
    [Tooltip("이 적은 항상 기절에 면역입니다. 풀 재사용 시에도 유지됩니다.")]
    [SerializeField] private bool stunImmune;

    [Header("Runtime")]
    [SerializeField] private float stunRemainingTime;

    private readonly ItemEffectCompletionGroup stunCompletion = new ItemEffectCompletionGroup();
    private Enemy enemy;
    private bool runtimeStunImmune;
    private readonly List<DamageOverTimeState> damageOverTimeStates = new List<DamageOverTimeState>();
    private readonly List<DamageOverTimeState> damageOverTimeUpdateBuffer = new List<DamageOverTimeState>();

    private sealed class DamageOverTimeState
    {
        public DamageOverTimeHitEffectData effect;
        public HitEffectContext context;
        public readonly ItemEffectCompletionGroup completion = new ItemEffectCompletionGroup();
        public int targetLifeId;
        public float damagePerTick;
        public float remainingDuration;
        public float tickInterval;
        public float timeUntilNextTick;
        public int revision;
    }

    private sealed class TimedControlState
    {
        public int targetLifeId;
        public float remaining;
        public bool root;
        public ItemEffectContext context;
        public ItemEffectLease lease;
        public TargetOverrideHandle targetOverride;
        public EffectVisualData endVisual;
    }
    private readonly List<TimedControlState> timedControls = new List<TimedControlState>();
    private readonly List<TimedControlState> timedControlUpdateBuffer = new List<TimedControlState>();
    public int ActiveRootCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < timedControls.Count; i++) if (timedControls[i].root) count++;
            return count;
        }
    }

    public bool ApplyRoot(float duration, HitEffectContext context = null, EffectVisualData endVisual = null)
    {
        if (!CanApplyControl(duration, context)) return false;
        TimedControlState state = CreateControl(duration, context, endVisual);
        state.root = true;
        timedControls.Add(state);
        enemy.SetRooted(true);
        return true;
    }

    public bool ApplyTargetOverride(IDamageable target, float duration, int priority = 0,
        HitEffectContext context = null, EffectVisualData endVisual = null)
    {
        if (!CanApplyControl(duration, context) || enemy.actorTarget == null) return false;
        TargetOverrideHandle handle = enemy.actorTarget.AddTargetOverride(target, priority);
        if (handle == null) return false;
        TimedControlState state = CreateControl(duration, context, endVisual);
        state.targetOverride = handle;
        timedControls.Add(state);
        return true;
    }

    private bool CanApplyControl(float duration, HitEffectContext context)
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
        return isActiveAndEnabled && enemy != null && enemy.CanReceiveHitEffects &&
            duration > 0f && !float.IsNaN(duration) && !float.IsInfinity(duration) &&
            (context == null || (context.IsTargetValid && context.CreateItemContext().CanContinue));
    }

    private TimedControlState CreateControl(float duration, HitEffectContext context, EffectVisualData endVisual)
    {
        ItemEffectContext itemContext = context != null ? context.CreateItemContext() : null;
        return new TimedControlState
        {
            targetLifeId = enemy.HitEffectLifeId,
            remaining = duration,
            context = itemContext,
            lease = itemContext != null ? itemContext.RetainLifetime() : null,
            endVisual = endVisual
        };
    }

    private void UpdateTimedControls(float deltaTime)
    {
        timedControlUpdateBuffer.Clear();
        timedControlUpdateBuffer.AddRange(timedControls);
        try
        {
            for (int i = 0; i < timedControlUpdateBuffer.Count; i++)
            {
                TimedControlState state = timedControlUpdateBuffer[i];
                if (!timedControls.Contains(state)) continue;
                if (enemy == null || !enemy.CanReceiveHitEffects || enemy.HitEffectLifeId != state.targetLifeId ||
                    (state.context != null && !state.context.CanContinue) ||
                    (state.targetOverride != null && !state.targetOverride.IsValid))
                { FinishControl(state, false); continue; }
                state.remaining -= Mathf.Max(0f, deltaTime);
                if (state.remaining <= 0f) FinishControl(state, true);
            }
        }
        finally { timedControlUpdateBuffer.Clear(); }
    }

    private void FinishControl(TimedControlState state, bool completed)
    {
        if (!timedControls.Remove(state)) return;
        if (state.targetOverride != null) state.targetOverride.Dispose();
        if (enemy != null) enemy.SetRooted(ActiveRootCount > 0);
        try
        {
            if (completed && state.endVisual != null && enemy != null && enemy.CanReceiveHitEffects &&
                enemy.HitEffectLifeId == state.targetLifeId && (state.context == null || state.context.CanContinue))
                state.endVisual.Play(new EffectVisualContext(enemy.transform.position, Quaternion.identity));
        }
        finally { if (state.lease != null) state.lease.Finish(completed); }
    }

    public void ClearMovementAndTargetControls()
    {
        while (timedControls.Count > 0) FinishControl(timedControls[timedControls.Count - 1], false);
        if (enemy != null) enemy.SetRooted(false);
    }

    public float StunRemainingTime => stunRemainingTime;
    public bool IsStunImmune => stunImmune || runtimeStunImmune;
    public int ActiveDamageOverTimeCount => damageOverTimeStates.Count;

    public void Bind(Enemy owner)
    {
        enemy = owner;
    }

    public bool ApplyStun(float duration, HitEffectContext context = null)
    {
        if (!isActiveAndEnabled || IsStunImmune)
            return false;

        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (enemy == null || !enemy.CanReceiveHitEffects)
            return false;

        // 죽음 패턴 처리 중에는 기절로 사망 처리를 취소하지 않는다.
        if (!enemy.SetStunned(true))
            return false;

        // 짧은 기절이 기존의 긴 기절 시간을 줄이지 않도록 한다.
        stunRemainingTime = Mathf.Max(stunRemainingTime, Mathf.Max(0.01f, duration));
        if (context != null) stunCompletion.Track(context.CreateItemContext(), null, enemy.transform);
        return true;
    }

    // 패턴이나 스킬에서 임시 면역을 켜고 끈다. 적 자체의 면역 설정은 유지한다.
    public void SetRuntimeStunImmunity(bool immune)
    {
        runtimeStunImmune = immune;
        if (IsStunImmune)
            ClearStun();
    }

    public bool ApplyDamageOverTime(
        DamageOverTimeHitEffectData effect,
        float damagePerTick,
        float duration,
        float tickInterval,
        HitEffectContext context = null
    )
    {
        if (!isActiveAndEnabled || effect == null || damagePerTick <= 0f ||
            float.IsNaN(damagePerTick) || float.IsInfinity(damagePerTick) ||
            float.IsNaN(duration) || float.IsInfinity(duration) ||
            float.IsNaN(tickInterval) || float.IsInfinity(tickInterval))
            return false;

        if (enemy == null)
            enemy = GetComponent<Enemy>();
        if (enemy == null || !enemy.CanReceiveHitEffects)
            return false;

        float safeDuration = Mathf.Max(0.01f, duration);
        float safeInterval = Mathf.Max(0.01f, tickInterval);
        int lifeId = enemy.HitEffectLifeId;
        for (int i = 0; i < damageOverTimeStates.Count; i++)
        {
            DamageOverTimeState state = damageOverTimeStates[i];
            if (state.effect != effect || state.targetLifeId != lifeId)
                continue;

            state.context = context;
            if (context != null) state.completion.Track(context.CreateItemContext(), effect.endVisualData, enemy.transform);
            state.damagePerTick = damagePerTick;
            state.remainingDuration = Mathf.Max(state.remainingDuration, safeDuration);
            state.tickInterval = safeInterval;
            // 매 명중마다 타이머를 처음부터 되돌려 도트 피해가 영원히 밀리지 않게 한다.
            state.timeUntilNextTick = Mathf.Min(Mathf.Max(0f, state.timeUntilNextTick), safeInterval);
            state.revision++;
            return true;
        }

        DamageOverTimeState created = new DamageOverTimeState
        {
            effect = effect,
            targetLifeId = lifeId,
            damagePerTick = damagePerTick,
            remainingDuration = safeDuration,
            tickInterval = safeInterval,
            timeUntilNextTick = safeInterval,
            context = context
        };
        if (context != null) created.completion.Track(context.CreateItemContext(), effect.endVisualData, enemy.transform);
        damageOverTimeStates.Add(created);
        return true;
    }

    private void Update() { TickStatuses(Time.deltaTime); }

    public void TickStatuses(float deltaTime)
    {
        if (stunRemainingTime <= 0f && damageOverTimeStates.Count == 0 && timedControls.Count == 0)
            return;

        if (enemy == null || !enemy.CanReceiveHitEffects)
        {
            ClearAllStatuses();
            return;
        }

        float statusDelta = TimeStopRuntime.IsStopped(TimeStopTargets.EnemyStatusTimers) ? 0f : Mathf.Max(0f, deltaTime);
        UpdateTimedControls(statusDelta);

        // 플레이 중 인스펙터에서 면역을 켠 경우에도 기존 기절을 해제한다.
        if (stunRemainingTime > 0f)
        {
            if (IsStunImmune)
                ClearStun();
            else
            {
                stunRemainingTime = Mathf.Max(0f, stunRemainingTime - statusDelta);
                if (stunRemainingTime <= 0f)
                    ClearStun(true);
            }
        }

        if (statusDelta > 0f && damageOverTimeStates.Count > 0)
            UpdateDamageOverTime(statusDelta);
    }

    private void UpdateDamageOverTime(float deltaTime)
    {
        const float timerEpsilon = 0.00001f;
        int lifeId = enemy.HitEffectLifeId;
        damageOverTimeUpdateBuffer.Clear();
        damageOverTimeUpdateBuffer.AddRange(damageOverTimeStates);
        try
        {
            for (int i = 0; i < damageOverTimeUpdateBuffer.Count; i++)
            {
                DamageOverTimeState state = damageOverTimeUpdateBuffer[i];
                if (!damageOverTimeStates.Contains(state))
                    continue;

                if (state.targetLifeId != lifeId)
                {
                    damageOverTimeStates.Remove(state);
                    state.completion.Finish(false);
                    continue;
                }

                int revision = state.revision;
                // 도트의 남은 지속시간까지만 진행해 종료 시점 이후 피해를 막는다.
                float activeDelta = Mathf.Min(deltaTime, Mathf.Max(0f, state.remainingDuration));
                state.remainingDuration = Mathf.Max(0f, state.remainingDuration - activeDelta);
                state.timeUntilNextTick -= activeDelta;

                while (state.timeUntilNextTick <= timerEpsilon)
                {
                    state.timeUntilNextTick += state.tickInterval;
                    //공격 지점
                    state.effect.PlayHit(state.context);
                    enemy.TakeDamage(state.damagePerTick);

                    // 피해 이벤트 중 사망·풀 재사용·상태 정리가 일어날 수 있다.
                    if (!isActiveAndEnabled || enemy == null || !enemy.CanReceiveHitEffects ||
                        enemy.HitEffectLifeId != lifeId)
                        return;
                    if (!damageOverTimeStates.Contains(state) || state.revision != revision)
                        break;
                }

                if (state.remainingDuration <= 0f)
                {
                    damageOverTimeStates.Remove(state);
                    state.completion.Finish(true);
                }
            }
        }
        finally
        {
            damageOverTimeUpdateBuffer.Clear();
        }
    }

    public void ClearStun(bool completed = false)
    {
        stunRemainingTime = 0f;
        stunCompletion.Finish(completed);
        if (enemy != null)
            enemy.SetStunned(false);
    }

    public void ClearAllStatuses()
    {
        runtimeStunImmune = false;
        ClearStun();
        ClearDamageOverTime();
        ClearMovementAndTargetControls();
    }

    public void ClearDamageOverTime()
    {
        for (int i = 0; i < damageOverTimeStates.Count; i++) damageOverTimeStates[i].completion.Finish(false);
        damageOverTimeStates.Clear();
    }

    private void OnDisable()
    {
        ClearAllStatuses();
    }
}
