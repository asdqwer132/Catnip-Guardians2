using UnityEngine;

[CreateAssetMenu(fileName = "SummonTurret", menuName = "GameData/Items/Summon Modules/Turret")]
public sealed class SummonTurretModule : SummonBehaviourModule
{
    [Tooltip("RandomPosition은 사용하지 않습니다. 적에게 조준해 자체 탄환을 발사합니다.")]
    public bool randomTarget;
    public SummonProjectile projectilePrefab;
    public Sprite projectileSprite;
    [Min(0f)] public float baseDamage = 10f;
    [Min(0f)] public float attackPowerMultiplier = 1f;
    [Min(0.01f)] public float intervalMultiplier = 1f;
    [Min(0.01f)] public float projectileSpeed = 10f;
    [Min(0.01f)] public float projectileLifetime = 3f;
    [Min(0.01f)] public float projectileRadius = 0.1f;
    public bool homing;
    public HitEffectData[] onHitEffects;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly SummonProjectile prefab;
        private readonly Sprite sprite;
        private readonly float damage, power, multiplier, speed, lifetime, radius;
        private readonly bool random, homing;
        private readonly HitEffectData[] effects;
        private float timer;
        public Runtime(SummonItemThrower summon, SummonTurretModule data) : base(summon)
        {
            prefab = data.projectilePrefab;
            sprite = data.projectileSprite;
            damage = EffectStatUtility.Safe(data.baseDamage, 0f, 1000000f, 10f);
            power = EffectStatUtility.Safe(data.attackPowerMultiplier, 0f, 100f, 1f);
            multiplier = EffectStatUtility.Safe(data.intervalMultiplier, 0.01f, 100f, 1f);
            speed = EffectStatUtility.Safe(data.projectileSpeed, 0.01f, 1000f, 10f);
            lifetime = EffectStatUtility.Safe(data.projectileLifetime, 0.01f, 60f, 3f);
            radius = EffectStatUtility.Safe(data.projectileRadius, 0.01f, 100f, 0.1f);
            random = data.randomTarget;
            homing = data.homing;
            effects = data.onHitEffects != null ? (HitEffectData[])data.onHitEffects.Clone() : null;
        }
        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            if (timer < summon.AttackInterval * multiplier) return;
            timer = 0f;
            query.Scan(summon.transform.position, summon.AttackRange, summon.enemyLayerMask);
            Enemy target = EnemyQueryBuffer.Select(query.Enemies, summon.transform.position, random);
            if (target == null) return;
            Vector3 direction = target.transform.position - summon.transform.position;
            if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
            SummonProjectile projectile = prefab != null ? UnityEngine.Object.Instantiate(prefab, summon.transform.position, Quaternion.identity) :
                new GameObject("SummonProjectile").AddComponent<SummonProjectile>();
            projectile.Init(summon.CreateContext(target.transform.position, direction.normalized), target,
                damage + summon.AttackPower * power, speed, lifetime, radius, homing, summon.enemyLayerMask, effects, sprite);
            if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(projectile);
        }
    }
}
