using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CodexAuthoringTests
{
    [Test]
    public void SharedDetailsBaseRetainsAuthoredCodexEffectsAndTooltipFields()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var lists = roots.SelectMany(r => r.GetComponentsInChildren<CodexInventoryUI>(true));
            foreach (var list in lists.Where(l => l.entries.Any(e => e is ItemData || e is EnemyDataSet)))
            {
                Assert.That(list.details, Is.InstanceOf<ItemDetailsUI>());
                Assert.That(list.details.effectDescriptions, Is.Not.Null,
                    "Moving the effect field to the common base must preserve its scene reference.");
            }
            var tooltips = roots.SelectMany(r => r.GetComponentsInChildren<ItemTooltipUI>(true)).ToArray();
            Assert.That(tooltips.Length, Is.GreaterThan(0));
            Assert.That(tooltips.Any(t => t.amountText != null), Is.True,
                "Existing tooltip amount bindings must survive the base-class change.");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void CodexInventoriesReuseInventoryRenderingAndRetainAllSlotBindings()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        try
        {
            var lists = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CodexInventoryUI>(true)).ToArray();
            Assert.That(lists.Length, Is.EqualTo(5));
            foreach (var list in lists)
            {
                Assert.That(list, Is.InstanceOf<InventoryUI>());
                Assert.That(list.slotPrefab, Is.Not.Null);
                Assert.That(((InventoryUI)list).slotPrefab, Is.SameAs(list.slotPrefab.gameObject));
                Assert.That(list.slotParent, Is.Not.Null);
                Assert.That(list.details, Is.Not.Null);
            }
            var items = lists.Single(l => l.entries.Length > 0 && l.entries.All(e => e is ItemData));
            Assert.That(items.showItemCount, Is.True);
            Assert.That(items.slotPrefab.amountText, Is.Not.Null);
            Assert.That(items.slotPrefab.amountText.enabled, Is.True);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void UpgradeSceneConnectsCatalogsPortraitPagingAndStageItemDetails()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var panel = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "DictonaryPannel");
            var lists = panel.GetComponentsInChildren<CodexInventoryUI>(true);
            Assert.That(lists.Length, Is.EqualTo(5)); // Four main categories and the map's stage list.
            var top = lists.Where(i => i.entries.Length > 0).ToArray();
            Assert.That(top.Length, Is.EqualTo(4));
            foreach (var list in top)
            {
                Assert.That(list.slotPrefab, Is.Not.Null); Assert.That(list.slotParent, Is.Not.Null);
                Assert.That(list.details, Is.Not.Null); Assert.That(list.slotParent.childCount, Is.Zero);
            }
            var items = top.Single(i => i.entries.All(e => e is ItemData));
            Assert.That(items.discoveredOnly, Is.True, "The authored item catalog must hide undiscovered entries.");
            Assert.That(items.entries.Length, Is.GreaterThanOrEqualTo(330));
            Assert.That(items.entries.Cast<ItemData>().Where(i => i.effectDatas != null).SelectMany(i => i.effectDatas)
                .Where(e => e != null).All(e => e.codexDescription != null), Is.True);
            var enemies = top.Single(i => i.entries.All(e => e is EnemyDataSet));
            Assert.That(enemies.entries.Length, Is.EqualTo(11));
            Assert.That(enemies.discoveredOnly, Is.True, "The authored enemy catalog must require a recorded defeat.");
            var characters = top.Single(i => i.entries.All(e => e is CharacterCodexData));
            Assert.That(characters.entries.Length, Is.EqualTo(8));
            Assert.That(characters.details.previousPortrait, Is.Not.Null); Assert.That(characters.details.nextPortrait, Is.Not.Null);
            Assert.That(characters.details.portraitDescription, Is.Not.Null);
            foreach (var character in characters.entries.Cast<CharacterCodexData>())
                Assert.That(character.portraits.Count(p => p != null && p.sprite != null), Is.EqualTo(7));
            var maps = top.Single(i => i.entries.All(e => e is MapCodexData));
            Assert.That(maps.entries.Length, Is.EqualTo(8));
            Assert.That(maps.details.childInventory.details, Is.SameAs(items.details));
            foreach (var map in maps.entries.Cast<MapCodexData>())
            {
                Assert.That(map.stages.Length, Is.EqualTo(10));
                Assert.That(map.stages.All(s => s != null && s.map == map && s.icon != null), Is.True);
                Assert.That(map.stages.Select(s => s.stageNumber), Is.EquivalentTo(Enumerable.Range(1, 10)));
            }
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
