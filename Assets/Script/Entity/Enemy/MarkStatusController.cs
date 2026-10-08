using System.Collections.Generic;
using UnityEngine;

// 타이머/중첩/정화는 ActiveBuff가 소유한다. 이 컴포넌트는 이벤트 반응만 연결한다.
[DisallowMultipleComponent]
public sealed class MarkStatusController : MonoBehaviour
{
    private sealed class MarkState
    {
        public MarkHitEffectData effect;
        public ItemEffectContext context;
        public BuffManager manager;
        public ActiveBuff marker;
        public int enemyLifeId;
        public int healthLifeId;
        public bool reacting;
        public bool damageReacted;
        public bool killReacted;
        public readonly ItemEffectCompletionGroup completion = new ItemEffectCompletionGroup();
    }
    private readonly List<MarkState> states = new List<MarkState>();
    private readonly List<MarkState> updateBuffer = new List<MarkState>();
    private Enemy enemy;
    private Health health;

    public bool Apply(MarkHitEffectData effect, HitEffectContext context)
    {
        if (effect == null || effect.markKey == null || effect.statusInfo == null ||
            context == null || !context.IsTargetValid) return false;
        ItemEffectContext item = context.CreateItemContext();
        if (!item.CanContinue) return false;
        Bind(context.target);
        BuffManager manager = item.buffManager != null ? item.buffManager : enemy.buffManager;
        if (health == null || manager == null) return false;
        ActiveBuff marker = manager.RegisterStatusForTarget(effect.markKey, effect.statusInfo, item, enemy, effect);
        if (marker == null) return false;
        for (int i = 0; i < states.Count; i++)
        {
            MarkState existing = states[i];
            if (existing.marker != marker) continue;
            existing.context = item;
            existing.effect = effect;
            existing.completion.Track(item, effect.endVisualData, enemy.transform);
            return true;
        }
        MarkState created = new MarkState
        {
            effect = effect, context = item, manager = manager, marker = marker,
            enemyLifeId = enemy.HitEffectLifeId, healthLifeId = health.LifeId
        };
        created.completion.Track(item, effect.endVisualData, enemy.transform);
        states.Add(created);
        marker.Removed += OnMarkerRemoved;
        return true;
    }

    public bool HasMark(StatusDefinition key, GameObject owner = null)
    {
        for (int i = 0; i < states.Count; i++)
            if (key != null && states[i].marker.statusDefinition == key && IsValid(states[i]) &&
                (owner == null || states[i].context.owner == owner)) return true;
        return false;
    }

    private void Bind(Enemy target)
    {
        enemy = target;
        Health next = enemy != null ? enemy.health : null;
        if (health == next) return;
        if (health != null) health.OnDamageResolved -= OnDamageResolved;
        health = next;
        if (health != null) health.OnDamageResolved += OnDamageResolved;
    }
    private bool IsCurrentLife(MarkState state) => enemy != null && health != null && isActiveAndEnabled &&
        enemy.HitEffectLifeId == state.enemyLifeId && health.LifeId == state.healthLifeId;
    private bool IsValid(MarkState state) => IsCurrentLife(state) && state.context.CanContinue &&
        state.marker != null && state.marker.StorageOwner != null && !state.marker.IsExpired;

    private void Update() { Tick(); }
    public void Tick()
    {
        updateBuffer.Clear();
        updateBuffer.AddRange(states);
        try
        {
            for (int i = 0; i < updateBuffer.Count; i++)
            {
                MarkState state = updateBuffer[i];
                if (!states.Contains(state)) continue;
                if (!IsCurrentLife(state) || !state.context.CanContinue || enemy.IsDead)
                    RemoveMarker(state);
            }
        }
        finally { updateBuffer.Clear(); }
    }

    private void OnMarkerRemoved(ActiveBuff marker, BuffRemovalReason reason)
    {
        MarkState state = null;
        for (int i = 0; i < states.Count; i++)
            if (states[i].marker == marker) { state = states[i]; break; }
        if (state == null) return;
        // 먼저 해제해서 종료 반응이 같은 표식을 새로 등록해도 이전 종료에 섞이지 않는다.
        states.Remove(state);
        marker.Removed -= OnMarkerRemoved;
        bool natural = reason == BuffRemovalReason.NaturalExpiry;
        try { if (natural) React(state, state.effect.onNaturalExpiryEffects); }
        finally { state.completion.Finish(natural); }
    }

    private void OnDamageResolved(HealthDamageResult result)
    {
        if (result.hpDamage <= 0f || result.isTransferredDamage) return;
        // 피해 콜백에서 새 표식 등록/풀 반환/후속 피해가 발생해도 현재 스냅샷만 실행한다.
        MarkState[] snapshot = states.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            MarkState state = snapshot[i];
            if (!states.Contains(state) || !IsValid(state)) continue;
            if (state.reacting)
            {
                if (result.killed && !state.killReacted)
                {
                    state.killReacted = true;
                    React(state, state.effect.onKilledEffects, result.sourceContext);
                    RemoveMarker(state);
                }
                continue;
            }
            state.reacting = true;
            try
            {
                if ((!result.killed || state.effect.reactToLethalDamage) &&
                    (!state.effect.damageReactionOnlyOnce || !state.damageReacted))
                {
                    state.damageReacted = true;
                    React(state, state.effect.onDamagedEffects, result.sourceContext);
                }
                if (result.killed && !state.killReacted && IsCurrentLife(state) && state.context.CanContinue)
                {
                    state.killReacted = true;
                    React(state, state.effect.onKilledEffects, result.sourceContext);
                    RemoveMarker(state);
                }
            }
            finally { state.reacting = false; }
        }
    }

    private void React(MarkState state, ItemEffectData[] effects, ItemEffectContext damageSource = null)
    {
        if (effects == null || enemy == null || !IsCurrentLife(state) || !state.context.CanContinue) return;
        Vector3 position = enemy.transform.position;
        ItemEffectContext reactionSource = state.effect.reactionSource == MarkReactionSource.DamageSource && damageSource != null
            ? damageSource : state.context;
        ItemEffectContext reaction = reactionSource.Copy(position, reactionSource.direction);
        if (reactionSource != state.context) reaction.InheritExecution(state.context);
        // 위치/출처를 보관한 컨텍스트는 처치된 대상의 생존 검사에 의존하지 않는다.
        for (int i = 0; i < effects.Length && i < 64 && reaction.CanContinue && IsCurrentLife(state); i++)
            if (effects[i] != null) effects[i].Execute(reaction);
    }

    private void RemoveMarker(MarkState state)
    {
        if (!states.Contains(state)) return;
        if (state.manager != null && state.manager.RemoveBuffHandle(state.marker)) return;
        states.Remove(state);
        state.marker.Removed -= OnMarkerRemoved;
        state.completion.Finish(false);
    }
    public void ClearMarks()
    {
        while (states.Count > 0) RemoveMarker(states[states.Count - 1]);
    }
    private void OnDisable()
    {
        ClearMarks();
        if (health != null) health.OnDamageResolved -= OnDamageResolved;
        health = null;
    }
}
