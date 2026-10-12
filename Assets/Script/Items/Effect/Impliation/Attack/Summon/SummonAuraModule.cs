using System.Collections.Generic;
using System;
using UnityEngine;

// 오라가 실제로 사용하는 필터만 노출합니다. 반경/기준 위치는 소환물 스탯과 위치를 사용합니다.
[Serializable]
public sealed class SummonAuraTargetFilter
{
    public SummonOwnerScope ownerScope = SummonOwnerScope.SameOwner;
    public SummonDefinition[] definitions;
    public string[] requiredTags;
}

[CreateAssetMenu(fileName = "SummonAura", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Aura")]
public sealed class SummonAuraModule : SummonBehaviourModule
{
    public SummonAuraTargetFilter targets = new SummonAuraTargetFilter();
    public BuffEffect buff;
    public bool includeEmitter;
    [Header("기본 스탯 프로필 · 오라")]
    public SummonAuraStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonModuleRuntime<SummonAuraStat>
    {
        private struct Entry { public int life; public ActiveBuff buff; }
        private readonly Dictionary<SummonActor, Entry> applied = new Dictionary<SummonActor, Entry>();
        private readonly List<SummonActor> matches = new List<SummonActor>();
        private readonly List<SummonActor> removed = new List<SummonActor>();
        private readonly SummonSelection selection;
        private readonly BuffEffect effect;
        private readonly ItemEffectContext context;
        private readonly BuffManager manager;
        private readonly bool includeEmitter;
        private float timer;

        public Runtime(SummonActor summon, SummonAuraModule data) : base(summon, data, data.stats)
        {
            SummonAuraTargetFilter configured = data.targets ?? new SummonAuraTargetFilter();
            selection = new SummonSelection
            {
                ownerScope = configured.ownerScope,
                definitions = configured.definitions != null ? (SummonDefinition[])configured.definitions.Clone() : null,
                requiredTags = configured.requiredTags != null ? (string[])configured.requiredTags.Clone() : null,
                limitRadius = true,
                maximumTargets = Mathf.Clamp(Mathf.FloorToInt(stat.maximumTargets), 1, 512),
                origin = SummonQueryOrigin.TargetPosition
            };
            context = summon.CreateContext(summon.transform.position, Vector3.right);
            manager = context.buffManager;
            includeEmitter = data.includeEmitter;
            timer = stat.scanInterval;
            if (data.buff != null)
            {
                // 각 안테나가 독립된 등록 키를 가져야 한 안테나의 제거가 다른 강화에 영향을 주지 않는다.
                effect = UnityEngine.Object.Instantiate(data.buff);
                effect.hideFlags = HideFlags.HideAndDontSave;
                effect.buffInfo = data.buff.buffInfo != null ? data.buff.buffInfo.Clone() : new BuffInfo();
                effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
                effect.endVisualData = null;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (effect == null || manager == null || !summon.CanAct || !context.CanContinue) return;
            timer += deltaTime;
            if (timer < stat.scanInterval) return;
            timer = 0f;
            context.targetPosition = summon.transform.position;
            selection.radius = stat.range;
            selection.maximumTargets = Mathf.Clamp(Mathf.FloorToInt(stat.maximumTargets), 1, 512);
            SummonRegistry.Collect(selection, context, matches);
            if (!includeEmitter) matches.Remove(summon);
            removed.Clear();
            foreach (KeyValuePair<SummonActor, Entry> pair in applied)
                if (pair.Key == null || pair.Key.LifeId != pair.Value.life || !matches.Contains(pair.Key) ||
                    pair.Value.buff == null || pair.Value.buff.StorageOwner != manager.Storage) removed.Add(pair.Key);
            foreach (SummonActor target in removed)
            {
                manager.RemoveBuffHandle(applied[target].buff);
                applied.Remove(target);
            }
            foreach (SummonActor target in matches)
            {
                if (!summon.CanAct || !context.CanContinue) break;
                if (applied.ContainsKey(target)) continue;
                // 범위 이탈로 이 버프가 취소되어도 안테나의 원래 실행을 취소하지 않는다.
                ItemEffectContext targetContext = context.Copy(target.transform.position, context.direction);
                ItemEffectLifetime scope = new ItemEffectLifetime(context.lifetime,
                    trackCompletion: context.lifetime != null && context.lifetime.TracksCompletion);
                targetContext.lifetime = scope;
                ActiveBuff active;
                try { active = manager.RegisterBuffForTargetHandle(effect, targetContext, target); }
                finally { scope.Close(); }
                if (active != null) applied.Add(target, new Entry { life = target.LifeId, buff = active });
            }
        }

        public override void Dispose()
        {
            if (manager != null)
                foreach (Entry entry in applied.Values) manager.RemoveBuffHandle(entry.buff);
            applied.Clear();
            if (effect != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(effect);
                else UnityEngine.Object.DestroyImmediate(effect);
            }
        }
    }
}
