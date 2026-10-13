using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class StatusReactionThrowTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private ItemThrowMover previousProbe, commonPrefab;
    private ItemThrowExecutor executor;
    private CombatEffectRecordingEffect record;
    private ItemEffectContext context;
    private GameObject Host()
    { var value = new GameObject("Status Throw Test"); owned.Add(value); return value; }
    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private ItemThrowMover Prefab(float time = 1f, float height = 3f)
    {
        var mover = Host().AddComponent<ItemThrowMover>(); mover.destroyOnArrive = false;
        mover.arriveTime = time; mover.autoArcHeightByDistance = false; mover.arcHeight = height;
        mover.gameObject.AddComponent<ThrowMoverTestProbe>(); return mover;
    }
    private UseItemStatusReactionModule Module()
    {
        var module = Asset<UseItemStatusReactionModule>();
        module.itemSource = StatusReactionItemSource.UsedItem; module.delivery = StatusReactionItemDelivery.ThrowAtImpact;
        return module;
    }
    private ItemThrowMover Launch(UseItemStatusReactionModule module)
    {
        ThrowMoverTestProbe.LastCreated = null;
        bool launched = module.TryReact(context,executor);
        ItemThrowMover mover = ThrowMoverTestProbe.LastCreated;
        if (mover != null) owned.Add(mover.gameObject);
        Assert.That(launched, Is.True); Assert.That(mover, Is.Not.Null); return mover;
    }
    [SetUp]
    public void Setup()
    {
        previousManager = BuffManager.instance; previousProbe = ThrowMoverTestProbe.LastCreated;
        manager = Host().AddComponent<BuffManager>(); if (manager.Storage == null) manager.SendMessage("Awake");
        var effectExecutor = Host().AddComponent<ItemEffectExecutor>(); effectExecutor.buffManager = manager;
        executor = Host().AddComponent<ItemThrowExecutor>(); executor.itemEffectExecutor = effectExecutor;
        executor.showTargetRange = false; commonPrefab = Prefab(); executor.throwMoverPrefab = commonPrefab;
        record = Asset<CombatEffectRecordingEffect>(); var item = Asset<ItemData>();
        item.effectDatas = new ItemEffectData[] { record };
        context = new ItemEffectContext(Host(),item,new Vector3(1f,2f,0f),new Vector3(5f,6f,0f),null,
            buffManager:manager,direction:Vector3.right);
    }
    [TearDown]
    public void Cleanup()
    {
        for (int i=owned.Count-1;i>=0;i--) if (owned[i]!=null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); BuffManager.instance = previousManager; ThrowMoverTestProbe.LastCreated = previousProbe;
    }
    [Test]
    public void UsedItemExecutesAtLandingWithTheOriginalOwnerAndSuppressedReactions()
    {
        var mover = Launch(Module()); Assert.That(record.calls.Count, Is.Zero);
        mover.Tick(mover.MoveDuration);
        Assert.That(record.calls.Count, Is.EqualTo(1)); Assert.That(record.calls[0].sourceItemData, Is.SameAs(context.sourceItemData));
        Assert.That(record.calls[0].owner, Is.SameAs(context.owner)); Assert.That(record.calls[0].allowItemUseReactions, Is.False);
    }
    [Test]
    public void TwoModulesUseSeparatePrefabsAndMotionsWithoutChangingTheirSharedExecutor()
    {
        var first = Module(); first.throwOptions.overrideMotion = true;
        first.throwOptions.motion.arriveTime = 0.25f; first.throwOptions.motion.autoArcHeightByDistance = false;
        first.throwOptions.motion.arcHeight = 0f; first.throwOptions.motion.spinWhileMoving = false;
        first.throwOptions.motion.faceMoveDirection = true;
        var second = Module(); var customPrefab = Prefab(1.2f,2f); customPrefab.spinSpeed = 123f;
        second.throwOptions.projectilePrefab = customPrefab;
        var a = Launch(first); var b = Launch(second);
        Assert.That(a.MoveDuration, Is.EqualTo(0.25f)); Assert.That(a.faceMoveDirection, Is.True);
        Assert.That(b.MoveDuration, Is.EqualTo(1.2f)); Assert.That(b.arcHeight, Is.EqualTo(2f)); Assert.That(b.spinSpeed, Is.EqualTo(123f));
        a.Tick(0.125f); Assert.That(Vector3.Distance(a.transform.position,new Vector3(5f,7.5f,0f)), Is.LessThan(0.0001f));
        Assert.That(executor.throwMoverPrefab, Is.SameAs(commonPrefab)); Assert.That(commonPrefab.arriveTime, Is.EqualTo(1f));
        Assert.That(commonPrefab.arcHeight, Is.EqualTo(3f)); Assert.That(customPrefab.arriveTime, Is.EqualTo(1.2f));
        Assert.That(a.arrivalTiming, Is.Not.SameAs(first.throwOptions.motion.arrivalTiming));
        a.arrivalTiming.randomOffset = 99f; Assert.That(first.throwOptions.motion.arrivalTiming.randomOffset, Is.EqualTo(0.15f));
    }
    [Test]
    public void OwnerStartAndOriginalUseTargetApplyIndependentOffsets()
    {
        context.owner.transform.position = new Vector3(10f,20f,0f);
        var module = Module(); module.throwStartAnchor = StatusReactionThrowAnchor.OwnerPosition;
        module.throwTargetAnchor = StatusReactionThrowAnchor.OriginalUsePosition;
        module.throwStartOffset = new Vector3(2f,3f,0f); module.throwTargetOffset = new Vector3(-1f,-2f,0f);
        var mover = Launch(module); Assert.That(mover.transform.position, Is.EqualTo(new Vector3(12f,23f,0f)));
        mover.Tick(mover.MoveDuration); Assert.That(record.calls[0].targetPosition, Is.EqualTo(Vector3.zero));
    }
    [Test]
    public void StationaryStateThrowPlaysItsArcBeforeExecutingAndOrdinaryThrowsKeepTheirOldValidation()
    {
        var module = Module(); module.throwStartOffset = Vector3.zero; module.throwOptions.overrideMotion = true;
        module.throwOptions.motion.arriveTime = 1f; module.throwOptions.motion.autoArcHeightByDistance = false;
        module.throwOptions.motion.arcHeight = 2f; module.throwOptions.motion.arcPeakProgress = 0.5f;
        var mover = Launch(module); mover.Tick(0.5f);
        Assert.That(mover.transform.position.y, Is.EqualTo(8f).Within(0.0001f)); Assert.That(record.calls.Count, Is.Zero);
        mover.Tick(0.5f); Assert.That(record.calls.Count, Is.EqualTo(1));
        Assert.That(executor.Throw(context.sourceItemData,context.targetPosition,context.targetPosition,context.owner,null,0), Is.False);
    }
    [Test]
    public void ParentCompletionWaitsForTheStateThrowToLand()
    {
        int completed = 0; context.lifetime = new ItemEffectLifetime(onCompleted:()=>completed++);
        var mover = Launch(Module()); context.lifetime.Close(); Assert.That(completed, Is.Zero);
        mover.Tick(mover.MoveDuration); Assert.That(completed, Is.EqualTo(1));
    }
    [Test]
    public void CancellingAStateThrowCancelsStrictParentCompletionAndNeverUsesTheItem()
    {
        int completed = 0; context.lifetime = new ItemEffectLifetime(onCompleted:()=>completed++,cancelOnChildFailure:true);
        var mover = Launch(Module()); context.lifetime.Close(); mover.gameObject.SetActive(false);
        Assert.That(context.lifetime.IsCancelled, Is.True); Assert.That(completed, Is.Zero); Assert.That(record.calls.Count, Is.Zero);
    }
}
