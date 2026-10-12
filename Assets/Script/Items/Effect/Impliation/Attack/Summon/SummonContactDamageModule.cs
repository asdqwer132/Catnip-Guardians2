using UnityEngine;

[CreateAssetMenu(fileName = "SummonContactDamage", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Contact Damage")]
public sealed class SummonContactDamageModule : SummonBehaviourModule
{
    [Header("기본 스탯 프로필 · 접촉 피해")]
    public SummonContactDamageStatDefinition stats;
    [Header("피해 주기 시작")]
    [Tooltip("즉시: 접촉 시 첫 피해. 인터벌 후: 진입 후 Interval만큼 기다린 뒤 첫 피해. 이후에는 두 옵션 모두 Interval마다 반복합니다.")]
    public SummonContactFirstHitTiming firstHitTiming = SummonContactFirstHitTiming.Immediate;
    public override SummonModuleStatDefinition StatDefinition => stats;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor actor) => new Runtime(actor, this);
    private sealed class Runtime : SummonModuleRuntime<SummonContactDamageStat>
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly SummonContactActivationGate contacts;
        private bool disposed;
        public Runtime(SummonActor actor, SummonContactDamageModule module) : base(actor, module, module.stats)
        {
            contacts = new SummonContactActivationGate(SummonContactTriggerMode.WhileTouching,
                firstHitTiming: module.firstHitTiming);
        }
        public override void Tick(float deltaTime)
        {
            contacts.BeginStep(deltaTime);
            query.Scan(summon.transform.position, stat.radius, summon.enemyLayerMask);
            int life = summon.LifeId, profile = summon.ProfileVersion;
            bool attacked = false;
            SummonContactDamageStat attack = null;
            foreach (Enemy enemy in query.Enemies)
            {
                if (disposed || !summon.CanAct || summon.LifeId != life || summon.ProfileVersion != profile) break;
                if (enemy == null || !enemy.CanReceiveHitEffects) continue;
                attacked |= contacts.TryActivate(enemy, enemy.HitEffectLifeId, stat.interval, int.MaxValue, () =>
                {
                    // 한 번의 접촉 피해 갱신에서 마지막 사용 버프를 모든 적에게 유지합니다.
                    if (attack == null) attack = CaptureActivationStat();
                    if (disposed || !summon.CanAct || summon.LifeId != life || summon.ProfileVersion != profile) return false;
                    return SummonDamageUtility.Hit(enemy, summon.CalculateDamage(attack.damage), null,
                        summon.CreateAttackContext(enemy.transform.position, enemy.transform.position - summon.transform.position));
                });
            }
            if (attacked && summon.CanAct && summon.LifeId == life) summon.NotifyAttack();
            if (!disposed) contacts.EndStep(stat.interval, ValidEnemy);
        }
        private static bool ValidEnemy(object value, int life)
            => value is Enemy enemy && enemy != null && enemy.CanReceiveHitEffects && enemy.HitEffectLifeId == life;
        public override void Dispose() { disposed = true; contacts.Clear(); }
    }
}
