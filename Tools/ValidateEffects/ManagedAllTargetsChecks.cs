using System;
using System.Collections.Generic;

public static partial class Runner
{
    private sealed class UniversalProbeTarget : IBuffTarget
    {
        public UnityEngine.Object BuffTargetObject { get; set; } = new UnityEngine.Object();
        public string BuffTargetGroup { get; set; }
        public string BuffTargetDebugName => "Universal Probe";
        public void RefreshBuffedStat() { }
    }

    private static ActiveBuff GlobalRecord(BuffStorage storage, ItemData item = null,
        EquipmentBag bag = null, ItemEffectData effect = null, bool includeSelf = true)
        => storage.RegisterBuff(null, new BuffInfo { useLimitType = BuffUseLimitType.Infinite },
            item, bag, effect, BuffTargetHandle.AllTargets(), includeSelf, false);

    private static void RunAllTargetsChecks()
    {
        Check("all target kind preserves every existing serialized value", () => {
            True((int)BuffTargetKind.Item == 0 && (int)BuffTargetKind.Bag == 1 &&
                (int)BuffTargetKind.ItemSeries == 2 && (int)BuffTargetKind.AllItems == 3 &&
                (int)BuffTargetKind.Target == 4 && (int)BuffTargetKind.Group == 5 &&
                (int)BuffTargetKind.AllBags == 6 && (int)BuffTargetKind.AllTargets == 7);
        });
        Check("all target resolver stores one universal handle without enumerating existing objects", () => {
            var resolver = new AllTargetsBuffTargetResolver();
            var results = new List<BuffTargetHandle>();
            resolver.ResolveTargets(null, results);
            True(results.Count == 1 && results[0].kind == BuffTargetKind.AllTargets);
            resolver.ResolveTargets(null, null);
        });
        Check("universal handle matches items, bags and later targets in arbitrary or empty groups", () => {
            var handle = BuffTargetHandle.AllTargets();
            True(handle.Matches(BuffQueryContext.ForItem(new ItemData(), new EquipmentBag())));
            True(handle.Matches(BuffQueryContext.ForBag(new EquipmentBag())));
            foreach (string group in new[] { "DamageArea", "Enemy/Boss", "Custom/NewGroup", "", null })
                True(handle.Matches(BuffQueryContext.ForTarget(new UniversalProbeTarget { BuffTargetGroup = group })));
        });
        Check("universal handle rejects absent and invalid targets", () => {
            var handle = BuffTargetHandle.AllTargets();
            True(!handle.Matches(null) && !handle.Matches(new BuffQueryContext()));
            True(!handle.Matches(BuffQueryContext.ForBag(null)));
            True(!handle.MatchesTarget(new UniversalProbeTarget { BuffTargetObject = null }));
        });
        Check("universal Include Self excludes source item and objects spawned by it", () => {
            var source = new ItemData(); var other = new ItemData(); var target = new UniversalProbeTarget();
            var buff = GlobalRecord(new BuffStorage(), source, includeSelf: false);
            True(!buff.MatchesQuery(BuffQueryContext.ForItem(source, null)));
            True(!buff.MatchesQuery(BuffQueryContext.ForTarget(target, source)));
            True(buff.MatchesQuery(BuffQueryContext.ForTarget(target, other)));
            True(buff.MatchesQuery(BuffQueryContext.ForTarget(target)));
            True(buff.MatchesQuery(BuffQueryContext.ForBag(new EquipmentBag())));
        });
        Check("target lineage does not change existing group Include Self behavior", () => {
            var source = new ItemData(); var target = new UniversalProbeTarget { BuffTargetGroup = "DamageArea" };
            var buff = new ActiveBuff(null, new BuffInfo(), source, null, null,
                BuffTargetHandle.Group("DamageArea"), false, false);
            True(buff.MatchesQuery(BuffQueryContext.ForTarget(target, source)));
        });
        Check("universal selection includes direct item bag group and universal buff records", () => {
            var selection = BuffTargetHandle.AllTargets();
            foreach (var handle in new[] { BuffTargetHandle.Item(new ItemData()), BuffTargetHandle.Bag(new EquipmentBag()),
                BuffTargetHandle.Group("DamageArea"), BuffTargetHandle.Target(new UniversalProbeTarget()),
                BuffTargetHandle.AllItems(), BuffTargetHandle.AllBags(), BuffTargetHandle.AllTargets() })
                True(BuffSelectionUtility.MatchesSelection(handle, selection));
            True(!BuffSelectionUtility.MatchesSelection(null, selection));
        });
        Check("specific selections can find universal buffs that affect their scope", () => {
            var buff = BuffTargetHandle.AllTargets();
            foreach (var selection in new[] { BuffTargetHandle.Item(new ItemData()), BuffTargetHandle.Bag(new EquipmentBag()),
                BuffTargetHandle.Group("New/Group"), BuffTargetHandle.Target(new UniversalProbeTarget()),
                BuffTargetHandle.GetItemSeries(ItemSeries.Weapon), BuffTargetHandle.AllItems(), BuffTargetHandle.AllBags() })
                True(BuffSelectionUtility.MatchesSelection(buff, selection));
            True(!BuffSelectionUtility.MatchesSelection(buff, BuffTargetHandle.GetItemSeries(ItemSeries.None)));
        });
        Check("tracked clone carries application history without mutating the clean base", () => {
            var buff = GlobalRecord(new BuffStorage()); var clean = new DamageAreaAttackStat { damageAreaPower = 5f };
            var snapshot = BuffStatCloneUtility.Clone(clean); snapshot.damageAreaPower *= 1.5f;
            BuffStatCloneUtility.RecordApplied(snapshot, buff);
            var copy = BuffStatCloneUtility.Clone(snapshot);
            True(BuffStatCloneUtility.WasApplied(copy, buff));
            True(!BuffStatCloneUtility.WasApplied(clean, buff)); Eq(copy.damageAreaPower, 7.5f); Eq(clean.damageAreaPower, 5f);
        });
        Check("execution scaling preserves application history", () => {
            var buff = GlobalRecord(new BuffStorage()); var stat = new DamageAreaAttackStat { damageAreaPower = 7.5f };
            BuffStatCloneUtility.RecordApplied(stat, buff); var context = Context(); context.damageMultiplier = 2f;
            var scaled = EffectExecutionScaling.Apply(stat, context);
            True(BuffStatCloneUtility.WasApplied(scaled, buff)); Eq(scaled.damageAreaPower, 15f); Eq(stat.damageAreaPower, 7.5f);
        });
        Check("context cached snapshot copies preserve application history", () => {
            var buff = GlobalRecord(new BuffStorage()); var stat = new DamageAreaAttackStat { damageAreaPower = 7.5f };
            BuffStatCloneUtility.RecordApplied(stat, buff); var context = Context(); var effect = new ItemEffectData();
            True(BuffStatCloneUtility.WasApplied(context.GetUnscaledSnapshotStat(effect, stat), buff));
            True(BuffStatCloneUtility.WasApplied(context.GetSnapshotStat(effect, stat), buff));
            context.buffManager = new BuffManager();
            True(BuffStatCloneUtility.WasApplied(context.GetCurrentStat(effect, stat), buff));
        });
        Check("different universal buff origins still stack independently", () => {
            var storage = new BuffStorage(); var item = new ItemData(); var bag = new EquipmentBag(); var effect = new ItemEffectData();
            var original = GlobalRecord(storage, item, bag, effect); var stat = new DamageAreaAttackStat();
            BuffStatCloneUtility.RecordApplied(stat, original);
            True(!BuffStatCloneUtility.WasApplied(stat, GlobalRecord(storage, new ItemData(), bag, effect)));
            True(!BuffStatCloneUtility.WasApplied(stat, GlobalRecord(storage, item, new EquipmentBag(), effect)));
            True(!BuffStatCloneUtility.WasApplied(stat, GlobalRecord(storage, item, bag, new ItemEffectData())));
            True(!BuffStatCloneUtility.WasApplied(stat, GlobalRecord(new BuffStorage(), item, bag, effect)));
        });
        Check("snapshot keeps same origin captured after removal and reapplication", () => {
            var storage = new BuffStorage(); var item = new ItemData(); var effect = new ItemEffectData();
            var first = GlobalRecord(storage, item, effect: effect); var stat = new DamageAreaAttackStat();
            BuffStatCloneUtility.RecordApplied(stat, first); storage.RemoveBuff(first);
            True(BuffStatCloneUtility.WasApplied(stat, GlobalRecord(storage, item, effect: effect)));
        });
        Check("existing non universal buffs never acquire global deduplication behavior", () => {
            var buff = new ActiveBuff(null, new BuffInfo(), null, null, null, BuffTargetHandle.AllItems(), true, false);
            var stat = new DamageAreaAttackStat(); BuffStatCloneUtility.RecordApplied(stat, buff);
            True(!BuffStatCloneUtility.WasApplied(stat, buff));
        });
    }
}
