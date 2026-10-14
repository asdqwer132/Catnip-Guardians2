using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// 실제 SummonActor 초기화와 SummonSpawnEffect를 통해 추가 소환까지 확인합니다.
public sealed class SummonGroupTests
{
    private readonly List<Object> owned = new List<Object>();
    private readonly List<SummonActor> before = new List<SummonActor>();
    private BuffManager manager, previousManager;
    private ItemThrowMover previousProbe;
    private SummonGroupDefinition group;
    private SummonDefinition red, blue, green, king;
    private GameObject owner;
    private static readonly Vector3 origin = new Vector3(32000f,32000f,0f);
    private T Asset<T>() where T : ScriptableObject
    { var result=ScriptableObject.CreateInstance<T>();owned.Add(result);return result; }
    private GameObject Host()
    { var result=new GameObject("Summon Group Test");result.transform.position=origin;owned.Add(result);return result; }
    private SummonDefinition Kind(bool member = true)
    { var result=Asset<SummonDefinition>();if(member)result.summonGroup=group;return result; }
    private ItemEffectContext Context(GameObject actorOwner = null)
        => new ItemEffectContext(actorOwner??owner,Asset<ItemData>(),origin,origin,null,buffManager:manager,direction:Vector3.right) {
            allowItemUseReactions=false };
    private SummonActor Actor(SummonDefinition definition, GameObject actorOwner = null)
    {
        var actor=Host().AddComponent<SummonActor>();actor.definition=definition;
        actor.Initialize(Context(actorOwner),30f);return actor;
    }
    private ItemData SpawnItem(SummonDefinition definition)
    {
        var host=Host();host.SetActive(false);var prefab=host.AddComponent<SummonActor>();prefab.definition=definition;
        var spawn=Asset<SummonSpawnEffect>();spawn.summonPrefab=prefab;spawn.spawnStat.count=1;spawn.spawnStat.lifetime=30f;
        var item=Asset<ItemData>();item.effectDatas=new ItemEffectData[]{spawn};return item;
    }
    private List<SummonActor> Kings()
    {
        var result=new List<SummonActor>();
        foreach(var actor in SummonRegistry.Active)if(actor!=null&&actor.CanAct&&actor.Definition==king)result.Add(actor);
        return result;
    }
    [SetUp]
    public void Setup()
    {
        before.AddRange(SummonRegistry.Active);previousManager=BuffManager.instance;previousProbe=ThrowMoverTestProbe.LastCreated;
        manager=Host().AddComponent<BuffManager>();if(manager.Storage==null)manager.SendMessage("Awake");owner=Host();
        group=Asset<SummonGroupDefinition>();red=Kind();blue=Kind();green=Kind();king=Kind(false);
        group.itemRules=new[]{new SummonGroupItemRule{item=SpawnItem(king),requiredKinds=3,requiredInstances=3}};
    }
    [TearDown]
    public void Cleanup()
    {
        // 테스트 이전부터 존재하던 소환물과 그룹은 유지합니다.
        foreach(var actor in new List<SummonActor>(SummonRegistry.Active))
            if(actor!=null&&!before.Contains(actor))Object.DestroyImmediate(actor.gameObject);
        for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.DestroyImmediate(owned[i]);
        owned.Clear();before.Clear();BuffManager.instance=previousManager;ThrowMoverTestProbe.LastCreated=previousProbe;
        SummonGroupRuntime.NotifyFieldChanged();
    }
    [Test]
    public void ThreeRedSlimesCountAsOneKindAndDoNotMeetTheThreeKindRule()
    {
        Actor(red);Actor(red);Actor(red);var count=SummonGroupRuntime.GetPopulation(group,owner);
        Assert.That(count.kindCount,Is.EqualTo(1));Assert.That(count.instanceCount,Is.EqualTo(3));Assert.That(Kings().Count,Is.Zero);
    }
    [Test]
    public void ThreeKindsCreateOneKingAndRearmWhenTheMissingKindReturns()
    {
        Actor(red);Actor(blue);var last=Actor(green);Assert.That(Kings().Count,Is.EqualTo(1));
        Assert.That(Kings()[0].Owner,Is.SameAs(owner));Assert.That(Kings()[0].CreateContext(origin,Vector3.right).allowSummonGroupReactions,Is.False);
        Actor(red);Assert.That(Kings().Count,Is.EqualTo(1));last.Clear();Actor(green);Assert.That(Kings().Count,Is.EqualTo(2));
    }
    [Test]
    public void SameOwnerScopeDoesNotCombineDifferentOwners()
    {
        var other=Host();Actor(red);Actor(blue);Actor(green,other);Assert.That(Kings().Count,Is.Zero);
        Actor(green);Assert.That(Kings().Count,Is.EqualTo(1));Assert.That(Kings()[0].Owner,Is.SameAs(owner));
    }
    [Test]
    public void TotalInstanceRuleCanBeSatisfiedByCopiesOfOneKind()
    {
        group.itemRules[0].requirementMode=SummonGroupRequirementMode.TotalInstances;
        Actor(red);Actor(red);Actor(red);Assert.That(Kings().Count,Is.EqualTo(1));
    }
    [Test]
    public void RewardSlimeInTheSameGroupIsCountedWithoutStartingARewardLoop()
    {
        var rule=group.itemRules[0];rule.requirementMode=SummonGroupRequirementMode.TotalInstances;rule.requiredInstances=1;
        rule.item=SpawnItem(red);Actor(red);var count=SummonGroupRuntime.GetPopulation(group,owner);
        Assert.That(count.instanceCount,Is.EqualTo(2));Assert.That(count.kindCount,Is.EqualTo(1));
    }
    [Test]
    public void ThrownRewardOnlySpawnsItsKingAfterArrivalAndRetainsParentCompletion()
    {
        var rule=group.itemRules[0];rule.requiredKinds=rule.requiredInstances=1;rule.itemUse.mode=ItemUseDeliveryMode.Throw;
        var host=Host();var prefab=host.AddComponent<ItemThrowMover>();prefab.destroyOnArrive=false;
        host.AddComponent<ThrowMoverTestProbe>();rule.itemUse.throwOptions.projectilePrefab=prefab;
        ThrowMoverTestProbe.LastCreated=null;int completed=0;
        var context=Context();context.lifetime=new ItemEffectLifetime(onCompleted:()=>completed++);
        var actor=Host().AddComponent<SummonActor>();actor.definition=red;actor.Initialize(context,30f);
        var mover=ThrowMoverTestProbe.LastCreated;if(mover!=null)owned.Add(mover.gameObject);
        Assert.That(mover,Is.Not.Null);Assert.That(Kings().Count,Is.Zero);context.lifetime.Close();Assert.That(completed,Is.Zero);
        mover.Tick(mover.MoveDuration);Assert.That(Kings().Count,Is.EqualTo(1));Assert.That(completed,Is.Zero);
        actor.Despawn(true);Kings()[0].Despawn(true);Assert.That(completed,Is.EqualTo(1));
    }
    [Test]
    public void InactiveInitializationOnlyCountsWhenTheActorIsActivated()
    {
        group.itemRules[0].requiredKinds=group.itemRules[0].requiredInstances=1;
        var host=Host();host.SetActive(false);var actor=host.AddComponent<SummonActor>();actor.definition=red;
        actor.Initialize(Context(),30f);Assert.That(SummonGroupRuntime.GetPopulation(group,owner).instanceCount,Is.Zero);
        Assert.That(Kings().Count,Is.Zero);host.SetActive(true);Assert.That(Kings().Count,Is.EqualTo(1));
    }
    [Test]
    public void ReactivationWaitsForTheNewOwnerInitializationAndDoesNotUseThePreviousLife()
    {
        group.itemRules[0].requiredKinds=group.itemRules[0].requiredInstances=1;
        var actor=Actor(red);Assert.That(Kings().Count,Is.EqualTo(1));actor.gameObject.SetActive(false);actor.gameObject.SetActive(true);
        Assert.That(Kings().Count,Is.EqualTo(1));var other=Host();actor.Initialize(Context(other),30f);
        Assert.That(Kings().Count,Is.EqualTo(2));Assert.That(Kings()[1].Owner,Is.SameAs(other));
        actor.SendMessage("LateUpdate");Assert.That(Kings().Count,Is.EqualTo(2));
    }
}
