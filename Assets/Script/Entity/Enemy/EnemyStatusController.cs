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

    private Enemy enemy;
    private bool runtimeStunImmune;
    private readonly List<DamageOverTimeState> damageOverTimeStates = new List<DamageOverTimeState>();
    private readonly List<DamageOverTimeState> damageOverTimeUpdateBuffer = new List<DamageOverTimeState>();

    private sealed class DamageOverTimeState
    {
        public DamageOverTimeHitEffectData effect;
        public int targetLifeId;
        public float damagePerTick;
        public float remainingDuration;
        public float tickInterval;
        public float timeUntilNextTick;
        public int revision;
    }

    public float StunRemainingTime => stunRemainingTime;
    public bool IsStunImmune => stunImmune || runtimeStunImmune;
    public int ActiveDamageOverTimeCount => damageOverTimeStates.Count;

    public void Bind(Enemy owner)
    {
        enemy = owner;
    }

    public bool ApplyStun(float duration)
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
        float tickInterval
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

            state.damagePerTick = damagePerTick;
            state.remainingDuration = Mathf.Max(state.remainingDuration, safeDuration);
            state.tickInterval = safeInterval;
            // 매 명중마다 타이머를 처음부터 되돌려 도트 피해가 영원히 밀리지 않게 한다.
            state.timeUntilNextTick = Mathf.Min(Mathf.Max(0f, state.timeUntilNextTick), safeInterval);
            state.revision++;
            return true;
        }

        damageOverTimeStates.Add(new DamageOverTimeState
        {
            effect = effect,
            targetLifeId = lifeId,
            damagePerTick = damagePerTick,
            remainingDuration = safeDuration,
            tickInterval = safeInterval,
            timeUntilNextTick = safeInterval
        });
        return true;
    }

    private void Update()
    {
        if (stunRemainingTime <= 0f && damageOverTimeStates.Count == 0)
            return;

        if (enemy == null || !enemy.CanReceiveHitEffects)
        {
            ClearAllStatuses();
            return;
        }

        // 플레이 중 인스펙터에서 면역을 켠 경우에도 기존 기절을 해제한다.
        if (stunRemainingTime > 0f)
        {
            if (IsStunImmune)
                ClearStun();
            else
            {
                stunRemainingTime = Mathf.Max(0f, stunRemainingTime - Time.deltaTime);
                if (stunRemainingTime <= 0f)
                    ClearStun();
            }
        }

        if (Time.deltaTime > 0f && damageOverTimeStates.Count > 0)
            UpdateDamageOverTime(Time.deltaTime);
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
                    state.effect.PlayHit();
                    enemy.TakeDamage(state.damagePerTick);

                    // 피해 이벤트 중 사망·풀 재사용·상태 정리가 일어날 수 있다.
                    if (!isActiveAndEnabled || enemy == null || !enemy.CanReceiveHitEffects ||
                        enemy.HitEffectLifeId != lifeId)
                        return;
                    if (!damageOverTimeStates.Contains(state) || state.revision != revision)
                        break;
                }

                if (state.remainingDuration <= 0f)
                    damageOverTimeStates.Remove(state);
            }
        }
        finally
        {
            damageOverTimeUpdateBuffer.Clear();
        }
    }

    public void ClearStun()
    {
        stunRemainingTime = 0f;
        if (enemy != null)
            enemy.SetStunned(false);
    }

    public void ClearAllStatuses()
    {
        runtimeStunImmune = false;
        ClearStun();
        ClearDamageOverTime();
    }

    public void ClearDamageOverTime()
    {
        damageOverTimeStates.Clear();
    }

    private void OnDisable()
    {
        ClearAllStatuses();
    }
}
