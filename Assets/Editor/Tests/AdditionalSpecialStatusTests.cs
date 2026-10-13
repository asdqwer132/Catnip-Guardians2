using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalSpecialStatusTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private StatusManager player, previousPlayer;
    private SpecialItemManager special, previousSpecial;
    private ItemThrowMover previousMover;
    private ItemData source;
    private CombatEffectRecordingEffect specialEffect;
    private SpecialStatusDefinition key;
    private ItemData bonusItem;
    private UseItemStatusReactionModule module;

    private T Asset<T>() where T : ScriptableObject
    { T asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private GameObject Host()
    { var host = new GameObject("Special Status Test"); owned.Add(host); return host; }
    private ItemEffectContext Context(ItemData item = null)
        => new ItemEffectContext(player.gameObject, item ?? source, Vector3.zero, Vector3.right, null, buffManager: manager);
    private GrantSpecialStatusEffect Grant(BuffUseLimitType limit = BuffUseLimitType.Time)
    {
        var effect = Asset<GrantSpecialStatusEffect>(); effect.specialStatusKey = key;
        effect.lifetimeSettings.useLimitType = limit;
        return effect;
    }
    private void OwnCreatedMover()
    { if (ThrowMoverTestProbe.LastCreated != null) owned.Add(ThrowMoverTestProbe.LastCreated.gameObject); }

    [SetUp] public void Setup()
    {
        previousManager = BuffManager.instance; previousPlayer = StatusManager.Instance;
        previousSpecial = SpecialItemManager.Instance; previousMover = ThrowMoverTestProbe.LastCreated;
        manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null)
            typeof(BuffManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        player = Host().AddComponent<StatusManager>(); player.buffManager = manager;
        typeof(StatusManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
        manager.RegisterBuffTarget(player);
        special = Host().AddComponent<SpecialItemManager>(); SpecialItemManager.Instance = special;
        key = Asset<SpecialStatusDefinition>();
        bonusItem = Asset<ItemData>(); bonusItem.dataId = "SpecialTestItem";
        module = Asset<UseItemStatusReactionModule>(); module.itemSource = StatusReactionItemSource.ConfiguredItem;
        module.configuredItem = bonusItem; module.delivery = StatusReactionItemDelivery.ThrowAtImpact;
        module.consumeUseBuffs = true;
        module.excludedItems = new[] { bonusItem }; module.excludedItemIds = new[] { "WeaponExtra1" };
        key.onItemUsedModules = new SpecialStatusItemUseModule[] { module };
        specialEffect = Asset<CombatEffectRecordingEffect>();
        bonusItem.effectDatas = new ItemEffectData[] { specialEffect };
        source = Asset<ItemData>(); source.dataId = "NormalTestItem";
        source.effectDatas = new ItemEffectData[] { Asset<CombatEffectRecordingEffect>() };
        var executor = Host().AddComponent<ItemEffectExecutor>(); executor.buffManager = manager;
        special.ItemThrowExecutor = Host().AddComponent<ItemThrowExecutor>();
        special.ItemThrowExecutor.itemEffectExecutor = executor; special.ItemThrowExecutor.showTargetRange = false;
        var prefab = Host().AddComponent<ItemThrowMover>(); prefab.destroyOnArrive = false;
        prefab.gameObject.AddComponent<ThrowMoverTestProbe>();
        special.ItemThrowExecutor.throwMoverPrefab = prefab;
        ThrowMoverTestProbe.LastCreated = null;
    }

    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); BuffManager.instance = previousManager; SpecialItemManager.Instance = previousSpecial;
        typeof(StatusManager).GetProperty("Instance").SetValue(null, previousPlayer);
        ThrowMoverTestProbe.LastCreated = previousMover;
    }

    [Test] public void NumericStarDoesNotTriggerButTheRequiredKeyDoesWithoutLegacyMapping()
    {
        player.baseStat.statStar = 5f; player.RefreshBuffedStat();
        special.Call(Context()); Assert.That(ThrowMoverTestProbe.LastCreated, Is.Null);
        Assert.That(special.CanTrigger(Context()), Is.False);
        player.baseStat.statStar = 0f; player.RefreshBuffedStat();
        Grant().Execute(Context());
        Assert.That(key.exposesPlayerStatus, Is.False);
        Assert.That(player.currentStat.statStar, Is.Zero);
        Assert.That(player.HasSpecialStatus(key), Is.True);
        special.Call(Context()); OwnCreatedMover();
        Assert.That(ThrowMoverTestProbe.LastCreated, Is.Not.Null);
        ThrowMoverTestProbe.LastCreated.Tick(ThrowMoverTestProbe.LastCreated.MoveDuration);
        Assert.That(specialEffect.calls.Count, Is.EqualTo(1));
    }

    [Test] public void LastCountedUseStillTriggersThenTheStatusExpires()
    {
        var grant = Grant(BuffUseLimitType.UseCount);
        grant.lifetimeSettings.maxUseCount = 1;
        grant.lifetimeSettings.useCountConsumeMode = BuffUseCountConsumeMode.SpecificItemsUsed;
        grant.lifetimeSettings.consumeItems = new[] { source };
        grant.Execute(Context());
        ItemEffectExecutor.ExecuteItem(source, Vector3.zero, Vector3.right, Vector3.right,
            player.gameObject, null, manager, triggerSpecialItems: true);
        OwnCreatedMover();
        Assert.That(ThrowMoverTestProbe.LastCreated, Is.Not.Null);
        Assert.That(player.HasSpecialStatus(key), Is.False);
        ThrowMoverTestProbe.LastCreated.Tick(ThrowMoverTestProbe.LastCreated.MoveDuration);
        Assert.That(specialEffect.calls.Count, Is.EqualTo(1));
    }

    [Test] public void ExpiredAndCleansedStatusDoNotTrigger()
    {
        Grant().Execute(Context()); new BuffTicker(manager.Storage).Tick(1f);
        Assert.That(special.CanTrigger(Context()), Is.False);
        var harmfulGrant = Grant(); harmfulGrant.buffClassification = Asset<BuffClassificationDefinition>();
        harmfulGrant.buffClassification.harmful = true; harmfulGrant.Execute(Context());
        Assert.That(special.CanTrigger(Context()), Is.True);
        manager.Cleanse(BuffTargetHandle.Target(player), new BuffCleanseFilter {
            mode = BuffCleanseMode.Harmful
        });
        Assert.That(special.CanTrigger(Context()), Is.False);
    }

    [Test] public void AnotherKeyOrAnotherTargetDoesNotEnableTheSpecialFunction()
    {
        var other = Grant(); other.specialStatusKey = Asset<SpecialStatusDefinition>();
        other.specialStatusKey.displayName = key.displayName; other.Execute(Context());
        Assert.That(special.CanTrigger(Context()), Is.False);
        var itemStatus = Grant(); itemStatus.target = SpecialStatusQueryTarget.SourceItem; itemStatus.Execute(Context());
        Assert.That(special.CanTrigger(Context()), Is.False);
    }

    [Test] public void SpecialItemAndExcludedItemCannotRecursivelyTrigger()
    {
        Grant().Execute(Context());
        Assert.That(special.CanTrigger(Context(bonusItem)), Is.False);
        var excluded = Asset<ItemData>(); excluded.dataId = "WeaponExtra1";
        Assert.That(special.CanTrigger(Context(excluded)), Is.False);
        key.onItemUsedModules = null;
        Assert.That(special.CanTrigger(Context()), Is.False);
    }

    [Test] public void ModuleThrowRetainsOriginalCompletionAndOwnerUntilTheBonusLands()
    {
        Grant().Execute(Context()); int completed = 0;
        var parent = Context(); parent.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        ItemEffectExecutor.ExecuteItem(source,Vector3.zero,Vector3.right,Vector3.right,
            player.gameObject,null,manager,parent);
        OwnCreatedMover(); parent.lifetime.Close();
        Assert.That(completed, Is.Zero, "원본 완료는 모듈이 던진 추가 아이템도 기다립니다.");
        ThrowMoverTestProbe.LastCreated.Tick(ThrowMoverTestProbe.LastCreated.MoveDuration);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(specialEffect.calls[0].owner, Is.SameAs(player.gameObject));
        Assert.That(specialEffect.calls[0].allowItemUseReactions, Is.False);
    }
}
