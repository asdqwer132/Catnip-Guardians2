using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalBuffClassificationTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    private GameObject Host()
    {
        var host = new GameObject("Buff Classification Test"); owned.Add(host); return host;
    }
    private BuffEffect Buff(bool harmful = false, params BuffFlagDefinition[] flags)
    {
        var effect = Asset<BuffEffect>();
        effect.harmful = harmful; effect.flags = flags; effect.includeSelf = true;
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        return effect;
    }
    private ItemEffectContext Context(GameObject owner, ItemData item = null)
        => new ItemEffectContext(owner, item, Vector3.zero, Vector3.right, null, buffManager: manager);
    private ContextStatusTargetResolver Targets(StatusQueryTarget target)
    {
        var resolver = Asset<ContextStatusTargetResolver>(); resolver.targets = new[] { target }; return resolver;
    }
    [SetUp] public void Setup()
    {
        previousManager = BuffManager.instance;
        manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null)
            typeof(BuffManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
    }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); BuffManager.instance = previousManager;
    }

    [Test]
    public void DefaultCleanseRemovesDirectModifierDebuffAndRestoresItemStat()
    {
        ItemData item = Asset<ItemData>();
        ItemEffectContext context = Context(null, item);
        BuffEffect harmful = Buff(true), beneficial = Buff();
        var slow = Asset<FloatFieldBuffModifier>(); slow.fieldName = "cooldown"; slow.addValue = 2f;
        var other = Asset<FloatFieldBuffModifier>(); other.fieldName = "cooldown"; other.addValue = 1f;
        harmful.modifiers = new BuffModifier[] { slow }; beneficial.modifiers = new BuffModifier[] { other };
        harmful.targetResolver = beneficial.targetResolver = Targets(StatusQueryTarget.SourceItem);
        harmful.Execute(context); beneficial.Execute(context);
        var stat = new ItemCooldownStat { cooldown = 5f };
        Assert.That(manager.GetBuffedStatForItem(stat, item, null).cooldown, Is.EqualTo(8f));
        CleanseEffect cleanse = Asset<CleanseEffect>(); cleanse.targetResolver = harmful.targetResolver;
        cleanse.Execute(context);
        Assert.That(manager.GetBuffedStatForItem(stat, item, null).cooldown, Is.EqualTo(6f));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].sourceEffectData, Is.SameAs(beneficial));
    }

    [Test]
    public void FlagOnlyBuffRegistersAndFlagCleanseKeepsUnselectedBuffsAndTargets()
    {
        var selected = Asset<BuffFlagDefinition>(); var other = Asset<BuffFlagDefinition>();
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        var otherTarget = Host().AddComponent<AdditionalAreaStatusTarget>();
        BuffEffect tagged = Buff(false, selected), unselected = Buff(false, other);
        tagged.targetResolver = Targets(StatusQueryTarget.Owner);
        tagged.Execute(Context(target.gameObject));
        ActiveBuff matched = manager.Storage.activeBuffs[0];
        ActiveBuff kept = manager.RegisterBuffForTargetHandle(unselected, Context(target.gameObject), target);
        ActiveBuff keptTarget = manager.RegisterBuffForTargetHandle(tagged, Context(otherTarget.gameObject), otherTarget);
        var cleanse = Asset<CleanseEffect>(); cleanse.targetResolver = tagged.targetResolver;
        cleanse.filter.mode = BuffCleanseMode.Flags; cleanse.filter.flags = new[] { selected };
        cleanse.Execute(Context(target.gameObject));
        //Assert.That(manager.Storage.activeBuffs, Does.Not.Contain(matched));
        Assert.That(manager.Storage.activeBuffs, Is.EquivalentTo(new[] { kept, keptTarget }));
    }

    [TestCase(BuffFlagMatchMode.Any, 2)]
    [TestCase(BuffFlagMatchMode.All, 1)]
    public void MultipleFlagsUseSelectedAnyOrAllRule(BuffFlagMatchMode match, int expectedRemoved)
    {
        var first = Asset<BuffFlagDefinition>(); var second = Asset<BuffFlagDefinition>();
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject);
        manager.RegisterBuffForTargetHandle(Buff(false, first), context, target);
        manager.RegisterBuffForTargetHandle(Buff(false, first, second), context, target);
        var filter = new BuffCleanseFilter { mode = BuffCleanseMode.Flags, flagMatch = match, flags = new[] { first, second } };
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target), filter), Is.EqualTo(expectedRemoved));
    }

    [Test]
    public void EmptyAllFlagConditionCannotClearEveryBuff()
    {
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        var active = manager.RegisterBuffForTargetHandle(Buff(true), Context(target.gameObject), target);
        var filter = new BuffCleanseFilter { mode = BuffCleanseMode.Flags, flagMatch = BuffFlagMatchMode.All,
            flags = new BuffFlagDefinition[] { null } };
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target), filter), Is.Zero);
        Assert.That(manager.Storage.activeBuffs, Does.Contain(active));
    }

    [Test]
    public void EitherStatusOrEffectCanForbidFlagCleanse()
    {
        var flag = Asset<BuffFlagDefinition>();
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        var status = Asset<StatusDefinition>(); status.harmful = true; status.dispellable = false;
        BuffEffect statusLocked = Buff(false, flag), effectLocked = Buff(true, flag);
        statusLocked.buffInfo.statusDefinition = status; effectLocked.dispellable = false;
        manager.RegisterBuffForTargetHandle(statusLocked, Context(target.gameObject), target);
        manager.RegisterBuffForTargetHandle(effectLocked, Context(target.gameObject), target);
        var filter = new BuffCleanseFilter { mode = BuffCleanseMode.HarmfulOrFlags, flags = new[] { flag } };
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target), filter), Is.Zero);
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(2));
    }

    [Test]
    public void IncludeModifierBuffsOptionStillProtectsStatModifiers()
    {
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        BuffEffect effect = Buff(true); effect.modifiers = new BuffModifier[] { Asset<FloatFieldBuffModifier>() };
        var active = manager.RegisterBuffForTargetHandle(effect, Context(target.gameObject), target);
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target), includeModifierBuffs: false), Is.Zero);
        Assert.That(manager.Storage.activeBuffs, Does.Contain(active));
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target)), Is.EqualTo(1));
    }

    [Test]
    public void ClassificationIsCopiedAndRefreshedWithoutDuplicatingTheBuff()
    {
        var first = Asset<BuffFlagDefinition>(); var second = Asset<BuffFlagDefinition>();
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        BuffEffect effect = Buff(true, first);
        var active = manager.RegisterBuffForTargetHandle(effect, Context(target.gameObject), target);
        effect.harmful = false; effect.flags[0] = second;
        Assert.That(active.harmful, Is.True); Assert.That(active.HasFlag(first), Is.True);
        Assert.That(manager.RegisterBuffForTargetHandle(effect, Context(target.gameObject), target), Is.SameAs(active));
        Assert.That(active.harmful, Is.False); Assert.That(active.HasFlag(second), Is.True);
        Assert.That(active.HasFlag(first), Is.False); Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
    }

    [Test]
    public void CleanseCancelsCompletionAndReportsCleansedRemoval()
    {
        var target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject); int completions = 0;
        context.lifetime = new ItemEffectLifetime(onCompleted: () => completions++);
        var active = manager.RegisterBuffForTargetHandle(Buff(true), context, target);
        BuffRemovalReason reason = BuffRemovalReason.Cancelled;
        active.Removed += (buff, why) => reason = why;
        context.lifetime.Close();
        Assert.That(context.lifetime.IsFinished, Is.False);
        manager.Cleanse(BuffTargetHandle.Target(target));
        Assert.That(reason, Is.EqualTo(BuffRemovalReason.Cleansed));
        Assert.That(context.lifetime.IsFinished, Is.True); Assert.That(context.lifetime.IsCancelled, Is.True);
        Assert.That(completions, Is.Zero);
    }
}
