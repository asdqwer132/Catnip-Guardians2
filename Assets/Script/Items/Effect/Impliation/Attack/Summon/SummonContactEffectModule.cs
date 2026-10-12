using UnityEngine;

[CreateAssetMenu(fileName = "SummonContactEffects", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Contact Effects")]
public sealed class SummonContactEffectModule : SummonBehaviourModule
{
    [Header("기본 스탯 프로필 · 접촉 이펙트")]
    public SummonContactEffectStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    [Header("발동 방식")]
    public SummonContactTriggerMode triggerMode;
    public bool limitActivations;
    public SummonContactLimitScope limitScope;
    [Header("접촉 시 실행할 이펙트")]
    [Tooltip("기절/버프 등 Hit Effect를 연결합니다. 일반 아이템 효과는 Execute Effects에 넣어 연결합니다.")]
    public HitEffectData[] contactEffects;
    public override void Prepare(ItemEffectContext context) => HitEffectPreparation.Prepare(contactEffects, context);
    public override SummonBehaviourRuntime CreateRuntime(SummonActor actor) => new Runtime(actor, this);
    private sealed class Runtime : SummonModuleRuntime<SummonContactEffectStat>
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly SummonContactActivationGate contacts;
        private readonly HitEffectData[] effects;
        private bool disposed;
        public Runtime(SummonActor actor, SummonContactEffectModule module) : base(actor, module, module.stats)
        {
            contacts = new SummonContactActivationGate(module.triggerMode, module.limitActivations, module.limitScope);
            effects = module.contactEffects != null ? (HitEffectData[])module.contactEffects.Clone() : new HitEffectData[0];
        }
        public override void Tick(float deltaTime)
        {
            if (effects.Length == 0) return;
            contacts.BeginStep(deltaTime);
            query.Scan(summon.transform.position, stat.radius, summon.enemyLayerMask);
            int life = summon.LifeId, profile = summon.ProfileVersion;
            foreach (Enemy enemy in query.Enemies)
            {
                if (disposed || !summon.CanAct || summon.LifeId != life || summon.ProfileVersion != profile) break;
                if (enemy == null || !enemy.CanReceiveHitEffects) continue;
                bool activated = contacts.TryActivate(enemy, enemy.HitEffectLifeId, stat.repeatInterval,
                    Mathf.Max(1, Mathf.FloorToInt(stat.activationCount)), () =>
                {
                    // 매 접촉 발동은 별도 공격 사건입니다. 이 모듈은 기본 피해를 주지 않습니다.
                    HitEffectContext hit = new HitEffectContext(enemy,
                        summon.CreateAttackContext(enemy.transform.position, enemy.transform.position - summon.transform.position));
                    bool applied = false;
                    foreach (HitEffectData effect in effects)
                    {
                        if (disposed || !summon.CanAct || !hit.IsHitEventValid ||
                            enemy == null || enemy.HitEffectLifeId != hit.targetLifeId) break;
                        if (effect != null) applied |= effect.TryExecute(hit);
                    }
                    return applied;
                });
                if (activated && !disposed && summon.CanAct && summon.LifeId == life)
                {
                    CaptureActivationStat(); // 실제로 적용된 경우에만 횟수제 버프를 사용합니다.
                    if (!disposed && summon.CanAct && summon.LifeId == life && summon.ProfileVersion == profile) summon.NotifyAttack();
                }
            }
            if (!disposed) contacts.EndStep(stat.repeatInterval,
                (value, id) => value is Enemy enemy && enemy != null && enemy.CanReceiveHitEffects && enemy.HitEffectLifeId == id);
        }
        public override void Dispose() { disposed = true; contacts.Clear(); }
    }
}
