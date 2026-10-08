using System.Collections.Generic;
using UnityEngine;

// Every application has its own timer, life identity and completion lease.
public sealed class HealthRegenerationRunner : MonoBehaviour
{
    private sealed class State
    {
        public RegenerationEffect effect;
        public ItemEffectContext context;
        public ItemEffectLease lease;
        public int lifeId;
        public float elapsed;
        public float nextTick;
        public float duration;
    }
    private Health health;
    private readonly List<State> states = new List<State>();
    private bool ticking;
    public int ActiveCount => states.Count;
    private void Awake() { health = GetComponent<Health>(); }
    public void Add(RegenerationEffect effect, ItemEffectContext context)
    {
        if (health == null) health = GetComponent<Health>();
        if (effect == null || context == null || !context.CanContinue || health == null || health.IsDead) return;
        RegenerationStat stat = context.GetCurrentStat(effect, effect.regenerationStat);
        if (stat == null || stat.regenerationDuration <= 0f) return;
        State state = new State
        {
            effect = effect, context = context.Copy(health.transform.position, context.direction),
            lease = context.RetainLifetime(), lifeId = health.LifeId,
            nextTick = stat.regenerationInterval, duration = stat.regenerationDuration
        };
        states.Add(state);
        if (effect.firstTick == RegenerationFirstTick.Immediate) Heal(state, stat);
    }
    private void Update() { Tick(Time.deltaTime); }
    public void Tick(float deltaTime)
    {
        if (ticking || states.Count == 0) return;
        ticking = true;
        try { TickStates(deltaTime); }
        finally { ticking = false; }
    }
    private void TickStates(float deltaTime)
    {
        deltaTime = EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
        State[] updating = states.ToArray();
        for (int i = 0; i < updating.Length; i++)
        {
            State state = updating[i];
            if (!states.Contains(state)) continue;
            bool valid = IsValid(state);
            if (!valid) { Remove(state, false); continue; }
            if (health.AreStatusTimersStopped) continue;
            state.elapsed += deltaTime;
            int budget = 128;
            while (state.nextTick <= state.elapsed + 0.00001f && state.nextTick <= state.duration + 0.00001f && budget-- > 0)
            {
                RegenerationStat current = state.context.GetCurrentStat(state.effect, state.effect.regenerationStat);
                if (current == null) { valid = false; break; }
                Heal(state, current);
                state.nextTick += current.regenerationInterval;
                if (!IsValid(state) || !states.Contains(state)) { valid = false; break; }
            }
            // A large frame catches up over subsequent frames without dropping scheduled ticks.
            bool remainingTicks = state.nextTick <= state.duration + 0.00001f && state.nextTick <= state.elapsed + 0.00001f;
            if (!valid || (state.elapsed >= state.duration && !remainingTicks)) Remove(state, valid);
        }
    }
    private bool IsValid(State state) => health != null && health.isActiveAndEnabled && !health.IsDead &&
        state.lifeId == health.LifeId && state.effect != null && state.context.CanContinue;
    private void Heal(State state, RegenerationStat stat)
    {
        health.ApplyHealing(HealthAmountUtility.Resolve(stat.regenerationAmount, state.effect.amountMode, health));
    }
    private void Remove(State state, bool completed)
    {
        if (!states.Remove(state)) return;
        if (state.lease != null) state.lease.Finish(completed);
    }
    private void OnDisable()
    {
        State[] removed = states.ToArray();
        states.Clear();
        for (int i = 0; i < removed.Length; i++) if (removed[i].lease != null) removed[i].lease.Cancel();
    }
    private void OnDestroy() { OnDisable(); }
}
