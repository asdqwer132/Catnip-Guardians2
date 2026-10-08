using System;
using System.Collections.Generic;
using UnityEngine;

public struct EnemyLifeKey : IEquatable<EnemyLifeKey>
{
    public readonly Enemy enemy;
    public readonly int lifeId;
    public EnemyLifeKey(Enemy enemy, int lifeId) { this.enemy = enemy; this.lifeId = lifeId; }
    public EnemyLifeKey(Enemy enemy) : this(enemy, enemy != null ? enemy.HitEffectLifeId : 0) { }
    public bool Equals(EnemyLifeKey other) => ReferenceEquals(enemy, other.enemy) && lifeId == other.lifeId;
    public override bool Equals(object other) => other is EnemyLifeKey && Equals((EnemyLifeKey)other);
    public override int GetHashCode() => (ReferenceEquals(enemy, null) ? 0 :
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(enemy)) ^ lifeId;
}

// A context copy shares this token with every attack created by the same use.
public sealed class HitEffectUseState
{
    private readonly HashSet<HitEffectData> claimed = new HashSet<HitEffectData>();
    private readonly HashSet<HitEffectData> attackClaims = new HashSet<HitEffectData>();
    public bool TryClaim(HitEffectData effect, bool attackScope = false)
        => effect != null && (attackScope ? attackClaims : claimed).Add(effect);
}

public sealed class HitEffectAttackState
{
    private readonly Dictionary<HitEffectData, HashSet<EnemyLifeKey>> targets =
        new Dictionary<HitEffectData, HashSet<EnemyLifeKey>>();
    private readonly HashSet<HitEffectData> first = new HashSet<HitEffectData>();

    public bool TryClaim(HitEffectData effect, HitEffectApplyMode mode, HitEffectContext hit)
    {
        if (effect == null || hit == null) return false;
        if (mode == HitEffectApplyMode.EveryHit) return true;
        if (mode == HitEffectApplyMode.FirstHitPerUse)
            return hit.SourceContext != null && hit.SourceContext.hitUseState.TryClaim(effect);
        if (mode == HitEffectApplyMode.FirstHitPerAttack) return first.Add(effect);
        HashSet<EnemyLifeKey> lives;
        if (!targets.TryGetValue(effect, out lives))
        { lives = new HashSet<EnemyLifeKey>(); targets.Add(effect, lives); }
        return lives.Add(new EnemyLifeKey(hit.target, hit.targetLifeId));
    }
}

// Snapshot before TakeDamage: on-hit explosions remain possible after lethal damage,
// while status effects still validate the original living target and life number.
public sealed class HitEffectDispatcher
{
    private readonly HitEffectData[] effects;
    private readonly HitEffectApplyMode mode;
    private readonly ItemEffectContext source;
    private readonly HitEffectAttackState attack = new HitEffectAttackState();
    private readonly HashSet<EnemyLifeKey> targets = new HashSet<EnemyLifeKey>();
    private bool firstHit;

    public HitEffectDispatcher(HitEffectData[] effects, HitEffectApplyMode mode, ItemEffectContext context)
    {
        this.effects = effects != null ? (HitEffectData[])effects.Clone() : null;
        this.mode = mode;
        source = context != null ? context.Copy(context.targetPosition, context.direction) : null;
        if (source != null) source.currentEffectData = context.currentEffectData;
    }

    public bool Hit(Enemy enemy, float damage, Vector3 direction)
    {
        if (enemy == null || !enemy.CanReceiveHitEffects || source == null || !source.CanContinue) return false;
        ItemEffectContext hitSource = source.Copy(enemy.transform.position, direction);
        hitSource.hitTarget = enemy;
        hitSource.hitTargetLifeId = enemy.HitEffectLifeId;
        HitEffectContext hit = new HitEffectContext(enemy, hitSource, attack);
        enemy.TakeDamage(EffectStatUtility.Safe(damage, 0f, 100000000f, 0f), hitSource);
        Dispatch(hit);
        return true;
    }

    public void Dispatch(HitEffectContext hit)
    {
        if (hit == null || !hit.IsHitEventValid || effects == null) return;
        if (mode == HitEffectApplyMode.FirstHitOnly && !targets.Add(new EnemyLifeKey(hit.target, hit.targetLifeId))) return;
        if (mode == HitEffectApplyMode.FirstHitPerAttack)
        { if (firstHit) return; firstHit = true; }
        foreach (HitEffectData effect in effects)
        {
            if (!hit.IsHitEventValid) break;
            if (effect == null) continue;
            if (mode == HitEffectApplyMode.FirstHitPerUse && !source.hitUseState.TryClaim(effect, attackScope: true)) continue;
            effect.TryExecute(hit);
        }
    }
}
