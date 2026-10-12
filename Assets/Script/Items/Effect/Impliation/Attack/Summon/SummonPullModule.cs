using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonPull", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Pull")]
public sealed class SummonPullModule : SummonBehaviourModule
{
    [Header("기본 스탯 · 끌어당김")]
    public SummonPullStat baseStat = new SummonPullStat();
    public override System.Type StatType => typeof(SummonPullStat);
    public bool holdAtCenter = true;
    public bool allowWhileStopped;
    public bool interruptPattern = true;
    public bool interruptAttack = true;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonModuleRuntime<SummonPullStat>
    {
        private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
        private readonly Dictionary<Enemy, int> affected = new Dictionary<Enemy, int>();
        private readonly List<Enemy> removed = new List<Enemy>();
        private readonly bool hold, stopped, interruptPattern, interruptAttack;
        private float timer;
        private bool disposed;
        public Runtime(SummonActor summon, SummonPullModule data) : base(summon, data, data.baseStat)
        {
            hold = data.holdAtCenter;
            stopped = data.allowWhileStopped;
            interruptPattern = data.interruptPattern;
            interruptAttack = data.interruptAttack;
        }
        public override void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (disposed || timer > 0f) return;
            timer = stat.tickInterval;
            query.Scan(summon.transform.position, stat.range, summon.enemyLayerMask);
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
                    strength = stat.speed,
                    duration = stat.tickInterval * 2f,
                    maxDistance = stat.range,
                    pullStopDistance = stat.stopDistance,
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
