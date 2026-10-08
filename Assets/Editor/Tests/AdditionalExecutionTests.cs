using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalExecutionTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject
    { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private ItemEffectContext Context() => new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null);
    [TearDown] public void Cleanup()
    { foreach (Object value in owned) if (value != null) Object.DestroyImmediate(value); owned.Clear(); }

    [Test] public void ReadySlotDoesNotRestartPreparationOnNextUse()
    {
        ItemData item = Asset<ItemData>(); item.cooldown = 10f;
        BagItemCooldownController controller = new BagItemCooldownController(); controller.Init(1);
        controller.MakeSlotReady(0);
        controller.StartPreparationCooldownIfNeeded(0, item);
        Assert.That(controller.GetSlotCooldownRemain(0), Is.Zero);
    }
    [TestCase(10f, CooldownOperation.ReduceSeconds, 3f, 7f)]
    [TestCase(10f, CooldownOperation.ReduceFraction, 0.3f, 7f)]
    [TestCase(10f, CooldownOperation.Ready, 0f, 0f)]
    [TestCase(10f, CooldownOperation.ReduceSeconds, 30f, 0f)]
    public void CooldownOperations(float remaining, CooldownOperation operation, float amount, float expected)
    { Assert.That(BagItemCooldownController.ChangeRemaining(remaining, operation, amount), Is.EqualTo(expected).Within(0.0001f)); }

    [Test] public void ExecutionScaleDoesNotMutateAssetOrCachedSnapshot()
    {
        DamageAreaAttackEffect effect = Asset<DamageAreaAttackEffect>();
        DamageAreaAttackStat stat = new DamageAreaAttackStat { damageAreaPower = 10f, damageAreaRange = 4f };
        ItemEffectContext context = Context(); context.damageMultiplier = 0.5f; context.rangeMultiplier = 0.5f;
        DamageAreaAttackStat first = context.GetSnapshotStat(effect, stat);
        DamageAreaAttackStat second = context.GetSnapshotStat(effect, stat);
        Assert.That(first.damageAreaPower, Is.EqualTo(5f));
        Assert.That(second.damageAreaPower, Is.EqualTo(5f));
        Assert.That(stat.damageAreaPower, Is.EqualTo(10f));
        Assert.That(first.damageAreaRange, Is.EqualTo(2f));
        Assert.That(first.damageAreaInterval, Is.EqualTo(stat.damageAreaInterval));
    }
    [Test] public void WeightedSelectionChoosesExactlyOneAtSeventyPercentBoundary()
    {
        WeightedRandomEffect random = Asset<WeightedRandomEffect>();
        CombatEffectRecordingEffect strong = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect weak = Asset<CombatEffectRecordingEffect>();
        random.entries = new[] {
            new WeightedEffectEntry { weight = 70f, effects = new ItemEffectData[] { strong } },
            new WeightedEffectEntry { weight = 30f, effects = new ItemEffectData[] { weak } }
        };
        Assert.That(random.SelectEntries(0.699f)[0].effects[0], Is.SameAs(strong));
        Assert.That(random.SelectEntries(0.701f)[0].effects[0], Is.SameAs(weak));
        Assert.That(random.SelectEntries(0.5f).Count, Is.EqualTo(1));
    }
    [Test] public void RandomBoxSelectsFiveBeforeExecutingInSameCall()
    {
        WeightedRandomEffect random = Asset<WeightedRandomEffect>();
        CombatEffectRecordingEffect recording = Asset<CombatEffectRecordingEffect>();
        random.entries = new[] { new WeightedEffectEntry { effects = new ItemEffectData[] { recording } } };
        random.selectionCount = 5;
        random.Execute(Context());
        Assert.That(recording.calls.Count, Is.EqualTo(5));
    }
    [Test] public void CancellationReachesChildScope()
    {
        ItemEffectLifetime parent = new ItemEffectLifetime();
        ItemEffectLifetime child = new ItemEffectLifetime(parent);
        parent.Close(false);
        Assert.That(child.IsCancelled, Is.True);
        child.Close();
    }
    [Test] public void ChildContextRetainsHitScopeAndScale()
    {
        ItemEffectContext parent = Context(); parent.damageMultiplier = 0.5f;
        ItemEffectContext child = parent.Copy(Vector3.right, Vector3.up);
        Assert.That(child.hitUseState, Is.SameAs(parent.hitUseState));
        Assert.That(child.damageMultiplier, Is.EqualTo(0.5f));
    }
    [Test] public void ThenScopeSkipsFollowupWhenNestedChildIsCancelled()
    {
        int completions = 0;
        ItemEffectLifetime sequence = new ItemEffectLifetime(onCompleted: () => completions++, cancelOnChildFailure: true);
        ItemEffectLifetime child = new ItemEffectLifetime(sequence);
        ItemEffectLifetime grandchild = new ItemEffectLifetime(child);
        sequence.Close(); child.Close(); grandchild.Close(false);
        Assert.That(sequence.IsCancelled, Is.True);
        Assert.That(completions, Is.Zero);
    }

    private BuffManager Manager()
    {
        GameObject host = new GameObject("AdditionalExecutionManager"); owned.Add(host);
        BuffManager manager = host.AddComponent<BuffManager>();
        if (manager.Storage == null)
            typeof(BuffManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).Invoke(manager, null);
        return manager;
    }

    [Test] public void DynamicDamageBuffIsScaledAfterItsAddition()
    {
        BuffManager manager = Manager();
        ItemData item = Asset<ItemData>();
        ItemEffectContext context = Context(); context.sourceItemData = item; context.buffManager = manager;
        BuffEffect buff = Asset<BuffEffect>(); buff.includeSelf = true;
        buff.targetResolver = Asset<AdditionalAllItemsResolver>();
        buff.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        buff.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.fieldName = "damageAreaPower"; modifier.addValue = 10f;
        buff.modifiers = new BuffModifier[] { modifier }; buff.Execute(context);
        context.damageMultiplier = 0.5f;
        DamageAreaAttackStat actual = context.GetCurrentStat(Asset<DamageAreaAttackEffect>(),
            new DamageAreaAttackStat { damageAreaPower = 10f });
        Assert.That(actual.damageAreaPower, Is.EqualTo(10f));
    }

    [Test] public void ChildWithoutUseConsumptionDoesNotChargeParentAppliedBuff()
    {
        BuffManager manager = Manager();
        ItemData item = Asset<ItemData>();
        ItemEffectContext context = Context(); context.sourceItemData = item; context.buffManager = manager;
        BuffEffect buff = Asset<BuffEffect>(); buff.includeSelf = true;
        buff.targetResolver = Asset<AdditionalAllItemsResolver>();
        buff.buffInfo.useLimitType = BuffUseLimitType.UseCount;
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.fieldName = "damageAreaPower"; modifier.addValue = 10f;
        buff.modifiers = new BuffModifier[] { modifier }; buff.Execute(context);
        BuffItemUseToken parent = manager.BeginItemUse(item, null);
        context.consumeUseBuffs = false;
        context.GetSnapshotStat(Asset<DamageAreaAttackEffect>(), new DamageAreaAttackStat());
        manager.EndItemUse(parent);
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(1));
    }
}

public sealed class AdditionalAllItemsResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
        => results.Add(BuffTargetHandle.AllItems());
}
