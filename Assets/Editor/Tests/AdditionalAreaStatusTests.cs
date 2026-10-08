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
    private BuffEffect Status(StatusDefinition key, BuffUseLimitType limit = BuffUseLimitType.Time)
    {
        BuffEffect effect = Asset<BuffEffect>();
        effect.buffInfo.statusDefinition = key;
        effect.buffInfo.useLimitType = limit;
        effect.includeSelf = true;
        return effect;
    }
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
        StatusDefinition key = Asset<StatusDefinition>();
        BuffEffect effect = Status(key);
        effect.buffInfo.stackMode = BuffStackMode.Stack;
        effect.buffInfo.maxStack = 3;
        effect.buffInfo.duration = 2f;
        ItemEffectContext context = Context(target.gameObject, manager);
        ActiveBuff first = manager.RegisterBuffForTargetHandle(effect, context, target);
        Assert.That(first, Is.Not.Null);
        Assert.That(manager.RegisterBuffForTargetHandle(effect, context, target), Is.SameAs(first));
        Assert.That(manager.GetStatusStack(key, BuffQueryContext.ForTarget(target)), Is.EqualTo(2));
        first.Tick(2f);
        Assert.That(manager.HasStatus(key, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void CleanseRequiresBothHarmfulAndDispellableTags()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        StatusDefinition harmful = Asset<StatusDefinition>();
        harmful.harmful = true;
        StatusDefinition locked = Asset<StatusDefinition>();
        locked.harmful = true;
        locked.dispellable = false;
        StatusDefinition beneficial = Asset<StatusDefinition>();
        manager.RegisterBuffForTargetHandle(Status(harmful), context, target);
        manager.RegisterBuffForTargetHandle(Status(locked), context, target);
        manager.RegisterBuffForTargetHandle(Status(beneficial), context, target);
        Assert.That(manager.Cleanse(BuffTargetHandle.Target(target)), Is.EqualTo(1));
        Assert.That(manager.HasStatus(harmful, BuffQueryContext.ForTarget(target)), Is.False);
        Assert.That(manager.HasStatus(locked, BuffQueryContext.ForTarget(target)), Is.True);
        Assert.That(manager.HasStatus(beneficial, BuffQueryContext.ForTarget(target)), Is.True);
    }

    [Test]
    public void NewUseCountStatusIsNotConsumedByTheUseThatRegisteredIt()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemData item = Asset<ItemData>();
        BuffEffect effect = Status(Asset<StatusDefinition>(), BuffUseLimitType.UseCount);
        effect.buffInfo.useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed;
        BuffItemUseToken token = manager.BeginItemUse(item, null);
        ActiveBuff buff = manager.RegisterBuffForTargetHandle(effect, Context(target.gameObject, manager, item), target);
        manager.EndItemUse(token);
        Assert.That(buff.remainUseCount, Is.EqualTo(1));
        token = manager.BeginItemUse(item, null);
        manager.EndItemUse(token);
        Assert.That(manager.HasStatus(effect.buffInfo.statusDefinition, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void ConsumeStatusDoesNotRemoveStateReappliedAfterTheCheck()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        BuffEffect effect = Status(Asset<StatusDefinition>());
        ItemEffectContext context = Context(target.gameObject, manager);
        manager.RegisterBuffForTargetHandle(effect, context, target);
        HasStatusConditionData condition = Asset<HasStatusConditionData>();
        condition.status = effect.buffInfo.statusDefinition;
        condition.target = StatusQueryTarget.Owner;
        Assert.That(condition.IsSatisfied(context), Is.True);
        manager.RegisterBuffForTargetHandle(effect, context, target);
        ConsumeStatusEffect consume = Asset<ConsumeStatusEffect>();
        consume.checkedCondition = condition;
        consume.ExecuteEffect(context);
        Assert.That(manager.HasStatus(condition.status, BuffQueryContext.ForTarget(target)), Is.True);
    }

    [Test]
    public void FrozenBranchUsesStateFromPrepareBeforeTheStateChanges()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        BuffEffect status = Status(Asset<StatusDefinition>());
        ActiveBuff buff = manager.RegisterBuffForTargetHandle(status, context, target);
        HasStatusConditionData condition = Asset<HasStatusConditionData>();
        condition.target = StatusQueryTarget.Owner;
        condition.status = status.buffInfo.statusDefinition;
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
    public void ConsumeAreaUsesTheCheckedHandleAndSkipsNaturalEndEffects()
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
        StatusDefinition key = Asset<StatusDefinition>();
        key.harmful = true;
        ActiveBuff marker = manager.RegisterStatusForTarget(key, new BuffInfo { duration = 5f },
            Context(target.gameObject, manager), target, Asset<StatusDefinition>());
        BuffRemovalReason? removed = null;
        marker.Removed += (buff, reason) => removed = reason;
        Assert.That(manager.HasStatus(key, BuffQueryContext.ForTarget(target)), Is.True);
        manager.Cleanse(BuffTargetHandle.Target(target));
        Assert.That(removed, Is.EqualTo(BuffRemovalReason.Cleansed));
        Assert.That(manager.HasStatus(key, BuffQueryContext.ForTarget(target)), Is.False);
    }

    [Test]
    public void ExpiryCallbackCanRegisterNewStatusWithoutTickingItInTheSameFrame()
    {
        BuffManager manager = Manager();
        AdditionalAreaStatusTarget target = Host().AddComponent<AdditionalAreaStatusTarget>();
        ItemEffectContext context = Context(target.gameObject, manager);
        BuffEffect initial = Status(Asset<StatusDefinition>());
        initial.buffInfo.duration = 1f;
        BuffEffect next = Status(Asset<StatusDefinition>());
        next.buffInfo.duration = 5f;
        ActiveBuff created = null;
        BuffRemovalReason? removed = null;
        ActiveBuff first = manager.RegisterBuffForTargetHandle(initial, context, target);
        first.Removed += (buff, reason) =>
        {
            removed = reason;
            created = manager.RegisterBuffForTargetHandle(next, context, target);
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
