using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatEffectTests
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
        GameObject host = new GameObject("CombatEffectTest");
        owned.Add(host);
        return host;
    }
    private ItemData Item(CombatEffectRecordingEffect effect)
    {
        ItemData item = Asset<ItemData>();
        item.effectDatas = new ItemEffectData[] { effect };
        return item;
    }
    private ItemEffectContext Context(ItemData item = null, GameObject owner = null)
        => new ItemEffectContext(owner, item, Vector3.zero, Vector3.right, null, direction: Vector3.right);

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void ShotgunUsesIndependentAnglesInsideConeAtFixedDistance()
    {
        Random.State before = Random.state;
        try
        {
            Random.InitState(147);
            Vector3 first = AttackPlacement.Position(AttackPlacementMode.Shotgun, Vector3.zero, Vector3.right, 0, 32, 0f, 0f, 5f, 30f);
            bool different = false;
            for (int i = 1; i < 32; i++)
            {
                Vector3 point = AttackPlacement.Position(AttackPlacementMode.Shotgun, Vector3.zero, Vector3.right, i, 32, 0f, 0f, 5f, 30f);
                Assert.That(point.magnitude, Is.EqualTo(5f).Within(0.0001f));
                Assert.That(Vector3.Angle(Vector3.right, point), Is.LessThanOrEqualTo(15.0001f));
                different |= (point - first).sqrMagnitude > 0.0001f;
            }
            Assert.That(different, Is.True);
        }
        finally { Random.state = before; }
    }

    [Test]
    public void FullCircleDoesNotOverlapFirstAndLastPoint()
    {
        Vector3 first = AttackPlacement.Position(AttackPlacementMode.CircleEven, Vector3.zero, Vector3.right, 0, 4, 0f, 0f, 2f, 360f);
        Vector3 last = AttackPlacement.Position(AttackPlacementMode.CircleEven, Vector3.zero, Vector3.right, 3, 4, 0f, 0f, 2f, 360f);
        Assert.That((first - last).sqrMagnitude, Is.GreaterThan(0.01f));
    }

    [Test]
    public void ForwardPlacementUsesBothOffsets()
    {
        Vector3 point = AttackPlacement.Position(AttackPlacementMode.Forward, Vector3.one, Vector3.right, 3, 4, 2f, 1f, 0f, 0f);
        Assert.That(point.x, Is.EqualTo(7f));
        Assert.That(point.y, Is.EqualTo(-2f));
    }

    [Test]
    public void RandomCircleStaysInsideRadius()
    {
        for (int i = 0; i < 64; i++)
            Assert.That(AttackPlacement.Position(AttackPlacementMode.CircleRandom, Vector3.zero, Vector3.right, i, 64, 0f, 0f, 2f, 360f).magnitude,
                Is.LessThanOrEqualTo(2.0001f));
    }

    [Test]
    public void LegacyItemsConvertWithoutLosingDuplicatesOrInterval()
    {
        ItemData item = Item(Asset<CombatEffectRecordingEffect>());
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.repeatStat.itemRepeatInterval = 0.7f;
        effect.items = new[] { item, item };
        effect.ConvertLegacyItemsToSteps();
        effect.steps[0].ConvertLegacyItems();
        effect.steps[1].ConvertLegacyItems();
        Assert.That(effect.steps.Length, Is.EqualTo(2));
        Assert.That(effect.steps[0].entries[0].item, Is.SameAs(item));
        Assert.That(effect.steps[1].entries[0].item, Is.SameAs(item));
        Assert.That(effect.steps[1].intervalAfter, Is.EqualTo(0.7f));
    }

    [Test]
    public void RuntimeStepCopiesEntriesAndRetainsEffectOnlySteps()
    {
        ItemData item = Item(Asset<CombatEffectRecordingEffect>());
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.steps = new[] {
            new RepeatItemStep { entries = new[] { new RepeatItemEntry { item = item, count = 6 } }, intervalAfter = 0.8f },
            new RepeatItemStep { onImpactEffects = new ItemEffectData[] { Asset<CombatEffectRecordingEffect>() }, effectOnlyCount = 3 }
        };
        RepeatItemStep[] copy = effect.CreateRuntimeSteps();
        Assert.That(copy.Length, Is.EqualTo(2));
        Assert.That(copy[0].entries[0].count, Is.EqualTo(6));
        Assert.That(copy[1].effectOnlyCount, Is.EqualTo(3));
        copy[0].entries[0].count = 2;
        Assert.That(effect.steps[0].entries[0].count, Is.EqualTo(6));
    }

    [Test]
    public void InvalidEntriesDoNotDropAnEffectOnlyPayload()
    {
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.steps = new[] { new RepeatItemStep { entries = new[] { new RepeatItemEntry() }, onImpactEffects = new ItemEffectData[] { Asset<CombatEffectRecordingEffect>() } } };
        Assert.That(effect.CreateRuntimeSteps().Length, Is.EqualTo(1));
        Assert.That(effect.CreateRuntimeSteps()[0].entries.Length, Is.EqualTo(0));
    }

    [Test]
    public void MultiItemStepUsesEachCountAndDifferentScatterPositions()
    {
        CombatEffectRecordingEffect hits = Asset<CombatEffectRecordingEffect>();
        ItemData item = Item(hits);
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.repeatStat.itemRepeatCount = 2; // 첫 스텝만 즉시 실행한다. EditMode에서 자연 종료는 호출하지 않는다.
        effect.repeatStat.itemRepeatInterval = 0;
        effect.throwItems = false;
        effect.placement = AttackPlacementMode.CircleEven;
        effect.steps = new[] { new RepeatItemStep { entries = new[] {
            new RepeatItemEntry { item = item, count = 2 }, new RepeatItemEntry { item = item, count = 3 }
        } } };
        Host().AddComponent<RepeatItemRunner>().Init(effect, Context());
        Assert.That(hits.calls.Count, Is.EqualTo(5));
        Assert.That((hits.calls[0].targetPosition - hits.calls[1].targetPosition).sqrMagnitude, Is.GreaterThan(0.01f));
    }

    [Test]
    public void EffectOnlyStepCanReplaceSequenceImpactPayload()
    {
        CombatEffectRecordingEffect hits = Asset<CombatEffectRecordingEffect>();
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.repeatStat.itemRepeatCount = 2;
        effect.repeatStat.itemRepeatInterval = 0;
        effect.throwItems = false;
        effect.steps = new[] { new RepeatItemStep { effectOnlyCount = 4, onImpactEffects = new ItemEffectData[] { hits } } };
        Host().AddComponent<RepeatItemRunner>().Init(effect, Context());
        Assert.That(hits.calls.Count, Is.EqualTo(4));
    }

    private ReactiveGroundArea Ground(CombatEffectRecordingEffect defaults, CombatEffectRecordingEffect specials,
        ItemData source, bool replace = false, ItemData[] accepted = null, GameObject owner = null)
    {
        ReactiveGroundEffect effect = Asset<ReactiveGroundEffect>();
        effect.defaultEffects = new ItemEffectData[] { defaults };
        effect.specialEffects = new ItemEffectData[] { specials };
        effect.acceptedItems = accepted;
        effect.replaceIncomingItemEffects = replace;
        effect.groundStat.groundReactionCooldown = 0;
        ReactiveGroundArea area = Host().AddComponent<ReactiveGroundArea>();
        area.Init(effect, Context(source, owner));
        return area;
    }

    [Test]
    public void GroundRunsDefaultThenReactsOnlyToThrownItem()
    {
        CombatEffectRecordingEffect defaults = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect specials = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect incoming = Asset<CombatEffectRecordingEffect>();
        ItemData item = Item(incoming);
        Ground(defaults, specials, item);
        Assert.That(defaults.calls.Count, Is.EqualTo(1));
        ItemEffectExecutor.ExecuteItem(item, Vector3.zero, Vector3.zero, Vector3.right, null, null, null, triggerSpecialItems: false);
        Assert.That(specials.calls.Count, Is.EqualTo(0));
        ItemEffectExecutor.ExecuteItem(item, Vector3.zero, Vector3.zero, Vector3.right, null, null, null, triggerSpecialItems: false, isThrownItem: true);
        Assert.That(specials.calls.Count, Is.EqualTo(1));
        Assert.That(incoming.calls.Count, Is.EqualTo(2));
    }

    [Test]
    public void GroundFilterAndReplacementUseIncomingItemButKeepCreatorSource()
    {
        CombatEffectRecordingEffect defaults = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect specials = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect incoming = Asset<CombatEffectRecordingEffect>();
        ItemData item = Item(incoming), creator = Item(defaults);
        Ground(defaults, specials, creator, true, new[] { item });
        ItemEffectExecutor.ExecuteItem(creator, Vector3.zero, Vector3.zero, Vector3.right, null, null, null, triggerSpecialItems: false, isThrownItem: true);
        Assert.That(specials.calls.Count, Is.EqualTo(0));
        ItemEffectExecutor.ExecuteItem(item, Vector3.zero, Vector3.zero, Vector3.right, null, null, null, triggerSpecialItems: false, isThrownItem: true);
        Assert.That(specials.calls.Count, Is.EqualTo(1));
        Assert.That(incoming.calls.Count, Is.EqualTo(0));
        Assert.That(specials.calls[0].sourceItemData, Is.SameAs(creator));
    }

    [Test]
    public void GroundRejectsOtherOwnerAndOutsideLanding()
    {
        CombatEffectRecordingEffect defaults = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect specials = Asset<CombatEffectRecordingEffect>();
        ItemData item = Item(defaults);
        GameObject owner = Host();
        Ground(defaults, specials, item, owner: owner);
        Assert.That(ReactiveGroundArea.NotifyItemLanded(Context(item)), Is.False);
        ItemEffectContext outside = Context(item, owner);
        outside.targetPosition = Vector3.right * 20f;
        ReactiveGroundArea.NotifyItemLanded(outside);
        Assert.That(specials.calls.Count, Is.EqualTo(0));
    }

    // 테스트 중 생성된 지면 오브젝트만 추적한다.
    // Resources.FindObjectsOfTypeAll은 비활성 객체와 에셋까지 포함하므로 씬에 속한 것만 남긴다.
    private static HashSet<ReactiveGroundArea> SnapshotSceneGroundAreas()
    {
        HashSet<ReactiveGroundArea> result = new HashSet<ReactiveGroundArea>();
        foreach (ReactiveGroundArea area in Resources.FindObjectsOfTypeAll<ReactiveGroundArea>())
        {
            if (area == null)
                continue;

            GameObject obj = area.gameObject;
            if (obj.scene.IsValid() && obj.scene.isLoaded)
                result.Add(area);
        }
        return result;
    }

    private void TrackNewSceneGroundAreas(HashSet<ReactiveGroundArea> before)
    {
        foreach (ReactiveGroundArea area in SnapshotSceneGroundAreas())
        {
            if (!before.Contains(area) && !owned.Contains(area.gameObject))
                owned.Add(area.gameObject);
        }
    }

    [Test]
    public void GroundCreatedByLandingDoesNotReactToThatSameLanding()
    {
        CombatEffectRecordingEffect defaults = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect specials = Asset<CombatEffectRecordingEffect>();
        ReactiveGroundEffect ground = Asset<ReactiveGroundEffect>();
        ground.defaultEffects = new ItemEffectData[] { defaults };
        ground.specialEffects = new ItemEffectData[] { specials };
        ItemData item = Asset<ItemData>();
        item.effectDatas = new ItemEffectData[] { ground };
        HashSet<ReactiveGroundArea> existing = SnapshotSceneGroundAreas();
        try
        {
            ItemEffectExecutor.ExecuteItem(item, Vector3.zero, Vector3.zero, Vector3.right,
                null, null, null, triggerSpecialItems: false, isThrownItem: true);
        }
        finally
        {
            // 실행 중 예외가 발생하더라도 생성된 테스트 오브젝트를 TearDown에서 정리한다.
            TrackNewSceneGroundAreas(existing);
        }
        Assert.That(defaults.calls.Count, Is.EqualTo(1));
        Assert.That(specials.calls.Count, Is.EqualTo(0));
    }

    [Test]
    public void EffectlessThrownItemCanTriggerGround()
    {
        CombatEffectRecordingEffect defaults = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect specials = Asset<CombatEffectRecordingEffect>();
        ItemData catalyst = Asset<ItemData>();
        Ground(defaults, specials, Item(defaults), accepted: new[] { catalyst });
        ItemEffectExecutor.ExecuteItem(catalyst, Vector3.zero, Vector3.zero, Vector3.right,
            null, null, null, triggerSpecialItems: false, isThrownItem: true);
        Assert.That(specials.calls.Count, Is.EqualTo(1));
    }

    [Test]
    public void StepsCanThrowEffectlessGroundCatalysts()
    {
        ItemData catalyst = Asset<ItemData>();
        RepeatItemEffect effect = Asset<RepeatItemEffect>();
        effect.steps = new[] { new RepeatItemStep { entries = new[] { new RepeatItemEntry { item = catalyst, count = 2 } } } };
        Assert.That(effect.CreateRuntimeSteps()[0].entries[0].count, Is.EqualTo(2));
    }

    [Test]
    public void OrbitModuleHasIndependentStateForEachSummon()
    {
        SummonOrbitModule module = Asset<SummonOrbitModule>();
        module.center = SummonOrbitCenter.SpawnPosition;
        module.startFromSpawnAngle = false;
        module.orbitRadius = 2f;
        module.angularSpeed = 90f;
        SummonItemThrower a = Host().AddComponent<SummonItemThrower>();
        SummonItemThrower b = Host().AddComponent<SummonItemThrower>();
        SummonBehaviourRuntime first = module.CreateRuntime(a), second = module.CreateRuntime(b);
        first.Tick(1f);
        second.Tick(0f);
        Assert.That(a.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(a.transform.position.y, Is.EqualTo(2f).Within(0.0001f));
        Assert.That(b.transform.position.x, Is.EqualTo(2f).Within(0.0001f));
        first.Dispose(); second.Dispose();
    }

    [Test]
    public void StatClampsHandleNaNAndSnapshotsAreIndependent()
    {
        RepeatItemStat repeat = new RepeatItemStat { itemRepeatFlightTime = float.NaN, itemRepeatBombLifetime = -1f };
        repeat.Clamp();
        Assert.That(repeat.itemRepeatFlightTime, Is.EqualTo(0.3f));
        Assert.That(repeat.itemRepeatBombLifetime, Is.GreaterThan(0f));
        ReactiveGroundStat ground = new ReactiveGroundStat { groundRadius = float.PositiveInfinity };
        ground.Clamp();
        Assert.That(ground.groundRadius, Is.EqualTo(2f));
        SummonStat summon = new SummonStat { summonThrowInterval = float.NaN, summonLifeTime = -1f };
        summon.Clamp();
        Assert.That(summon.summonThrowInterval, Is.EqualTo(0.5f));
        SummonStat copy = summon.Clone(); copy.summonAttackPower = 100f;
        Assert.That(summon.summonAttackPower, Is.EqualTo(0f));
    }
}
