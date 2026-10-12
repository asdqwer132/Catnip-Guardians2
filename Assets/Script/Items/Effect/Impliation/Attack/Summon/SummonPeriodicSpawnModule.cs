using UnityEngine;

[CreateAssetMenu(fileName = "SummonPeriodicSpawn", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Periodic Spawn")]
public sealed class SummonPeriodicSpawnModule : SummonBehaviourModule
{
    [Header("기본 스탯 프로필 · 주기적 소환")]
    public SummonPeriodicSpawnStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    [Header("소환할 효과와 순서")]
    public SummonSpawnEffect[] spawnEffects;
    public SummonPeriodicSpawnOrder order;
    public bool limitActivations;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor actor) => new Runtime(actor, this);
    private sealed class Runtime : SummonModuleRuntime<SummonPeriodicSpawnStat>
    {
        private readonly SummonSpawnEffect[] effects;
        private readonly SummonPeriodicSpawnOrder order;
        private readonly bool limited;
        private readonly SummonPeriodicSpawnSchedule schedule = new SummonPeriodicSpawnSchedule();
        private bool disposed;
        public Runtime(SummonActor actor, SummonPeriodicSpawnModule module) : base(actor, module, module.stats)
        {
            effects = module.spawnEffects != null ? (SummonSpawnEffect[])module.spawnEffects.Clone() : new SummonSpawnEffect[0];
            order = module.order; limited = module.limitActivations;
        }
        public override void Tick(float deltaTime)
        {
            int life = summon.LifeId, profile = summon.ProfileVersion;
            bool executed = schedule.Advance(deltaTime, stat.interval, limited,
                Mathf.FloorToInt(stat.activationCount), effects.Length, order,
                count => Random.Range(0, count), index =>
                {
                    SummonSpawnEffect effect = effects[index];
                    if (effect == null || effect.summonPrefab == null || effect.spawnStat == null) return false;
                    ItemEffectContext context = summon.CreateAttackContext(summon.transform.position, Vector3.right);
                    return effect.TryExecute(context);
                }, () => !disposed && summon.CanAct && summon.LifeId == life && summon.ProfileVersion == profile);
            if (executed && !disposed && summon.CanAct && summon.LifeId == life)
            {
                CaptureActivationStat();
                summon.NotifyAttack();
            }
        }
        public override void Dispose() { disposed = true; }
    }
}
