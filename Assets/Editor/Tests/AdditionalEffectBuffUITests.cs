using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalEffectBuffUITests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;

    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private GameObject Host()
    {
        var host = new GameObject("Effect Buff UI Test"); owned.Add(host); return host;
    }
    private Health Target()
    {
        Health health = Host().AddComponent<Health>(); health.Init(100f); return health;
    }
    private ItemEffectContext Context(Health target, ItemData item = null, EquipmentBag bag = null)
        => new ItemEffectContext(target.gameObject, item, Vector3.zero, Vector3.right, bag, buffManager: manager);
    private RegenerationEffect Regeneration(bool visible = true)
    {
        var effect = Asset<RegenerationEffect>(); effect.targets.target = HealthTargetMode.Owner;
        effect.buffUI.showInUI = visible; effect.buffUI.displayName = "재생"; return effect;
    }
    private ShieldEffect Shield(bool visible = true)
    {
        var effect = Asset<ShieldEffect>(); effect.targets.target = HealthTargetMode.Owner;
        effect.buffUI.showInUI = visible; effect.buffUI.displayName = "보호막"; return effect;
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
    public void ExistingUIManagerCreatesAndHidesTheEffectSlot()
    {
        BuffUIManager ui = Host().AddComponent<BuffUIManager>();
        ui.buffManager = manager; ui.contentParent = Host().transform;
        ui.slotPrefab = Host().AddComponent<BuffUISlot>(); ui.slotPrefab.gameObject.SetActive(false);
        manager.buffUIManager = ui;
        Health target = Target(); Regeneration().Execute(Context(target));
        BuffUISlot[] slots = ui.contentParent.GetComponentsInChildren<BuffUISlot>(true);
        Assert.That(slots.Length, Is.EqualTo(1));
        Assert.That(slots[0].gameObject.activeSelf, Is.True);
        target.GetComponent<HealthRegenerationRunner>().Tick(5f);
        Assert.That(slots[0].gameObject.activeSelf, Is.False);
    }

    [Test]
    public void HiddenEffectsStillHealAndAbsorbDamage()
    {
        Health target = Target(); target.TakeDamage(20f);
        Regeneration(false).Execute(Context(target));
        target.GetComponent<HealthRegenerationRunner>().Tick(1f);
        Assert.That(target.Hp, Is.EqualTo(81f));
        Shield(false).Execute(Context(target));
        target.TakeDamage(5f);
        Assert.That(target.Hp, Is.EqualTo(81f));
        Assert.That(target.ShieldAmount, Is.EqualTo(10f));
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void RegenerationUIUsesActualClockAndNeverEntersGameplayStorage()
    {
        Health target = Target(); target.TakeDamage(20f);
        RegenerationEffect effect = Regeneration(); effect.Execute(Context(target));
        ActiveBuff display = manager.GetAllVisibleBuffs()[0];
        Assert.That(display.GetUIName(), Is.EqualTo("재생"));
        Assert.That(display.remainTime, Is.EqualTo(5f));
        Assert.That(manager.GetAllActiveBuffs(), Is.Empty);
        Assert.That(manager.Storage.activeBuffs, Is.Empty);
        new BuffTicker(manager.Storage).Tick(2f);
        Assert.That(display.remainTime, Is.EqualTo(5f));
        target.GetComponent<HealthRegenerationRunner>().Tick(1f);
        Assert.That(display.remainTime, Is.EqualTo(4f));
        Assert.That(display.GetTimeRate(), Is.EqualTo(0.8f).Within(0.00001f));
        Assert.That(target.Hp, Is.EqualTo(81f));
        target.GetComponent<HealthRegenerationRunner>().Tick(4f);
        Assert.That(target.Hp, Is.EqualTo(85f));
        Assert.That(display.IsExpired, Is.True);
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void VisibleFiltersUseActualTargetAndSourceProvenance()
    {
        Health target = Target(), otherTarget = Target(); target.buffTargetGroup = "Health/Ally";
        ItemData item = Asset<ItemData>(), otherItem = Asset<ItemData>(); item.series = ItemSeries.Food;
        GameObject bagHost = Host(); bagHost.SetActive(false);
        EquipmentBag bag = bagHost.AddComponent<EquipmentBag>();
        Regeneration().Execute(Context(target, item, bag));
        Assert.That(manager.GetVisibleBagBuffsAsList(bag).Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleBagBuffsAsList(null), Is.Empty);
        Assert.That(manager.GetVisibleItemBuffsAsList(item).Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleItemBuffsAsList(otherItem), Is.Empty);
        Assert.That(manager.GetVisibleItemSeriesBuffsAsList(ItemSeries.Food).Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleItemSeriesBuffsAsList(ItemSeries.Weapon), Is.Empty);
        Assert.That(manager.GetVisibleTargetBuffsAsList(target).Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleTargetBuffsAsList(otherTarget), Is.Empty);
        Assert.That(manager.GetVisibleTargetGroupBuffsAsList("Health").Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleTargetGroupBuffsAsList("Health/Ally").Count, Is.EqualTo(1));
        Assert.That(manager.GetVisibleTargetGroupBuffsAsList("Enemy"), Is.Empty);
    }

    [Test]
    public void IndependentRegenerationApplicationsKeepIndependentTimers()
    {
        Health target = Target(); target.TakeDamage(20f);
        RegenerationEffect effect = Regeneration(); effect.Execute(Context(target));
        HealthRegenerationRunner runner = target.GetComponent<HealthRegenerationRunner>(); runner.Tick(1f);
        effect.Execute(Context(target));
        List<ActiveBuff> displays = manager.GetAllVisibleBuffs();
        Assert.That(displays.Count, Is.EqualTo(2));
        Assert.That(displays[0].remainTime, Is.EqualTo(4f));
        Assert.That(displays[1].remainTime, Is.EqualTo(5f));
        runner.Tick(4f);
        Assert.That(target.Hp, Is.EqualTo(89f));
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(1));
        Assert.That(manager.GetAllVisibleBuffs()[0].remainTime, Is.EqualTo(1f));
    }

    [Test]
    public void ShieldRefreshAndAddKeepOneUIEntryWithRefreshedDuration()
    {
        Health target = Target(); ShieldEffect effect = Shield(); effect.Execute(Context(target));
        ActiveBuff old = manager.GetAllVisibleBuffs()[0]; target.TickShields(3f);
        Assert.That(old.remainTime, Is.EqualTo(7f));
        target.TakeDamage(5f); effect.Execute(Context(target));
        Assert.That(target.ShieldAmount, Is.EqualTo(15f));
        Assert.That(old.IsExpired, Is.True);
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(1));
        Assert.That(manager.GetAllVisibleBuffs()[0].remainTime, Is.EqualTo(10f));
        effect.reapplyMode = ShieldReapplyMode.Add; effect.Execute(Context(target));
        Assert.That(target.ShieldAmount, Is.EqualTo(30f));
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(1));
        target.TickShields(10f);
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void FullyConsumedShieldRemovesUIImmediately()
    {
        Health target = Target(); Shield().Execute(Context(target));
        ActiveBuff display = manager.GetAllVisibleBuffs()[0]; target.TakeDamage(20f);
        Assert.That(target.Hp, Is.EqualTo(95f));
        Assert.That(target.ShieldAmount, Is.Zero);
        Assert.That(display.IsExpired, Is.True);
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void TimeStopFreezesEnemyEffectClocksAndUIWhileItsOwnClockAdvances()
    {
        Health target = Target(); target.team = HealthTeam.Enemy;
        Regeneration().Execute(Context(target)); Shield().Execute(Context(target));
        var effect = Asset<TimeStopEffect>(); effect.buffUI.showInUI = true;
        TimeStopRunner stop = Host().AddComponent<TimeStopRunner>();
        stop.Init(Context(target), TimeStopTargets.EnemyStatusTimers, 2f, effect);
        List<ActiveBuff> displays = manager.GetAllVisibleBuffs(); Assert.That(displays.Count, Is.EqualTo(3));
        target.GetComponent<HealthRegenerationRunner>().Tick(1f); target.TickShields(1f); stop.Tick(1f);
        Assert.That(displays[0].remainTime, Is.EqualTo(5f));
        Assert.That(displays[1].remainTime, Is.EqualTo(10f));
        Assert.That(displays[2].remainTime, Is.EqualTo(1f));
        stop.Tick(1f);
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(2));
        target.GetComponent<HealthRegenerationRunner>().Tick(1f); target.TickShields(1f);
        Assert.That(displays[0].remainTime, Is.EqualTo(4f));
        Assert.That(displays[1].remainTime, Is.EqualTo(9f));
    }

    [Test]
    public void GenerationCancellationHidesUIBeforeTheNextGameplayTick()
    {
        Health target = Target(); Regeneration().Execute(Context(target)); Shield().Execute(Context(target));
        List<ActiveBuff> displays = manager.GetAllVisibleBuffs(); ItemEffectRuntime.CancelAll();
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
        Assert.That(displays[0].IsExpired && displays[1].IsExpired, Is.True);
        target.GetComponent<HealthRegenerationRunner>().Tick(0f); target.TickShields(0f);
        Assert.That(target.GetComponent<HealthRegenerationRunner>().ActiveCount, Is.Zero);
        Assert.That(target.ShieldAmount, Is.Zero);
    }

    [Test]
    public void TargetDisableOrNewLifeRemovesOldEntries()
    {
        Health target = Target(); Regeneration().Execute(Context(target)); Shield().Execute(Context(target));
        target.Init(30f); target.GetComponent<HealthRegenerationRunner>().Tick(0f);
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
        Regeneration().Execute(Context(target)); Shield().Execute(Context(target));
        target.gameObject.SetActive(false);
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void UIUsesScaledDurationAndDisappearsBeforeCompletionCallback()
    {
        Health target = Target(); RegenerationEffect effect = Regeneration();
        ItemEffectContext context = Context(target); context.durationMultiplier = 2f;
        bool completed = false;
        context.lifetime = new ItemEffectLifetime(onCompleted: () =>
        {
            completed = true; Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
        });
        effect.Execute(context); context.lifetime.Close();
        Assert.That(manager.GetAllVisibleBuffs()[0].maxTime, Is.EqualTo(10f));
        target.GetComponent<HealthRegenerationRunner>().Tick(10f);
        Assert.That(completed, Is.True);
    }

    [Test]
    public void InvalidApplicationsDoNotCreateUI()
    {
        Health target = Target(); RegenerationEffect regeneration = Regeneration(); ShieldEffect shield = Shield();
        regeneration.regenerationStat.regenerationDuration = 0f;
        shield.shieldStat.shieldAmount = 0f;
        regeneration.Execute(Context(target)); shield.Execute(Context(target));
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
        target.ApplyDamage(1000f);
        Regeneration().Execute(Context(target)); Shield().Execute(Context(target));
        Assert.That(manager.GetAllVisibleBuffs(), Is.Empty);
    }

    [Test]
    public void UIReadAndBuffClearingDoNotConsumeOrCancelRegeneration()
    {
        Health target = Target(); target.TakeDamage(20f); RegenerationEffect regeneration = Regeneration();
        ItemData source = Asset<ItemData>(); regeneration.Execute(Context(target, source));
        BuffEffect buff = Asset<BuffEffect>(); buff.includeSelf = true;
        buff.targetResolver = Asset<AllItemsBuffTargetResolver>();
        buff.buffInfo.useLimitType = BuffUseLimitType.UseCount; buff.buffInfo.maxUseCount = 2;
        buff.buffInfo.useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed;
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = "ItemCooldownStat"; modifier.fieldName = "cooldown"; modifier.addValue = -0.2f;
        buff.modifiers = new BuffModifier[] { modifier }; buff.Execute(Context(target, source));
        ActiveBuff gameplayBuff = manager.GetAllActiveBuffs()[0];
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(2));
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(2));
        Assert.That(gameplayBuff.remainUseCount, Is.EqualTo(2));
        manager.ClearAllBuffs();
        Assert.That(manager.GetAllVisibleBuffs().Count, Is.EqualTo(1));
        target.GetComponent<HealthRegenerationRunner>().Tick(1f);
        Assert.That(target.Hp, Is.EqualTo(81f));
    }

    [Test]
    public void NameAndIconOverridesFallBackWithoutChangingExistingBuffs()
    {
        Health target = Target(); var effect = Regeneration(); effect.name = "Regeneration Asset";
        effect.buffUI.displayName = " "; effect.Execute(Context(target));
        Assert.That(manager.GetAllVisibleBuffs()[0].GetUIName(), Is.EqualTo("Regeneration Asset"));
        Texture2D texture = new Texture2D(2, 2); owned.Add(texture);
        Sprite icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(icon);
        effect.buffUI.displayName = "회복 중"; effect.buffUI.buffIcon = icon;
        effect.Execute(Context(target));
        ActiveBuff display = manager.GetAllVisibleBuffs()[1];
        Assert.That(display.GetUIName(), Is.EqualTo("회복 중"));
        Assert.That(display.GetUIIcon(), Is.SameAs(icon));
        ItemData item = Asset<ItemData>(); item.icon = icon;
        effect.buffUI.buffIcon = null; effect.Execute(Context(target, item));
        Assert.That(manager.GetAllVisibleBuffs()[2].GetUIIcon(), Is.SameAs(icon));
        BuffEffect existing = Asset<BuffEffect>(); existing.buffIcon = icon;
        ActiveBuff normal = new ActiveBuff(null, new BuffInfo(), null, null, existing, null, true, true);
        Assert.That(normal.GetUIIcon(), Is.SameAs(icon));
    }
}
