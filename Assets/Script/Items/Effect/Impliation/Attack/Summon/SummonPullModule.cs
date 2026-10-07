using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonPull", menuName = "GameData/Items/Summon Modules/Pull")]
public sealed class SummonPullModule : SummonBehaviourModule
{
    [Min(0f)] public float pullSpeed = 5f;
    [Min(0.01f)] public float tickInterval = 0.05f;
    [Min(0f)] public float stopDistance = 0.1f;
    public bool holdAtCenter = true;
    public bool allowWhileStopped;
    public bool interruptPattern = true;
    public bool interruptAttack = true;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly Dictionary<Enemy, int> affected = new Dictionary<Enemy, int>();
        private readonly List<Enemy> removed = new List<Enemy>();
        private readonly float speed, interval, stop;
        private readonly bool hold, stopped, interruptPattern, interruptAttack;
        private float timer;
        private bool disposed;
        public Runtime(SummonItemThrower summon, SummonPullModule data) : base(summon)
        {
            speed = EffectStatUtility.Safe(data.pullSpeed, 0f, 1000f, 5f);
            interval = EffectStatUtility.Safe(data.tickInterval, 0.01f, 1f, 0.05f);
            stop = EffectStatUtility.Safe(data.stopDistance, 0f, 100f, 0.1f);
            hold = data.holdAtCenter;
            stopped = data.allowWhileStopped;
            interruptPattern = data.interruptPattern;
            interruptAttack = data.interruptAttack;
        }
        public override void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (disposed || timer > 0f) return;
            timer = interval;
            query.Scan(summon.transform.position, summon.AttackRange, summon.enemyLayerMask);
            foreach (Enemy enemy in query.Enemies)
            {
                if (!summon.CanAct || disposed) break;
                if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
                int life = enemy.HitEffectLifeId;
                ActorMovementControlRequest request = new ActorMovementControlRequest
                {
                    mode = ActorMovementControlMode.PullTowards,
                    speedCurve = ActorMovementControlSpeedCurve.Constant,
                    reapplyMode = ActorMovementControlReapplyMode.Replace,
                    center = summon.transform.position,
                    fallbackDirection = Vector2.right,
                    strength = speed,
                    duration = interval * 2f,
                    maxDistance = summon.AttackRange,
                    pullStopDistance = stop,
                    suppressBaseMovement = true,
                    clearExternalVelocity = true,
                    allowWhileStopped = stopped,
                    holdAtCenter = hold,
                    source = this
                };
                if (enemy.TryApplyMovementControl(request, interruptPattern, interruptAttack))
                {
                    if (disposed)
                    {
                        if (enemy != null && enemy.HitEffectLifeId == life && enemy.mover != null)
                            enemy.mover.CancelMovementControl(this);
                        break;
                    }
                    affected[enemy] = life;
                }
            }
            removed.Clear();
            foreach (KeyValuePair<Enemy, int> pair in affected)
            {
                Enemy enemy = pair.Key;
                if (enemy != null && enemy.HitEffectLifeId == pair.Value && enemy.CanReceiveHitEffects &&
                    query.Enemies.Contains(enemy) && enemy.mover != null && enemy.mover.IsMovementControlledBy(this)) continue;
                Release(enemy, pair.Value);
                removed.Add(enemy);
            }
            foreach (Enemy enemy in removed) affected.Remove(enemy);
        }
        private void Release(Enemy enemy, int life)
        {
            if (enemy != null && enemy.HitEffectLifeId == life && enemy.mover != null)
                enemy.mover.CancelMovementControl(this);
        }
        public override void Dispose()
        {
            disposed = true;
            foreach (KeyValuePair<Enemy, int> pair in affected) Release(pair.Key, pair.Value);
            affected.Clear();
        }
    }
}
