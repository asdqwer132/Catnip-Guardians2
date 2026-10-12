using UnityEngine;

[CreateAssetMenu(fileName = "SummonHealth", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Health")]
public sealed class SummonHealthModule : SummonBehaviourModule
{
    [Header("기본 스탯 프로필 · 체력")]
    public SummonHealthStatDefinition stats;
    public HealthDefenseFormula defenseFormula = HealthDefenseFormula.FlatReduction;
    public override SummonModuleStatDefinition StatDefinition => stats;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor actor) => new Runtime(actor, this);
    private sealed class Runtime : SummonModuleRuntime<SummonHealthStat>
    {
        private readonly HealthDefenseFormula formula;
        private bool disposed;
        public Runtime(SummonActor actor, SummonHealthModule module) : base(actor, module, module.stats)
        {
            formula = module.defenseFormula;
            actor.ConfigureHealth(stat, initialize: true, defenseFormula: formula);
        }
        public override void RefreshBuffedStat()
        {
            if (disposed) return;
            base.RefreshBuffedStat();
            summon.ConfigureHealth(stat, initialize: false, defenseFormula: formula);
        }
        public override void Tick(float deltaTime) { }
        public override void Dispose() { disposed = true; summon.ReleaseHealthModule(); }
    }
}
