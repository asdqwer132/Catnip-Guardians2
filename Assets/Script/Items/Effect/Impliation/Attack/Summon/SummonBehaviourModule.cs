using System;
using UnityEngine;

// 에셋에는 설정만, 소환수마다 생성한 Runtime에는 타이머/타깃/이동 상태를 보관한다.
public abstract class SummonBehaviourModule : ScriptableObject
{
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
    public static void Hit(Enemy enemy, float damage, HitEffectData[] effects, ItemEffectContext source)
    {
        if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || source == null || !source.CanContinue) return;
        int life = enemy.HitEffectLifeId;
        enemy.TakeDamage(Mathf.Max(0f, damage));
        if (enemy == null || enemy.HitEffectLifeId != life || !enemy.CanReceiveHitEffects || effects == null) return;
        HitEffectContext context = new HitEffectContext(enemy, source);
        foreach (HitEffectData effect in effects)
        {
            if (!source.CanContinue || !context.IsTargetValid) break;
            if (effect != null) effect.TryExecute(context);
        }
    }
}
