using System;
using UnityEngine;

// 에셋에는 설정만, 소환수마다 생성한 Runtime에는 타이머/타깃/이동 상태를 보관한다.
public abstract class SummonBehaviourModule : ScriptableObject
{
    public virtual SummonModuleStatDefinition StatDefinition => null;
    public virtual void Prepare(ItemEffectContext context) { }
    public abstract SummonBehaviourRuntime CreateRuntime(SummonActor summon);
}

public abstract class SummonBehaviourRuntime : IDisposable
{
    protected readonly SummonActor summon;
    protected SummonBehaviourRuntime(SummonActor summon) { this.summon = summon; }
    public abstract void Tick(float deltaTime);
    public virtual object RuntimeStat => null;
    public virtual SummonBehaviourModule Module => null;
    public virtual void RefreshBuffedStat() { }
    public virtual void Dispose() { }
}

// 프로필은 공유하지만 기본값 복제본과 버프 결과는 소환물의 모듈 인스턴스별로 보관합니다.
public abstract class SummonModuleRuntime<T> : SummonBehaviourRuntime where T : class, IGameStat<T>, new()
{
    private readonly SummonBehaviourModule module;
    private readonly T baseStat;
    protected T stat;
    public override SummonBehaviourModule Module => module;
    public override object RuntimeStat => stat;
    protected SummonModuleRuntime(SummonActor actor, SummonBehaviourModule module,
        SummonModuleStatDefinition<T> definition) : base(actor)
    {
        this.module = module;
        baseStat = definition != null ? definition.CloneBaseStat() : new T();
        baseStat.Clamp();
        stat = summon.CalculateModuleStat(baseStat);
    }
    public override void RefreshBuffedStat() => stat = summon.CalculateModuleStat(baseStat);
    protected T CaptureActivationStat() => summon.CalculateModuleStat(baseStat, consumeUseCount: true);
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
