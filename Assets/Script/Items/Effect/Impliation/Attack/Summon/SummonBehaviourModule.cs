using System;
using UnityEngine;

// 에셋에는 설정만, 소환수마다 생성한 Runtime에는 타이머/타깃/이동 상태를 보관한다.
public abstract class SummonBehaviourModule : ScriptableObject
{
    public virtual void Prepare(ItemEffectContext context) { }
    public abstract SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon);
}

public abstract class SummonBehaviourRuntime : IDisposable
{
    protected readonly SummonItemThrower summon;
    protected SummonBehaviourRuntime(SummonItemThrower summon) { this.summon = summon; }
    public abstract void Tick(float deltaTime);
    public virtual void Dispose() { }
}

public static class SummonDamageUtility
{
    public static bool Hit(Enemy enemy, float damage, HitEffectData[] effects, ItemEffectContext source,
        HitEffectAttackState attackState = null)
    {
        if (enemy == null || !enemy.CanReceiveHitEffects || source == null || !source.CanContinue) return false;
        HitEffectContext context = new HitEffectContext(enemy, source, attackState);
        enemy.TakeDamage(EffectStatUtility.Safe(damage, 0f, 1000000f, 0f), source);
        if (effects == null) return true;
        foreach (HitEffectData effect in effects)
        {
            if (!context.IsHitEventValid) break;
            if (effect != null) effect.TryExecute(context);
        }
        return true;
    }
}
