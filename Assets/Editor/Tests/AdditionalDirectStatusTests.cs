using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalDirectStatusTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private StatusManager player, previousPlayer;
    private T Asset<T>() where T : ScriptableObject
    { T asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private GameObject Host()
    { var host = new GameObject("Direct Status Test"); owned.Add(host); return host; }
    private ItemEffectContext Context(ItemData item = null)
        => new ItemEffectContext(player.gameObject, item, Vector3.zero, Vector3.right, null, buffManager: manager);
    private ApplyStatusEffect Effect(BuffUseLimitType limit)
    {
        var effect = Asset<ApplyStatusEffect>(); effect.statusKey = Asset<StatusDefinition>();
        effect.statusKey.exposesPlayerStatus = true; effect.statusKey.playerStatus = PlayerStatusList.arrow;
        effect.lifetimeSettings.useLimitType = limit; effect.lifetimeSettings.duration = 5f; effect.lifetimeSettings.maxUseCount = 3;
        return effect;
    }
    private void Tick(float delta) => new BuffTicker(manager.Storage).Tick(delta);
    private void Use(ItemData item) => manager.EndItemUse(manager.BeginItemUse(item, null));

    [SetUp] public void Setup()
    {
        previousManager = BuffManager.instance; previousPlayer = StatusManager.Instance;
        manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null)
            typeof(BuffManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
        player = Host().AddComponent<StatusManager>(); player.buffManager = manager;
        typeof(StatusManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
        manager.RegisterBuffTarget(player);
    }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); BuffManager.instance = previousManager;
        typeof(StatusManager).GetProperty("Instance").SetValue(null, previousPlayer);
    }

    [Test] public void DirectStatusAddsRemainingTimeWithoutModifyingStatusStat()
    {
        var effect = Effect(BuffUseLimitType.Time); var item = Asset<ItemData>();
        effect.Execute(Context(item)); Tick(2f); effect.Execute(Context(item));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].remainTime, Is.EqualTo(8f));
        Assert.That(manager.HasStatus(effect.statusKey, BuffQueryContext.ForTarget(player)), Is.True);
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.True);
        Assert.That(player.currentStat.statArrow, Is.Zero);
        Assert.That(effect.lifetimeSettings.duration, Is.EqualTo(5f));
        Tick(8f);
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.False);
        Assert.That(manager.Storage.activeBuffs, Is.Empty);
    }

    [Test] public void CountedStatusAddsUnusedChargesAndExpiresOnActualItemUses()
    {
        var effect = Effect(BuffUseLimitType.UseCount); var item = Asset<ItemData>();
        effect.Execute(Context(item)); Use(item); effect.Execute(Context(item));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(5));
        for (int i = 0; i < 4; i++) Use(item);
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.True);
        Use(item);
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.False);
        Assert.That(manager.Storage.activeBuffs, Is.Empty);
    }

    [Test] public void SpecificItemsConsumeOnceAndFailedUsesDoNotConsume()
    {
        var effect = Effect(BuffUseLimitType.UseCount); var selected = Asset<ItemData>(); var other = Asset<ItemData>();
        effect.lifetimeSettings.useCountConsumeMode = BuffUseCountConsumeMode.SpecificItemsUsed;
        effect.lifetimeSettings.consumeItems = new[] { selected, selected };
        effect.Execute(Context(other)); Use(other);
        manager.EndItemUse(manager.BeginItemUse(selected, null), false);
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(3));
        Use(selected);
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(2));
    }

    [Test] public void ReapplicationDuringUseDoesNotImmediatelyConsumeTheAddedStatus()
    {
        var effect = Effect(BuffUseLimitType.UseCount); var item = Asset<ItemData>();
        effect.Execute(Context(item)); BuffItemUseToken use = manager.BeginItemUse(item, null);
        effect.Execute(Context(item)); manager.EndItemUse(use);
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(6));
    }

    [Test] public void SameStatusFromDifferentItemsEffectsAndBagsSharesOneBudget()
    {
        var effect = Effect(BuffUseLimitType.Time);
        effect.lifetimeSettings.stackMode = BuffStackMode.Stack; effect.lifetimeSettings.maxStack = 5;
        var otherEffect = Effect(BuffUseLimitType.Time); otherEffect.statusKey = effect.statusKey;
        otherEffect.lifetimeSettings.stackMode = BuffStackMode.Stack; otherEffect.lifetimeSettings.maxStack = 5;
        var first = Context(Asset<ItemData>()); first.sourceBag = Host().AddComponent<EquipmentBag>();
        var second = Context(Asset<ItemData>()); second.sourceBag = Host().AddComponent<EquipmentBag>();
        effect.Execute(first); Tick(2f); otherEffect.Execute(second);
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].remainTime, Is.EqualTo(8f));
        Assert.That(manager.GetStatusStack(effect.statusKey, BuffQueryContext.ForTarget(player)), Is.EqualTo(2));
    }

    [Test] public void DifferentTargetsAndDifferentStatusKeysStaySeparate()
    {
        var effect = Effect(BuffUseLimitType.Time); effect.Execute(Context());
        var other = Host().AddComponent<AdditionalAreaStatusTarget>();
        Assert.That(manager.HasStatus(effect.statusKey, BuffQueryContext.ForTarget(other)), Is.False);
        effect.target = StatusQueryTarget.Owner;
        effect.Execute(new ItemEffectContext(other.gameObject, null, Vector3.zero, Vector3.right, null, buffManager: manager));
        var otherEffect = Effect(BuffUseLimitType.Time); otherEffect.Execute(Context());
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(3));
        var statuses = new List<ActiveBuff>(); player.GetActiveStatuses(statuses);
        Assert.That(statuses.Count, Is.EqualTo(2));
        Assert.That(statuses[0].remainTime, Is.EqualTo(5f));
    }

    [Test] public void DifferentGrantEffectsShareUnusedCharges()
    {
        var first = Effect(BuffUseLimitType.UseCount); var second = Effect(BuffUseLimitType.UseCount);
        second.statusKey = first.statusKey;
        var item = Asset<ItemData>(); first.Execute(Context(item)); Use(item);
        second.Execute(Context(Asset<ItemData>()));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(5));
    }

    [Test] public void DirectStatusRespectsStackCapAndDurationScale()
    {
        var effect = Effect(BuffUseLimitType.Time); var item = Asset<ItemData>();
        effect.lifetimeSettings.stackMode = BuffStackMode.Stack; effect.lifetimeSettings.maxStack = 2;
        for (int i = 0; i < 3; i++)
        { var context = Context(item); context.durationMultiplier = 0.5f; effect.Execute(context); }
        var active = manager.Storage.activeBuffs[0];
        Assert.That(active.stack, Is.EqualTo(2)); Assert.That(active.remainTime, Is.EqualTo(7.5f));
    }

    [Test] public void LegacyStatusStillRecognizesStackedStatBuffs()
    {
        player.baseStat.statArrow = 2f; player.RefreshBuffedStat();
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.True);
        Assert.That(Asset<HasBuffConditionData>().IsSatisfied(Context()), Is.True);
    }

    [Test] public void CleanseUsesDefinitionClassificationAndCancelsCompletion()
    {
        var effect = Effect(BuffUseLimitType.Time); effect.statusKey.harmful = true;
        int completed = 0; var context = Context();
        context.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        effect.Execute(context); context.lifetime.Close();
        Assert.That(context.lifetime.IsFinished, Is.False);
        manager.Cleanse(BuffTargetHandle.Target(player), new BuffCleanseFilter());
        Assert.That(context.lifetime.IsCancelled, Is.True); Assert.That(completed, Is.Zero);
        Assert.That(player.HasStatus(PlayerStatusList.arrow), Is.False);
    }

    [Test] public void NaturalExpiryFinishesTheEffectLifetime()
    {
        var effect = Effect(BuffUseLimitType.Time); int completed = 0; var context = Context();
        context.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        effect.Execute(context); context.lifetime.Close(); Tick(5f);
        Assert.That(context.lifetime.IsFinished, Is.True); Assert.That(completed, Is.EqualTo(1));
    }

    [Test] public void MissingDefinitionOrTargetDoesNotRegisterAnything()
    {
        var effect = Asset<ApplyStatusEffect>(); effect.Execute(Context());
        effect.statusKey = Asset<StatusDefinition>(); effect.target = StatusQueryTarget.HitTarget;
        effect.Execute(Context()); Assert.That(manager.Storage.activeBuffs, Is.Empty);
    }
}
