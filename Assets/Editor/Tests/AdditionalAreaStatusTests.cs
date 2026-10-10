using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalAreaStatusTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }
    private GameObject Host()
    {
        GameObject host = new GameObject("AreaStatusTest");
        owned.Add(host);
        return host;
    }
    private BuffManager Manager()
    {
        BuffManager manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null)
            typeof(BuffManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
        return manager;
    }
    private ItemEffectContext Context(GameObject owner, BuffManager manager = null, ItemData item = null)
        => new ItemEffectContext(owner, item, Vector3.zero, Vector3.zero, null, buffManager: manager, direction: Vector3.right);
    private GrantSpecialStatusEffect Status(SpecialStatusDefinition key, BuffUseLimitType limit = BuffUseLimitType.Time,
        BuffClassificationDefinition classification = null)
    {
        GrantSpecialStatusEffect effect = Asset<GrantSpecialStatusEffect>();
        effect.specialStatusKey = key;
        effect.buffClassification = classification;
        effect.target = SpecialStatusQueryTarget.Owner;
        effect.lifetimeSettings.reapplyMode = BuffReapplyMode.Refresh;
        effect.lifetimeSettings.useLimitType = limit;
        return effect;
    }
    private ActiveBuff Register(BuffManager manager, GrantSpecialStatusEffect effect, ItemEffectContext context, IBuffTarget target)
        => manager.RegisterSpecialStatus(effect, context.GetCurrentStat(effect, effect.lifetimeSettings), context, BuffTargetHandle.Target(target));
    private ReactiveGroundArea Area(AreaDefinition definition, GameObject owner, Vector3 position)
    {
        AreaEffectData effect = Asset<AreaEffectData>();
        effect.areaDefinition = definition;
        GameObject host = Host();
        host.transform.position = position;
        ReactiveGroundArea area = host.AddComponent<ReactiveGroundArea>();
        area.Init(effect, Context(owner));
        return area;
    }

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void StatusWithoutModifiersUsesExistingStackAndExpiry()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        SpecialStatusDefinition key = Asset<SpecialStatusDefinition>();
        GrantSpecialStatusEffect effect = Status(key);
        effect.lifetimeSettings.stackMode = BuffStackMode.Stack;
        effect.lifetimeSettings.maxStack = 3;
        effect.lifetimeSettings.duration = 2f;
        ItemEffectContext context = Context(target.gameObject, manager);
        ActiveBuff first = Register(manager, effect, context, target);
        Assert.That(first, Is.Not.Null);
        Assert.That(Register(manager, effect, context, target), Is.SameAs(first));
        Assert.That(manager.GetSpecialStatusStack(key, BuffQueryContext.ForTarget(target)), Is.EqualTo(2));
        first.Tick(2f);
        Assert.That(manager.HasSpecialStatus(key, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void CleanseRequiresBothHarmfulAndDispellableTags()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        SpecialStatusDefinition harmful = Asset<SpecialStatusDefinition>();
        var harmfulRules = Asset<BuffClassificationDefinition>(); harmfulRules.harmful = true;
        SpecialStatusDefinition locked = Asset<SpecialStatusDefinition>();
        var lockedRules = Asset<BuffClassificationDefinition>(); lockedRules.harmful = true; lockedRules.dispellable = false;
        SpecialStatusDefinition beneficial = Asset<SpecialStatusDefinition>();
        Register(manager, Status(harmful, classification: harmfulRules), context, target);
        Register(manager, Status(locked, classification: lockedRules), context, target);
        Register(manager, Status(beneficial), context, target);
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target)), Is.EqualTo(1));
        Assert.That(manager.HasSpecialStatus(harmful, BuffQueryContext.ForTarget(target)), Is.False);
        Assert.That(manager.HasSpecialStatus(locked, BuffQueryContext.ForTarget(target)), Is.True);
        Assert.That(manager.HasSpecialStatus(beneficial, BuffQueryContext.ForTarget(target)), Is.True);
    }

    [Test]
    public void NewUseCountStatusIsNotConsumedByTheUseThatRegisteredIt()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemData item = Asset<ItemData>();
        GrantSpecialStatusEffect effect = Status(Asset<SpecialStatusDefinition>(), BuffUseLimitType.UseCount);
        effect.lifetimeSettings.useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed;
        BuffItemUseToken token = manager.BeginItemUse(item, null);
        ActiveBuff buff = Register(manager, effect, Context(target.gameObject, manager, item), target);
        manager.EndItemUse(token);
        Assert.That(buff.remainUseCount, Is.EqualTo(1));
        token = manager.BeginItemUse(item, null);
        manager.EndItemUse(token);
        Assert.That(manager.HasSpecialStatus(effect.specialStatusKey, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void ConsumeStatusDoesNotRemoveStateReappliedAfterTheCheck()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        GrantSpecialStatusEffect effect = Status(Asset<SpecialStatusDefinition>());
        ItemEffectContext context = Context(target.gameObject, manager);
        Register(manager, effect, context, target);
        HasSpecialStatusCondition condition = Asset<HasSpecialStatusCondition>();
        condition.specialStatusKey = effect.specialStatusKey;
        condition.target = SpecialStatusQueryTarget.Owner;
        Assert.That(condition.IsSatisfied(context), Is.True);
        Register(manager, effect, context, target);
        RemoveCheckedSpecialStatusEffect consume = Asset<RemoveCheckedSpecialStatusEffect>();
        consume.checkedCondition = condition;
        consume.ExecuteEffect(context);
        Assert.That(manager.HasSpecialStatus(condition.specialStatusKey, BuffQueryContext.ForTarget(target)), Is.True);
    }

    [Test]
    public void FrozenBranchUsesStateFromPrepareBeforeTheStateChanges()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        GrantSpecialStatusEffect status = Status(Asset<SpecialStatusDefinition>());
        ActiveBuff buff = Register(manager, status, context, target);
        HasSpecialStatusCondition condition = Asset<HasSpecialStatusCondition>();
        condition.target = SpecialStatusQueryTarget.Owner;
        condition.specialStatusKey = status.specialStatusKey;
        CombatEffectRecordingEffect yes = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect no = Asset<CombatEffectRecordingEffect>();
        ConditionalItemEffectData branch = Asset<ConditionalItemEffectData>();
        branch.freezeBranchAtPrepare = true;
        branch.conditions = new ItemEffectConditionData[] { condition };
        branch.effectWhenTrue = yes;
        branch.effectWhenFalse = no;
        branch.Prepare(context);
        manager.RemoveBuffHandle(buff);
        branch.ExecuteEffect(context);
        Assert.That(yes.calls.Count, Is.EqualTo(1));
        Assert.That(no.calls.Count, Is.Zero);
    }

    [Test]
    public void AreaQueriesUseAssetIdentityPositionOwnerAndCount()
    {
        GameObject owner = Host();
        AreaDefinition type = Asset<AreaDefinition>();
        Area(owner: owner, definition: type, position: Vector3.zero);
        Area(type, owner, Vector3.right * 20f);
        Area(type, Host(), Vector3.zero);
        Area(Asset<AreaDefinition>(), owner, Vector3.zero);
        HasAreaConditionData condition = Asset<HasAreaConditionData>();
        condition.definition = type;
        condition.minimumCount = 2;
        ItemEffectContext context = Context(owner);
        Assert.That(condition.IsSatisfied(context), Is.False);
        condition.position = AreaQueryPosition.Anywhere;
        Assert.That(condition.IsSatisfied(context), Is.True);
        condition.position = AreaQueryPosition.ImpactPosition;
        condition.ownerFilter = AreaOwnerFilter.Any;
        Assert.That(condition.IsSatisfied(context), Is.True);
    }

    [Test]
    public void ConsumeAreaUsesTheCheckedHandleAndSkipsNaturalEnEffects()
    {
        GameObject owner = Host();
        AreaDefinition type = Asset<AreaDefinition>();
        CombatEffectRecordingEffect naturalEnd = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect after = Asset<CombatEffectRecordingEffect>();
        type.onNaturalEnd = new ItemEffectData[] { naturalEnd };
        ReactiveGroundArea first = Area(type, owner, Vector3.zero);
        HasAreaConditionData condition = Asset<HasAreaConditionData>();
        condition.definition = type;
        ItemEffectContext context = Context(owner);
        Assert.That(condition.IsSatisfied(context), Is.True);
        ReactiveGroundArea later = Area(type, owner, Vector3.zero);
        ConsumeAreaEffect consume = Asset<ConsumeAreaEffect>();
        consume.checkedCondition = condition;
        consume.afterConsumeEffects = new ItemEffectData[] { after };
        consume.ExecuteEffect(context);
        Assert.That(first == null || !first.IsRegistered, Is.True);
        Assert.That(later.IsRegistered, Is.True);
        Assert.That(naturalEnd.calls.Count, Is.Zero);
        Assert.That(after.calls.Count, Is.EqualTo(1));
    }

    [Test]
    public void ConsumingAStaleAreaCheckCannotRemoveAReplacement()
    {
        GameObject owner = Host();
        AreaDefinition type = Asset<AreaDefinition>();
        ReactiveGroundArea first = Area(type, owner, Vector3.zero);
        HasAreaConditionData condition = Asset<HasAreaConditionData>();
        condition.definition = type;
        ItemEffectContext context = Context(owner);
        Assert.That(condition.IsSatisfied(context), Is.True);
        first.Consume();
        ReactiveGroundArea later = Area(type, owner, Vector3.zero);
        ConsumeAreaEffect consume = Asset<ConsumeAreaEffect>();
        consume.checkedCondition = condition;
        consume.ExecuteEffect(context);
        Assert.That(later.IsRegistered, Is.True);
    }

    [Test]
    public void EventMarkerSharesStatusQueriesAndReportsCleanseReason()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        SpecialStatusDefinition key = Asset<SpecialStatusDefinition>();
        var classification = Asset<BuffClassificationDefinition>(); classification.harmful = true;
        ActiveBuff marker = manager.RegisterSpecialStatusForTarget(key, new BuffInfo { duration = 5f },
            Context(target.gameObject, manager), target, Asset<SpecialStatusDefinition>(), classification);
        BuffRemovalReason? removed = null;
        marker.Removed += (buff, reason) => removed = reason;
        Assert.That(manager.HasSpecialStatus(key, BuffQueryContext.ForTarget(target)), Is.True);
        manager.Cleanse(BuffTargetHandle.Target(target));
        Assert.That(removed, Is.EqualTo(BuffRemovalReason.Cleansed));
        Assert.That(manager.HasSpecialStatus(key, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void ExpiryCallbackCanRegisterNewStatusWithoutTickingItInTheSameFrame()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        GrantSpecialStatusEffect initial = Status(Asset<SpecialStatusDefinition>());
        initial.lifetimeSettings.duration = 1f;
        GrantSpecialStatusEffect next = Status(Asset<SpecialStatusDefinition>());
        next.lifetimeSettings.duration = 5f;
        ActiveBuff created = null;
        BuffRemovalReason? removed = null;
        ActiveBuff first = Register(manager, initial, context, target);
        first.Removed += (buff, reason) =>
        {
            removed = reason;
            created = Register(manager, next, context, target);
        };
        Assert.That(new BuffTicker(manager.Storage).Tick(1f), Is.True);
        Assert.That(removed, Is.EqualTo(BuffRemovalReason.NaturalExpiry));
        Assert.That(created, Is.Not.Null);
        Assert.That(created.remainTime, Is.EqualTo(5f));
    }
}

public sealed class AdditionalAreaStatusTarget : MonoBehaviour, IBuffTarget
{
    public Object BuffTargetObject => this;
    public string BuffTargetGroup => "AreaStatusTest";
    public string BuffTargetDebugName => name;
    public void RefreshBuffedStat() { }
}
