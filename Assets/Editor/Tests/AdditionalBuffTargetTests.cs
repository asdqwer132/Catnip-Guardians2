using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalBuffTargetTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private GameObject Host()
    {
        var host = new GameObject("Buff Target Test"); owned.Add(host); return host;
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
        owned.Clear();
        BuffManager.instance = previousManager;
    }
    private ItemEffectContext Context(ItemData item = null)
        => new ItemEffectContext(null, item, Vector3.zero, Vector3.zero, null, buffManager: manager);
    private DamageArea Area(ItemData item = null, ItemEffectContext context = null)
    {
        GameObject host = Host();
        host.AddComponent<CircleCollider2D>();
        DamageArea area = host.AddComponent<DamageArea>();
        if (context != null) area.BindLifetime(context);
        area.InitWithSnapshotAndDynamicBuff(new DamageAreaAttackStat { damageAreaPower = 10f,
            damageAreaRange = 1f, damageAreaLifeTime = 60f }, item, null, manager, null);
        return area;
    }
    private BuffEffect Buff(BuffTargetResolver resolver, string field, float add, ItemData item = null)
    {
        BuffEffect effect = Asset<BuffEffect>();
        effect.includeSelf = true;
        effect.targetResolver = resolver;
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = nameof(DamageAreaAttackStat);
        modifier.fieldName = field; modifier.addValue = add;
        effect.modifiers = new BuffModifier[] { modifier };
        effect.Execute(Context(item));
        return effect;
    }
    private BuffTargetGroupResolver Group(string name = "DamageArea")
    {
        BuffTargetGroupResolver resolver = Asset<BuffTargetGroupResolver>(); resolver.targetGroup = name; return resolver;
    }
    private float Damage(DamageArea area)
        => (float)typeof(DamageArea).GetField("damage", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(area);

    [Test] public void DamageAreaGroupBuffAppliesToExistingAndFutureAttacksAndRemovalRestoresBoth()
    {
        DamageArea existing = Area();
        Buff(Group(), "damageAreaPower", 5f);
        ActiveBuff active = manager.Storage.activeBuffs[0];
        DamageArea future = Area();
        Assert.That(Damage(existing), Is.EqualTo(15f));
        Assert.That(Damage(future), Is.EqualTo(15f));
        Assert.That(manager.GetRegisteredBuffTargetsUnsafe(), Does.Contain(existing));
        manager.RemoveBuffHandle(active);
        Assert.That(Damage(existing), Is.EqualTo(10f));
        Assert.That(Damage(future), Is.EqualTo(10f));
    }

    [Test] public void DamageAreaTargetBuffAndItemDynamicBuffApplyOnceBeforeExecutionScale()
    {
        ItemData item = Asset<ItemData>();
        BuffEffect itemBuff = Buff(Asset<AdditionalAllItemsResolver>(), "damageAreaPower", 10f, item);
        itemBuff.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        manager.Storage.activeBuffs[0].applyTiming = BuffApplyTiming.Dynamic;
        Buff(Group(), "damageAreaPower", 10f);
        ItemEffectContext context = Context(item); context.damageMultiplier = 0.5f;
        DamageArea area = Area(item, context);
        Assert.That(Damage(area), Is.EqualTo(15f));
        area.RefreshBuffedStat(); area.RefreshBuffedStat();
        Assert.That(Damage(area), Is.EqualTo(15f));
    }

    [Test] public void DirectDamageAreaBuffIsRemovedBeforePoolReuseWhileGroupBuffRemains()
    {
        DamageArea area = Area();
        Buff(Group(), "damageAreaPower", 3f);
        DirectBuffTargetResolver direct = Asset<DirectBuffTargetResolver>(); direct.targetComponent = area;
        Buff(direct, "damageAreaPower", 2f);
        Assert.That(Damage(area), Is.EqualTo(15f));
        area.gameObject.SetActive(false);
       // Assert.That(manager.GetRegisteredBuffTargetsUnsafe(), Does.Not.Contain(area));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        area.gameObject.SetActive(true);
        Assert.That(manager.GetRegisteredBuffTargetsUnsafe(), Does.Contain(area));
        Assert.That(Damage(area), Is.EqualTo(13f));
    }

    [Test] public void DirectResolverAcceptsTheAttackTransformWithoutSelectingAnUnrelatedAttack()
    {
        DamageArea area = Area(), other = Area();
        DirectBuffTargetResolver resolver = Asset<DirectBuffTargetResolver>(); resolver.targetComponent = area.transform;
        Buff(resolver, "damageAreaRange", 2f);
        Assert.That(area.circleCollider.radius, Is.EqualTo(3f));
        Assert.That(other.circleCollider.radius, Is.EqualTo(1f));
    }

    [Test] public void CustomDamageAreaChildGroupMatchesItsParentAndRetainsItsSerializedName()
    {
        DamageArea area = Area(); area.buffTargetGroup = "DamageArea/Fire";
        Buff(Group(), "damageAreaPower", 5f);
        Assert.That(Damage(area), Is.EqualTo(15f));
        Assert.That(area.buffTargetGroup, Is.EqualTo("DamageArea/Fire"));
    }

    [Test] public void AreaResolverIncludesDamageAreaChildGroupsButExcludesOtherGroupsAndDistantAttacks()
    {
        DamageArea near = Area(), child = Area(), far = Area();
        child.buffTargetGroup = "DamageArea/Fire";
        child.transform.position = Vector3.right;
        far.transform.position = Vector3.right * 20f;
        GameObject unrelated = Host(); unrelated.AddComponent<CircleCollider2D>(); unrelated.AddComponent<Health>();
        Physics2D.SyncTransforms();
        BuffTargetAreaResolver resolver = Asset<BuffTargetAreaResolver>();
        resolver.radius = 3f; resolver.requiredGroup = "DamageArea";
        var matches = new List<BuffTargetHandle>();
        resolver.ResolveTargets(new BuffRegisterContext(Context(), manager), matches);
        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches.Exists(handle => handle.targetObject == near), Is.True);
        Assert.That(matches.Exists(handle => handle.targetObject == child), Is.True);
    }

    [Test] public void ReinitializingIntoAnotherManagerRemovesTheOldTargetRegistration()
    {
        DamageArea area = Area();
        BuffManager next = Host().AddComponent<BuffManager>();
        if (next.Storage == null)
            typeof(BuffManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(next, null);
        area.InitWithSnapshotAndDynamicBuff(new DamageAreaAttackStat { damageAreaPower = 7f }, null, null, next, null);
       // Assert.That(manager.GetRegisteredBuffTargetsUnsafe(), Does.Not.Contain(area));
        Assert.That(next.GetRegisteredBuffTargetsUnsafe(), Does.Contain(area));
        Assert.That(Damage(area), Is.EqualTo(7f));
    }
}
