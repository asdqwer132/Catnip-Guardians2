using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class MergeContainerTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private MergeContainerDefinition Definition(out ItemData material, out ItemData product)
    {
        material = Asset<ItemData>(); product = Asset<ItemData>(); material.series = product.series = ItemSeries.Weapon;
        var definition = Asset<MergeContainerDefinition>(); definition.series = ItemSeries.Weapon;
        definition.injectionMaterials = new[] { material };
        definition.items = new[] { new MergeContainerItem { item = material, mergeResult = product }, new MergeContainerItem { item = product } };
        return definition;
    }
    [TearDown] public void Cleanup() { foreach (var asset in owned) if (asset != null) Object.DestroyImmediate(asset); owned.Clear(); }
    [Test] public void SameMaterialPairCreatesOneProductAndCannotBeMergedTwice()
    {
        var definition = Definition(out var material, out var product); var board = new MergeContainerBoard(definition);
        int first = board.Add(material, 10), second = board.Add(material, 10);
        Assert.That(board.TryMerge(first, second, out var result), Is.True);
        Assert.That(board.Count, Is.EqualTo(1)); Assert.That(board.Get(result).item, Is.SameAs(product));
        Assert.That(board.TryMerge(first, second, out _), Is.False);
    }
    [Test] public void SelectionProtectsTheItemUntilTheGestureIsCancelled()
    {
        var board = new MergeContainerBoard(Definition(out var material, out _));
        int first = board.Add(material, 10), second = board.Add(material, 10); board.Select(first);
        Assert.That(board.TryMerge(first, second, out _), Is.False);
        board.ClearSelection(); Assert.That(board.TryMerge(first, second, out _), Is.True);
    }
    [Test] public void APartiallyFailedBatchOnlyRemovesSuccessfullyThrownItems()
    {
        var board = new MergeContainerBoard(Definition(out var material, out _));
        int first = board.Add(material, 10), second = board.Add(material, 10); board.Select(first); board.Select(second);
        Assert.That(board.CommitThrow(first, true), Is.True); Assert.That(board.CommitThrow(second, false), Is.False);
        Assert.That(board.Count, Is.EqualTo(1)); Assert.That(board.Get(second), Is.Not.Null);
    }
    [Test] public void ABlockedInletDoesNotSpendStockAndRepeatedModeRequiresOwnership()
    {
        var board = new MergeContainerBoard(Definition(out var material, out _)); int stock = 2;
        Assert.That(MergeContainerSupply.TryInject(board, material, 10, false, MergeMaterialSupply.ConsumeStock,
            _ => stock > 0, _ => { stock--; return true; }, _ => stock++, out _), Is.False);
        Assert.That(stock, Is.EqualTo(2));
        Assert.That(MergeContainerSupply.TryInject(board, material, 10, true, MergeMaterialSupply.RepeatOwnedMaterial,
            _ => stock > 0, _ => { stock--; return true; }, _ => stock++, out _), Is.True);
        Assert.That(stock, Is.EqualTo(2));
    }
    [Test] public void SeriesUpgradeIsIndependentAndDoesNotMutateTheAsset()
    {
        var weapon = Definition(out _, out _); var food = Definition(out _, out _);
        var reward = Asset<MergeContainerUpgradeReward>(); reward.skillId = "weapon-inlet"; reward.container = weapon;
        reward.upgrade = MergeContainerUpgradeKind.InletCount; reward.amount = 1;
        var ledger = new MergeContainerUpgradeLedger(); Assert.That(ledger.Add(reward), Is.True); Assert.That(ledger.Add(reward), Is.False);
        Assert.That(ledger.Resolve(weapon).inletCount, Is.EqualTo(2)); Assert.That(ledger.Resolve(food).inletCount, Is.EqualTo(1));
        Assert.That(weapon.baseStats.inletCount, Is.EqualTo(1));
    }
    [Test] public void ClickAndDragHaveIndependentCooldownsAndClearResetsTheRound()
    {
        var board = new MergeContainerBoard(Definition(out _, out _)); board.SpendCooldown(false, 1f); board.Tick(0.5f);
        Assert.That(board.CanSelect(false), Is.False); Assert.That(board.CanSelect(true), Is.True);
        board.Clear(); Assert.That(board.CanSelect(false), Is.True);
    }
    [Test] public void InventorySnapshotsAreCopiesAndCanRestoreStockAfterASceneChange()
    {
        // 비활성 오브젝트로 만들어 씬의 기존 싱글턴을 바꾸지 않고 저장 함수 자체를 확인합니다.
        var host = new GameObject("Merge Stock Test"); host.SetActive(false); owned.Add(host);
        var inventory = host.AddComponent<InventoryManager>(); var session = host.AddComponent<MergeGameSession>();
        Definition(out var material, out _); inventory.items.Add(new InventoryItem(material, 4));
        session.SaveInventory(inventory); inventory.items[0].amount = 1;
        Assert.That(session.inventory[0].amount, Is.EqualTo(4));
        session.RestoreInventory(inventory); Assert.That(inventory.GetItemAmount(material), Is.EqualTo(4));
        inventory.items[0].amount = 2; Assert.That(session.inventory[0].amount, Is.EqualTo(4));
    }
    [Test] public void ReopeningTheSkillTreeDoesNotChargeForAnAlreadyPurchasedContainerSkill()
    {
        var oldSession = MergeGameSession.Instance; var oldCurrency = CurrencyManager.instance;
        var instanceField = typeof(MergeGameSession).GetField("<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var debugField = typeof(SkillTreeManager).GetField("debugLog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var host = new GameObject("Merge Purchase Test"); host.SetActive(false); owned.Add(host);
        try
        {
            var session = host.AddComponent<MergeGameSession>(); instanceField.SetValue(null, session);
            var currency = host.AddComponent<CurrencyManager>(); CurrencyManager.instance = currency;
            currency.AddCurrency(CurrencyType.Gold, 100);
            var node = Asset<SkillNodeData>(); node.dataId = "test-container-skill";
            node.costs = new[] { new Cost { currencyType = CurrencyType.Gold, amount = 50 } };
            var reward = Asset<MergeContainerUpgradeReward>(); reward.skillId = node.dataId; node.rewards.Add(reward);
            var first = host.AddComponent<SkillTreeManager>(); debugField.SetValue(first, false);
            Assert.That(first.UnlockSkill(node), Is.True); Assert.That(currency.GetCurrency(CurrencyType.Gold), Is.EqualTo(50));
            // 새 씬에서 새 매니저가 만들어진 상황: 로컬 해금 목록은 비었지만 세션 구매 기록은 남아 있습니다.
            var reopened = host.AddComponent<SkillTreeManager>(); debugField.SetValue(reopened, false);
            Assert.That(reopened.IsUnlocked(node.dataId), Is.True); Assert.That(reopened.UnlockSkill(node), Is.False);
            Assert.That(currency.GetCurrency(CurrencyType.Gold), Is.EqualTo(50)); Assert.That(session.upgrades.purchases.Count, Is.EqualTo(1));
        }
        finally { instanceField.SetValue(null, oldSession); CurrencyManager.instance = oldCurrency; }
    }
}
