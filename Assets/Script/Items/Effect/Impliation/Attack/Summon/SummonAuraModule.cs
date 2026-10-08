using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonAura", menuName = "GameData/Items/Summon Modules/Aura")]
public sealed class SummonAuraModule : SummonBehaviourModule
{
    public SummonSelection targets = new SummonSelection();
    public BuffEffect buff;
    public bool includeEmitter;
    [Min(0.01f)] public float radiusMultiplier = 1f;
    [Min(0.01f)] public float scanInterval = 0.1f;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private struct Entry { public int life; public ActiveBuff buff; }
        private readonly Dictionary<SummonItemThrower, Entry> applied = new Dictionary<SummonItemThrower, Entry>();
        private readonly List<SummonItemThrower> matches = new List<SummonItemThrower>();
        private readonly List<SummonItemThrower> removed = new List<SummonItemThrower>();
        private readonly SummonSelection selection;
        private readonly BuffEffect effect;
        private readonly ItemEffectContext context;
        private readonly BuffManager manager;
        private readonly bool includeEmitter;
        private readonly float radiusMultiplier, interval;
        private float timer;

        public Runtime(SummonItemThrower summon, SummonAuraModule data) : base(summon)
        {
            SummonSelection configured = data.targets ?? new SummonSelection();
            selection = new SummonSelection
            {
                ownerScope = configured.ownerScope,
                definitions = configured.definitions != null ? (SummonDefinition[])configured.definitions.Clone() : null,
                requiredTags = configured.requiredTags != null ? (string[])configured.requiredTags.Clone() : null,
                limitRadius = true,
                maximumTargets = Mathf.Clamp(configured.maximumTargets, 1, 512),
                origin = SummonQueryOrigin.TargetPosition
            };
            context = summon.CreateContext(summon.transform.position, Vector3.right);
            manager = context.buffManager;
            includeEmitter = data.includeEmitter;
            radiusMultiplier = EffectStatUtility.Safe(data.radiusMultiplier, 0.01f, 100f, 1f);
            interval = EffectStatUtility.Safe(data.scanInterval, 0.01f, 10f, 0.1f);
            timer = interval;
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
            if (timer < interval) return;
            timer = 0f;
            context.targetPosition = summon.transform.position;
            selection.radius = summon.AttackRange * radiusMultiplier;
            SummonRegistry.Collect(selection, context, matches);
            if (!includeEmitter) matches.Remove(summon);
            removed.Clear();
            foreach (KeyValuePair<SummonItemThrower, Entry> pair in applied)
                if (pair.Key == null || pair.Key.LifeId != pair.Value.life || !matches.Contains(pair.Key) ||
                    pair.Value.buff == null || pair.Value.buff.StorageOwner != manager.Storage) removed.Add(pair.Key);
            foreach (SummonItemThrower target in removed)
            {
                manager.RemoveBuffHandle(applied[target].buff);
                applied.Remove(target);
            }
            foreach (SummonItemThrower target in matches)
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
