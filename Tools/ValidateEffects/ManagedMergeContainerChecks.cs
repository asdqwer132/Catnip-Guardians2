using System;
using UnityEngine;

public static partial class Runner
{
    private static MergeContainerDefinition MergeDefinition(out ItemData material, out ItemData product)
    {
        material = new ItemData { series = ItemSeries.Weapon }; product = new ItemData { series = ItemSeries.Weapon };
        return new MergeContainerDefinition { series = ItemSeries.Weapon, injectionMaterials = new[] { material },
            items = new[] { new MergeContainerItem { item = material, mergeResult = product }, new MergeContainerItem { item = product } } };
    }
    private static void RunMergeContainerChecks()
    {
        Check("merge pair is replaced atomically and duplicate collision callbacks cannot merge it again", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out var product));
            int a = board.Add(material, 8), b = board.Add(material, 8);
            True(board.TryMerge(a, b, out int next) && board.Count == 1 && board.Get(next).item == product);
            True(!board.TryMerge(a, b, out _) && board.Count == 1);
        });
        Check("different items terminal items and the same token cannot merge", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out var product));
            int a = board.Add(material, 8), b = board.Add(product, 8), c = board.Add(product, 8);
            True(!board.TryMerge(a, b, out _) && !board.TryMerge(a, a, out _) && !board.TryMerge(b, c, out _) && board.Count == 3);
        });
        Check("unregistered and cross series merge results are rejected without spending the pair", () => {
            var definition = MergeDefinition(out var material, out var product); var board = new MergeContainerBoard(definition);
            int a = board.Add(material, 8), b = board.Add(material, 8); product.series = ItemSeries.Food;
            True(!board.TryMerge(a, b, out _) && board.Count == 2); product.series = ItemSeries.Weapon;
            definition.items[0].mergeResult = new ItemData { series = ItemSeries.Weapon };
            True(!board.TryMerge(a, b, out _) && board.Count == 2);
        });
        Check("selected tokens cannot merge until selection is cancelled", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out _));
            int a = board.Add(material, 8), b = board.Add(material, 8); board.Select(a);
            True(!board.TryMerge(a, b, out _) && board.SelectedIds().Count == 1);
            board.ClearSelection(); True(board.TryMerge(a, b, out _));
        });
        Check("a partial throw batch keeps failed tokens and never removes a successful token twice", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out _));
            int a = board.Add(material, 8), b = board.Add(material, 8); board.Select(a); board.Select(b);
            True(board.CommitThrow(a, true) && !board.CommitThrow(a, true));
            True(!board.CommitThrow(b, false) && board.Count == 1 && board.Get(b).item == material);
            board.ClearSelection(); True(!board.CommitThrow(b, true));
        });
        Check("clearing a round invalidates old token ids without affecting another series board", () => {
            var definition = MergeDefinition(out var material, out _); var a = new MergeContainerBoard(definition); var b = new MergeContainerBoard(definition);
            int old = a.Add(material, 8); b.Add(material, 8); a.Clear();
            True(a.Add(material, 8) > old && a.Get(old) == null && b.Count == 1);
        });
        Check("stock injection consumes exactly one owned material for one token", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out _)); int stock = 2;
            True(MergeContainerSupply.TryInject(board, material, 8, true, MergeMaterialSupply.ConsumeStock,
                _ => stock > 0, _ => { stock--; return true; }, _ => stock++, out int id));
            True(stock == 1 && board.Count == 1 && board.Get(id).item == material);
        });
        Check("blocked full and unregistered inlets never consume inventory", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out var product)); int consumed = 0;
            Func<ItemData,bool> consume = _ => { consumed++; return true; };
            True(!MergeContainerSupply.TryInject(board, material, 8, false, MergeMaterialSupply.ConsumeStock, _ => true, consume, null, out _));
            True(!MergeContainerSupply.TryInject(board, material, 0, true, MergeMaterialSupply.ConsumeStock, _ => true, consume, null, out _));
            True(!MergeContainerSupply.TryInject(board, product, 8, true, MergeMaterialSupply.ConsumeStock, _ => true, consume, null, out _));
            True(consumed == 0 && board.Count == 0);
        });
        Check("repeated supply still requires ownership but does not consume the owned material", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out _)); int consumed = 0;
            Func<ItemData,bool> consume = _ => { consumed++; return true; };
            True(!MergeContainerSupply.TryInject(board, material, 8, true, MergeMaterialSupply.RepeatOwnedMaterial, _ => false, consume, null, out _));
            True(MergeContainerSupply.TryInject(board, material, 8, true, MergeMaterialSupply.RepeatOwnedMaterial, _ => true, consume, null, out _));
            True(consumed == 0 && board.Count == 1);
        });
        Check("an acquisition race cannot create an unowned token and a failed add refunds stock", () => {
            var board = new MergeContainerBoard(MergeDefinition(out var material, out _)); int refunds = 0;
            True(!MergeContainerSupply.TryInject(board, material, 1, true, MergeMaterialSupply.ConsumeStock, _ => true, _ => false, null, out _));
            True(board.Count == 0);
            True(!MergeContainerSupply.TryInject(board, material, 1, true, MergeMaterialSupply.ConsumeStock, _ => true,
                _ => { board.Add(material, 1); return true; }, _ => refunds++, out _));
            True(refunds == 1 && board.Count == 1);
        });
        Check("click and drag cooldowns tick independently and ignore invalid frame deltas", () => {
            var board = new MergeContainerBoard(MergeDefinition(out _, out _)); board.SpendCooldown(false, 1f); board.SpendCooldown(true, 2f);
            board.Tick(0f); board.Tick(-1f); board.Tick(float.NaN); board.Tick(float.PositiveInfinity);
            Eq(board.Cooldown(false), 1f); board.Tick(1f); True(board.CanSelect(false) && !board.CanSelect(true));
            board.Clear(); True(board.CanSelect(true));
        });
        Check("series upgrades apply once and do not mutate the definition or another series", () => {
            var weapon = MergeDefinition(out _, out _); var food = MergeDefinition(out _, out _);
            var reward = new MergeContainerUpgradeReward { skillId = "weapon-inlet", container = weapon, upgrade = MergeContainerUpgradeKind.InletCount, amount = 1f };
            var ledger = new MergeContainerUpgradeLedger(); True(ledger.Add(reward) && !ledger.Add(reward));
            True(ledger.Resolve(weapon).inletCount == 2 && ledger.Resolve(food).inletCount == 1 && weapon.baseStats.inletCount == 1);
        });
        Check("all six container upgrade kinds change their own functional settings", () => {
            var definition = MergeDefinition(out _, out _); var ledger = new MergeContainerUpgradeLedger();
            foreach (MergeContainerUpgradeKind kind in Enum.GetValues(typeof(MergeContainerUpgradeKind)))
                ledger.Add(new MergeContainerUpgradeReward { skillId = kind.ToString(), upgrade = kind, amount = kind == MergeContainerUpgradeKind.InletCount ? 1f : 0.5f });
            var stat = ledger.Resolve(definition); Eq(stat.width, 9f); Eq(stat.height, 12f); Eq(stat.injectionInterval, 1f);
            True(stat.inletCount == 2); Eq(stat.dragRadius, 1.1f); Eq(stat.clickCooldown, 0.15f); Eq(stat.dragCooldown, 0.5f);
        });
        Check("container reward replay is idempotent and can be removed by its skill or map", () => {
            var old = MergeGameSession.Instance; MergeGameSession.Instance = new MergeGameSession();
            try {
                var reward = new MergeContainerUpgradeReward { skillId = "skill-1", skillMapId = "map", amount = 1f };
                reward.Apply(null); reward.Apply(null); True(MergeGameSession.Instance.upgrades.purchases.Count == 1);
                True(MergeGameSession.Instance.upgrades.InMap("map").Contains("skill-1")); reward.Remove(null);
                True(!MergeGameSession.Instance.upgrades.Has("skill-1")); reward.Apply(null); MergeGameSession.Instance.upgrades.ClearMap("map");
                True(MergeGameSession.Instance.upgrades.purchases.Count == 0);
            } finally { MergeGameSession.Instance = old; }
        });
        Check("invalid container base stats and extreme upgrades are bounded on runtime copies", () => {
            var definition = MergeDefinition(out _, out _); definition.baseStats.width = float.NaN; definition.baseStats.dragCooldown = -5f;
            var ledger = new MergeContainerUpgradeLedger(); ledger.Add(new MergeContainerUpgradeReward { skillId = "many", upgrade = MergeContainerUpgradeKind.InletCount, amount = 100f });
            var stat = ledger.Resolve(definition); Eq(stat.width, 6f); Eq(stat.dragCooldown, 0f); True(stat.inletCount == 8);
            True(float.IsNaN(definition.baseStats.width) && definition.baseStats.dragCooldown == -5f);
        });
    }
}
