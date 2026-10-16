using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonGroupSustainedSpawn", menuName = "04.Items/Effects/Direct Effects/Summon/Group Reactions/Sustained Spawn")]
public sealed class SummonGroupSustainedSpawnModule : SummonGroupReactionModule
{
    [Tooltip("여러 종류를 연결할 수 있습니다. 각 효과의 버프 적용된 소환 수/수명/배치를 그대로 사용합니다.")]
    public SummonSpawnEffect[] spawnEffects;
    public SummonGroupSpawnPosition position = new SummonGroupSpawnPosition();
    [Tooltip("기본은 조건이 깨지는 즉시 이 모듈이 만든 소환물과 하위 실행을 회수합니다.")]
    public bool despawnWhenConditionLost = true;
    [Tooltip("각 효과가 만든 소환물이 모두 사라지면 조건이 유지되는 동안 그 묶음을 다시 소환합니다.")]
    public bool respawnWhenAllGone = true;
    [Min(0.1f)] public float retryInterval = 0.25f;
    public override SummonGroupReactionRuntime CreateRuntime() => new Runtime(this);

    private sealed class Runtime : SummonGroupReactionRuntime
    {
        private struct Member { public SummonActor actor; public int life; }
        private sealed class Batch
        {
            public SummonSpawnEffect effect;
            public bool spawnedBefore;
            public readonly List<Member> members = new List<Member>();
        }
        private readonly SummonGroupSustainedSpawnModule module;
        private readonly List<Batch> batches = new List<Batch>();
        private SummonGroupReactionContext context;
        private ItemEffectLifetime lifetime;
        private bool active, disposed;
        private float retry;
        public Runtime(SummonGroupSustainedSpawnModule module) { this.module = module; }

        public override void Refresh(SummonGroupReactionContext value)
        {
            if (disposed) return;
            context = value;
            bool wanted = value != null && value.source != null && module.condition != null && module.condition.Matches(value.population);
            if (!wanted) { Stop(false); return; }
            if (active) return;
            active = true;
            lifetime = new ItemEffectLifetime(trackCompletion: false);
            var seen = new HashSet<SummonSpawnEffect>();
            foreach (var effect in module.spawnEffects ?? new SummonSpawnEffect[0])
                if (effect != null && seen.Add(effect)) batches.Add(new Batch { effect = effect });
            Replenish();
        }
        public override void Tick(float deltaTime)
        {
            if (!active || disposed) return;
            retry -= EffectStatUtility.Safe(deltaTime, 0f, 60f, 0f);
            if (retry <= 0f) Replenish();
        }
        private void Replenish()
        {
            retry = EffectStatUtility.Safe(module.retryInterval, 0.1f, 600f, 0.25f);
            ItemEffectLifetime current = lifetime;
            // 소환 중 조건 변경으로 Stop()이 들어와도 반복 목록을 변경하지 않습니다.
            foreach (Batch batch in batches.ToArray())
            {
                if (!active || disposed || current != lifetime || current.IsCancelled) break;
                batch.members.RemoveAll(member => !IsCurrent(member));
                if (batch.members.Count > 0 || (batch.spawnedBefore && !module.respawnWhenAllGone)) continue;
                if (module.position == null || !module.position.TryResolve(context, out Vector3 point)) continue;
                ItemEffectContext execution = context.CreateExecution(point, current);
                execution.onSummonSpawned = actor =>
                {
                    if (actor == null) return;
                    if (current.IsCancelled) { actor.Despawn(false); return; }
                    // 회수하지 않는 옵션의 이전 묶음은 자연 수명까지 살아 있으며 새 묶음과 섞지 않습니다.
                    if (!active || disposed || current != lifetime) return;
                    batch.members.Add(new Member { actor = actor, life = actor.LifeId });
                };
                // 기존 실행 경로로 조건/연출/순환 방지/생성 스탯 버프를 모두 적용합니다.
                batch.effect.TryExecute(execution);
                batch.spawnedBefore |= batch.members.Count > 0;
            }
        }
        private static bool IsCurrent(Member member)
            => member.actor != null && member.actor.LifeId == member.life && member.actor.CanAct;
        private void Stop(bool force)
        {
            if (!active) return;
            active = false;
            ItemEffectLifetime ending = lifetime;
            lifetime = null;
            Batch[] removed = batches.ToArray();
            batches.Clear();
            bool recall = force || module.despawnWhenConditionLost;
            ending.Close(!recall);
            if (!recall) return;
            foreach (Batch batch in removed)
                foreach (Member member in batch.members.ToArray())
                    // LifeId로 이미 풀에서 재사용된 다른 소환물을 회수하지 않도록 합니다.
                    if (member.actor != null && member.actor.LifeId == member.life) member.actor.Despawn(false);
        }
        public override void Dispose() { if (disposed) return; disposed = true; Stop(true); }
    }
}
