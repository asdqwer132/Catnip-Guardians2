using UnityEngine;

[CreateAssetMenu(fileName = "SummonTurret", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Turret")]
public sealed class SummonTurretModule : SummonBehaviourModule
{
    [Tooltip("RandomPosition은 사용하지 않습니다. 적에게 조준해 자체 탄환을 발사합니다.")]
    public bool randomTarget;
    [Header("기본 스탯 프로필 · 포탑")]
    public SummonTurretStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    public SummonProjectile projectilePrefab;
    public Sprite projectileSprite;
    public bool homing;
    public HitEffectData[] onHitEffects;
    public override void Prepare(ItemEffectContext context) => HitEffectPreparation.Prepare(onHitEffects, context);
    public override SummonBehaviourRuntime CreateRuntime(SummonActor summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonModuleRuntime<SummonTurretStat>
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly SummonProjectile prefab;
        private readonly Sprite sprite;
        private readonly bool random, homing;
        private readonly HitEffectData[] effects;
        private float timer;
        public Runtime(SummonActor summon, SummonTurretModule data) : base(summon, data, data.stats)
        {
            prefab = data.projectilePrefab;
            sprite = data.projectileSprite;
            random = data.randomTarget;
            homing = data.homing;
            effects = data.onHitEffects != null ? (HitEffectData[])data.onHitEffects.Clone() : null;
        }
        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            if (timer < stat.interval) return;
            timer = 0f;
            query.Scan(summon.transform.position, stat.range, summon.enemyLayerMask);
            Enemy target = EnemyQueryBuffer.Select(query.Enemies, summon.transform.position, random);
            if (target == null) return;
            Vector3 direction = target.transform.position - summon.transform.position;
            if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
            int life = summon.LifeId, profile = summon.ProfileVersion;
            SummonTurretStat shot = CaptureActivationStat();
            if (!summon.CanAct || summon.LifeId != life || summon.ProfileVersion != profile) return;
            SummonProjectile projectile = prefab != null ? UnityEngine.Object.Instantiate(prefab, summon.transform.position, Quaternion.identity) :
                new GameObject("SummonProjectile").AddComponent<SummonProjectile>();
            projectile.Init(summon.CreateAttackContext(target.transform.position, direction.normalized), target,
                summon.CalculateDamage(shot.damage), shot.projectileSpeed, shot.projectileLifetime, shot.projectileRadius, homing, summon.enemyLayerMask, effects, sprite);
            if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(projectile);
            summon.NotifyAttack();
        }
    }
}
