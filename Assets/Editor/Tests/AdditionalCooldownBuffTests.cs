using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalCooldownBuffTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;

    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private GameObject Host()
    {
        var host = new GameObject("Cooldown Buff Test"); owned.Add(host); return host;
    }
    private ItemData Item(float cooldown = 5f)
    {
        ItemData item = Asset<ItemData>(); item.cooldown = cooldown; return item;
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
    private BagItemUseManager Bag(params ItemData[] items)
    {
        GameObject host = Host(); host.SetActive(false);
        EquipmentBag bag = host.AddComponent<EquipmentBag>();
        bag.equippedItems = new List<InventoryItem>();
        foreach (ItemData item in items) bag.equippedItems.Add(new InventoryItem(item, 1));
        bag.currentSlotCount = items.Length;
        ItemEffectExecutor executor = host.AddComponent<ItemEffectExecutor>(); executor.buffManager = manager;
        ItemThrowExecutor thrower = host.AddComponent<ItemThrowExecutor>(); thrower.itemEffectExecutor = executor;
        BagItemUseManager use = host.AddComponent<BagItemUseManager>();
        use.bag = bag; use.bagCooldown = 3f; use.throwExecutor = thrower;
        use.Init(); use.SetCooldownTickEnabled(true);
        return use;
    }
    private BagItemCooldownController Controller(BagItemUseManager bag)
        => (BagItemCooldownController)typeof(BagItemUseManager).GetField("cooldownController",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bag);
    private OwnerBuffTargetResolver Source(OwnerBuffTargetResolver.OwnerTargetMode mode)
    {
        OwnerBuffTargetResolver resolver = Asset<OwnerBuffTargetResolver>(); resolver.targetMode = mode; return resolver;
    }
    private BuffEffect Buff<TStat>(BuffTargetResolver resolver, string field = "cooldown", float add = -0.2f,
        float multiply = 0f, ItemData sourceItem = null, EquipmentBag sourceBag = null)
    {
        BuffEffect effect = Asset<BuffEffect>(); effect.targetResolver = resolver; effect.includeSelf = true;
        effect.buffInfo.duration = 5f;
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = typeof(TStat).Name; modifier.fieldName = field;
        modifier.addValue = add; modifier.multiplyValue = multiply;
        effect.modifiers = new BuffModifier[] { modifier };
        Apply(effect, sourceItem, sourceBag);
        return effect;
    }
    private void Apply(BuffEffect effect, ItemData sourceItem = null, EquipmentBag sourceBag = null)
        => effect.Execute(new ItemEffectContext(null, sourceItem, Vector3.zero, Vector3.zero, sourceBag, buffManager: manager));

    [Test] public void AllItemsFixedReductionChangesBothBagsWithoutChangingAssetsOrBagCooldown()
    {
        ItemData first = Item(), second = Item(2f);
        BagItemUseManager a = Bag(first), b = Bag(second);
        Buff<ItemCooldownStat>(Asset<AllItemsBuffTargetResolver>());
        Assert.That(a.GetItemCooldown(first), Is.EqualTo(4.8f).Within(0.0001f));
        Assert.That(b.GetItemCooldown(second), Is.EqualTo(1.8f).Within(0.0001f));
        Assert.That(first.cooldown, Is.EqualTo(5f));
        Assert.That(a.GetBagCooldown(), Is.EqualTo(3f));
        Assert.That(manager.GetBuffedStatForItem(new DamageAreaAttackStat { damageAreaPower = 10f }, first, a.bag)
            .damageAreaPower, Is.EqualTo(10f));
    }

    [Test] public void FixedReductionIsCapturedOnStartAndExpiryRestoresFutureCooldownWithStableUi()
    {
        ItemData item = Item(); BagItemUseManager bag = Bag(item);
        Buff<ItemCooldownStat>(Asset<AllItemsBuffTargetResolver>());
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(5f));
        bag.ResetAllCooldowns();
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(4.8f).Within(0.0001f));
        Assert.That(bag.GetSlotCooldownRatio(0), Is.EqualTo(1f));
        bag.TickCooldown(1f);
        Assert.That(new BuffTicker(manager.Storage).Tick(5f), Is.True);
        Assert.That(bag.GetItemCooldown(item), Is.EqualTo(5f));
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(3.8f).Within(0.0001f));
        Assert.That(bag.GetSlotCooldownRatio(0), Is.EqualTo(3.8f / 4.8f).Within(0.0001f));
        bag.ResetAllCooldowns();
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(5f));
    }

    [Test] public void RefreshDoesNotCompoundWhileStackUsesTheConfiguredLimit()
    {
        ItemData item = Item(); BagItemUseManager bag = Bag(item);
        BuffEffect effect = Buff<ItemCooldownStat>(Asset<AllItemsBuffTargetResolver>());
        Apply(effect);
        Assert.That(bag.GetItemCooldown(item), Is.EqualTo(4.8f).Within(0.0001f));
        effect.buffInfo.stackMode = BuffStackMode.Stack; effect.buffInfo.maxStack = 3;
        Apply(effect); Apply(effect); Apply(effect);
        Assert.That(bag.GetItemCooldown(item), Is.EqualTo(4.4f).Within(0.0001f));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
    }

    [Test] public void SourceBagItemBuffAffectsOnlyItemsInTheGeneratingBag()
    {
        ItemData item = Item(); BagItemUseManager a = Bag(item), b = Bag(item);
        Buff<ItemCooldownStat>(Source(OwnerBuffTargetResolver.OwnerTargetMode.SourceBag), sourceBag: a.bag);
        Assert.That(a.GetItemCooldown(item), Is.EqualTo(4.8f).Within(0.0001f));
        Assert.That(b.GetItemCooldown(item), Is.EqualTo(5f));
        Assert.That(a.GetBagCooldown(), Is.EqualTo(3f));
    }

    [Test] public void ItemRecoveryRateChangesRunningItemsAndExpiryRestoresRateWithoutChangingBag()
    {
        BagItemUseManager bag = Bag(Item());
        Buff<ItemCooldownStat>(Asset<AllItemsBuffTargetResolver>(), "cooldownRecoveryRate", 0f, 0.2f);
        Controller(bag).StartBagCooldown(bag.GetBagCooldown());
        bag.TickCooldown(1f);
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(3.8f).Within(0.0001f));
        Assert.That(bag.GetBagCooldownRemain(), Is.EqualTo(2f));
        new BuffTicker(manager.Storage).Tick(5f);
        bag.TickCooldown(1f);
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(2.8f).Within(0.0001f));
        Assert.That(bag.GetBagCooldownRemain(), Is.EqualTo(1f));
    }

    [Test] public void ItemSpecificRecoveryDoesNotAccelerateOtherPreparedItems()
    {
        ItemData first = Item(), second = Item(); BagItemUseManager bag = Bag(first, second);
        Buff<ItemCooldownStat>(Source(OwnerBuffTargetResolver.OwnerTargetMode.SourceItem),
            "cooldownRecoveryRate", 0f, 1f, first);
        CooldownControlEffect prepare = Asset<CooldownControlEffect>(); prepare.amount = 0f;
        bag.ApplyCooldownControl(prepare);
        bag.TickCooldown(1f);
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(3f));
        Assert.That(bag.GetSlotCooldownRemain(1), Is.EqualTo(4f));
    }

    [Test] public void PlayerRecoveryQueriesCurrentBuffsEvenWhenPlayerRegistrationWasMissed()
    {
        BagItemUseManager bag = Bag(Item());
        GameObject host = Host(); host.SetActive(false);
        Player player = host.AddComponent<Player>(); player.baseStat = new PlayerStat();
        player.currentStat = new PlayerStat(); player.buffManager = manager; bag.cooldownOwner = player;
        BuffTargetGroupResolver group = Asset<BuffTargetGroupResolver>(); group.targetGroup = "Player";
        Buff<PlayerStat>(group, "cooldownRecoveryRate", 0f, 0.2f);
        Assert.That(player.currentStat.cooldownRecoveryRate, Is.EqualTo(1f));
        Controller(bag).StartBagCooldown(bag.GetBagCooldown());
        bag.TickCooldown(1f);
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(3.8f).Within(0.0001f));
        Assert.That(bag.GetBagCooldownRemain(), Is.EqualTo(2f));
    }

    [Test] public void BagFixedReductionIsSeparateAndCapturedAtTheActualCycleBoundary()
    {
        BagItemUseManager bag = Bag(Item());
        Buff<BagCooldownStat>(Asset<AllBagsBuffTargetResolver>());
        typeof(BagItemUseManager).GetMethod("ApplyUseResult", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(bag, new object[] { 0 });
        Assert.That(bag.GetBagCooldownRemain(), Is.EqualTo(2.8f).Within(0.0001f));
        Assert.That(bag.GetBagCooldownRatio(), Is.EqualTo(1f));
        Assert.That(bag.GetItemCooldown(bag.bag.equippedItems[0].itemData), Is.EqualTo(5f));
        bag.TickCooldown(1f);
        new BuffTicker(manager.Storage).Tick(5f);
        Assert.That(bag.GetBagCooldown(), Is.EqualTo(3f));
        Assert.That(bag.GetBagCooldownRatio(), Is.EqualTo(1.8f / 2.8f).Within(0.0001f));
    }

    [Test] public void BagRecoveryDoesNotAccelerateItemCooldowns()
    {
        BagItemUseManager bag = Bag(Item());
        Buff<BagCooldownStat>(Asset<AllBagsBuffTargetResolver>(), "cooldownRecoveryRate", 0f, 1f);
        Controller(bag).StartBagCooldown(bag.GetBagCooldown());
        bag.TickCooldown(1f);
        Assert.That(bag.GetBagCooldownRemain(), Is.EqualTo(1f));
        Assert.That(bag.GetSlotCooldownRemain(0), Is.EqualTo(4f));
    }

    [Test] public void SourceBagCooldownBuffAffectsOnlyThatBag()
    {
        BagItemUseManager a = Bag(Item()), b = Bag(Item());
        Buff<BagCooldownStat>(Source(OwnerBuffTargetResolver.OwnerTargetMode.SourceBag), sourceBag: a.bag);
        Assert.That(a.GetBagCooldown(), Is.EqualTo(2.8f).Within(0.0001f));
        Assert.That(b.GetBagCooldown(), Is.EqualTo(3f));
    }

    [Test] public void AllItemsAndAllBagsTargetsDoNotLeakIntoEachOthersQueries()
    {
        ItemData item = Item(); BagItemUseManager a = Bag(item), b = Bag(Item());
        BuffEffect bagBuff = Buff<BagCooldownStat>(Asset<AllBagsBuffTargetResolver>());
        Buff<BagCooldownStat>(Asset<AllItemsBuffTargetResolver>(), add: -1f);
        Buff<ItemCooldownStat>(Asset<AllBagsBuffTargetResolver>(), add: -1f);
        Assert.That(a.GetBagCooldown(), Is.EqualTo(2.8f).Within(0.0001f));
        Assert.That(a.GetItemCooldown(item), Is.EqualTo(5f));
        Assert.That(manager.GetVisibleBagBuffsAsList(a.bag), Does.Contain(manager.Storage.activeBuffs[0]));
        Assert.That(manager.HasActiveBuffForBag(bagBuff, b.bag), Is.True);
        Assert.That(BuffTargetHandle.AllBags().Matches(BuffQueryContext.ForItem(item, a.bag)), Is.False);
        Assert.That(BuffTargetHandle.AllItems().Matches(BuffQueryContext.ForBag(a.bag)), Is.False);
    }

    [Test] public void QueriesAndTicksDoNotConsumeUseCountBuffsAndExcessReductionClampsAtZero()
    {
        ItemData item = Item(0.1f); BagItemUseManager bag = Bag(item);
        BuffEffect effect = Buff<ItemCooldownStat>(Asset<AllItemsBuffTargetResolver>());
        effect.buffInfo.useLimitType = BuffUseLimitType.UseCount;
        effect.buffInfo.maxUseCount = 2;
        Apply(effect);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(bag.GetItemCooldown(item), Is.Zero);
            bag.GetSlotCooldownRatio(0); bag.TickCooldown(0.01f);
        }
        Assert.That(manager.Storage.activeBuffs[0].remainUseCount, Is.EqualTo(2));
        Assert.That(item.cooldown, Is.EqualTo(0.1f));
    }
}
