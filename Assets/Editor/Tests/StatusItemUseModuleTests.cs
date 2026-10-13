using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// 실제 아이템 실행 경로로 검사합니다. 확률 경계만 난수를 직접 주어 재현 가능하게 만듭니다.
public sealed class StatusItemUseModuleTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private StatusManager player, previousPlayer;
    private SpecialItemManager reactions, previousReactions;
    private UseItemStatusReactionModule repeat;
    private SpecialStatusDefinition key;
    private CombatEffectRecordingEffect original;
    private ItemData mineral;

    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private GameObject Host()
    { var value = new GameObject("Status Item Use Test"); owned.Add(value); return value; }
    private ItemData Item(CombatEffectRecordingEffect effect, ItemSeries series = ItemSeries.Mineral)
    {
        var item = Asset<ItemData>(); item.series = series;
        item.effectDatas = new ItemEffectData[] { effect }; return item;
    }
    private ItemEffectContext Context()
        => new ItemEffectContext(player.gameObject, mineral, Vector3.zero, Vector3.right, null,
            buffManager: manager, direction: Vector3.right);
    private ActiveBuff Grant(BuffUseLimitType limit = BuffUseLimitType.Infinite, int uses = 2)
    {
        var grant = Asset<GrantSpecialStatusEffect>(); grant.specialStatusKey = key;
        grant.lifetimeSettings.useLimitType = limit; grant.lifetimeSettings.maxUseCount = uses;
        grant.lifetimeSettings.useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed;
        grant.Execute(Context()); return manager.Storage.activeBuffs.Find(buff => buff.specialStatusKey == key);
    }
    private void Use(ItemEffectContext parent = null)
        => ItemEffectExecutor.ExecuteItem(mineral,Vector3.zero,Vector3.right,Vector3.right,player.gameObject,null,manager,parent);

    [SetUp]
    public void SetUp()
    {
        previousManager = BuffManager.instance; previousPlayer = StatusManager.Instance;
        previousReactions = SpecialItemManager.Instance;
        manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null) manager.SendMessage("Awake");
        player = Host().AddComponent<StatusManager>(); player.buffManager = manager;
        player.SendMessage("Awake"); manager.RegisterBuffTarget(player);
        reactions = Host().AddComponent<SpecialItemManager>(); SpecialItemManager.Instance = reactions;
        reactions.statusManager = player;
        repeat = Asset<UseItemStatusReactionModule>(); repeat.allowedSeries = new[] { ItemSeries.Mineral };
        // 통합 검사에서는 100%로 설정하여 무작위 실패를 없앱니다. 실제 에셋은 10%입니다.
        repeat.probability = 1f;
        key = Asset<SpecialStatusDefinition>(); key.onItemUsedModules = new SpecialStatusItemUseModule[] { repeat };
        original = Asset<CombatEffectRecordingEffect>(); mineral = Item(original);
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count-1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); BuffManager.instance = previousManager; SpecialItemManager.Instance = previousReactions;
        typeof(StatusManager).GetProperty("Instance").SetValue(null,previousPlayer);
    }
    [Test]
    public void ActiveMineralStateAddsOneUseAndItNeverReactsToItsOwnBonus()
    {
        Use(); Assert.That(original.calls.Count, Is.EqualTo(1), "상태가 없으면 원본만 실행합니다.");
        original.calls.Clear(); Grant(); Use();
        Assert.That(original.calls.Count, Is.EqualTo(2), "100% 시험 설정: 원본 1회 + 추가 1회입니다.");
        Assert.That(original.calls[1].sourceItemData, Is.SameAs(mineral));
        Assert.That(original.calls[1].allowItemUseReactions, Is.False);
        Assert.That(original.calls[0].allowItemUseReactions, Is.True);
    }
    [Test]
    public void TenPercentSucceedsOnlyBelowTheProbabilityBoundary()
    {
        repeat.probability = 0.1f;
        Assert.That(repeat.TryReact(Context(),randomValue:()=>0.099f), Is.True);
        Assert.That(repeat.TryReact(Context(),randomValue:()=>0.1f), Is.False);
        Assert.That(original.calls.Count, Is.EqualTo(1));
    }
    [Test]
    public void AnotherSeriesDoesNotProduceABonus()
    {
        Grant(); mineral.series = ItemSeries.Food; Use(); Assert.That(original.calls.Count, Is.EqualTo(1));
    }
    [Test]
    public void FreeBonusDoesNotSpendAnotherCountAndLastOriginalUseStillReacts()
    {
        ActiveBuff state = Grant(BuffUseLimitType.UseCount,2); Use();
        Assert.That(original.calls.Count, Is.EqualTo(2)); Assert.That(state.remainUseCount, Is.EqualTo(1));
        Use(); Assert.That(original.calls.Count, Is.EqualTo(4)); Assert.That(player.HasSpecialStatus(key), Is.False);
    }
    [Test]
    public void ConfiguredItemIsExecutedInsteadOfRepeatingTheSource()
    {
        var record = Asset<CombatEffectRecordingEffect>(); var item = Item(record,ItemSeries.Weapon);
        repeat.itemSource = StatusReactionItemSource.ConfiguredItem; repeat.configuredItem = item;
        Grant(); Use(); Assert.That(original.calls.Count, Is.EqualTo(1)); Assert.That(record.calls.Count, Is.EqualTo(1));
        Assert.That(record.calls[0].sourceItemData, Is.SameAs(item));
        Assert.That(record.calls[0].owner, Is.SameAs(player.gameObject));
        Assert.That(record.calls[0].targetPosition, Is.EqualTo(Vector3.right));
    }
    [Test]
    public void StateCanCombineItemUseAndExistingEffectModulesWithoutDuplicateSlots()
    {
        var record = Asset<CombatEffectRecordingEffect>(); var effects = Asset<ExecuteEffectsStatusReactionModule>();
        effects.effects = new ItemEffectData[] { record };
        key.onItemUsedModules = new SpecialStatusItemUseModule[] { repeat,repeat,null,effects };
        Grant(); Use(); Assert.That(original.calls.Count, Is.EqualTo(2)); Assert.That(record.calls.Count, Is.EqualTo(1));
        Assert.That(record.calls[0].sourceItemData, Is.SameAs(mineral));
    }
    [Test]
    public void BonusCompletionItemCannotRestartStateModules()
    {
        var bonus = Asset<CombatEffectRecordingEffect>(); var follow = Asset<CombatEffectRecordingEffect>();
        var item = Item(bonus); item.afterCompletionItems = new[] { Item(follow) };
        item.afterCompletionTriggerSpecialItems = true;
        repeat.itemSource = StatusReactionItemSource.ConfiguredItem; repeat.configuredItem = item;
        Grant(); Use(); Assert.That(bonus.calls.Count, Is.EqualTo(1)); Assert.That(follow.calls.Count, Is.EqualTo(1));
        Assert.That(follow.calls[0].allowItemUseReactions, Is.False);
    }
    [Test]
    public void CleansedStateAndDisabledDispatcherDoNotRunModules()
    {
        ActiveBuff state = Grant(); manager.RemoveBuffHandle(state); Use(); Assert.That(original.calls.Count, Is.EqualTo(1));
        Grant(); reactions.enabled = false; Use(); Assert.That(original.calls.Count, Is.EqualTo(2));
    }
}
