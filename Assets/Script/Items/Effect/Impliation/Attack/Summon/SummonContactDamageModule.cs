using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonContactDamage", menuName = "GameData/Items/Summon Modules/Contact Damage")]
public sealed class SummonContactDamageModule : SummonBehaviourModule
{
    [Min(0.01f)] public float contactRadius = 0.3f;
    [Min(0f)] public float baseDamage = 10f;
    [Min(0f)] public float attackPowerMultiplier = 1f;
    [Min(0.01f)] public float hitInterval = 0.5f;
    public HitEffectData[] onHitEffects;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private struct HitState { public int life; public float next; }
        private readonly Dictionary<Enemy, HitState> hitTimes = new Dictionary<Enemy, HitState>();
        private readonly List<Enemy> removed = new List<Enemy>();
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly float radius, damage, power, interval;
        private readonly HitEffectData[] effects;
        private float elapsed;
        public Runtime(SummonItemThrower summon, SummonContactDamageModule data) : base(summon)
        {
            radius = EffectStatUtility.Safe(data.contactRadius, 0.01f, 100f, 0.3f);
            damage = EffectStatUtility.Safe(data.baseDamage, 0f, 1000000f, 10f);
            power = EffectStatUtility.Safe(data.attackPowerMultiplier, 0f, 100f, 1f);
            interval = EffectStatUtility.Safe(data.hitInterval, 0.01f, 60f, 0.5f);
            effects = data.onHitEffects != null ? (HitEffectData[])data.onHitEffects.Clone() : null;
        }
        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            query.Scan(summon.transform.position, radius, summon.enemyLayerMask);
            foreach (Enemy enemy in query.Enemies)
            {
                if (!summon.CanAct) break;
                if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
                HitState previous;
                if (hitTimes.TryGetValue(enemy, out previous) && previous.life == enemy.HitEffectLifeId && elapsed < previous.next)
                    continue;
                hitTimes[enemy] = new HitState { life = enemy.HitEffectLifeId, next = elapsed + interval };
                SummonDamageUtility.Hit(enemy, damage + summon.AttackPower * power, effects,
                    summon.CreateContext(enemy.transform.position, enemy.transform.position - summon.transform.position));
            }
            // 밖으로 나갔다 들어와도 interval 안에는 중복 피해를 주지 않는다.
            removed.Clear();
            foreach (KeyValuePair<Enemy, HitState> pair in hitTimes)
                if (pair.Key == null || (!query.Enemies.Contains(pair.Key) && elapsed >= pair.Value.next)) removed.Add(pair.Key);
            foreach (Enemy enemy in removed) hitTimes.Remove(enemy);
        }
        public override void Dispose() => hitTimes.Clear();
    }
}
