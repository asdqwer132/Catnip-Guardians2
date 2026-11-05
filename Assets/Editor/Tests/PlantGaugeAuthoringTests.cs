using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlantGaugeAuthoringTests
{
    private static void AssertFill(ImageFillUI fill)
    {
        Assert.That(fill, Is.Not.Null); Assert.That(fill.fillImage, Is.Not.Null);
        Assert.That(fill.fillImage.enabled, Is.True);
        Assert.That(fill.fillImage.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(fill.fillImage.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
        Assert.That(fill.fillImage.GetComponent<Mask>(), Is.Not.Null);
        var art = fill.fillImage.transform.Find("Fill").GetComponent<Image>();
        Assert.That(art.type, Is.EqualTo(Image.Type.Sliced), "Keep the authored sliced artwork; animate its parent's mask.");
    }
    [Test]
    public void AuthoredBattleAndGeneralGaugesHaveLiveSourcesAndRenderableFillImages()
    {
        try
        {
            var battle = EditorSceneManager.OpenScene("Assets/Scenes/GameBattleScene.unity", OpenSceneMode.Single);
            var roots = battle.GetRootGameObjects();
            var sender = roots.SelectMany(r => r.GetComponentsInChildren<GeneralUIDataSender>(true)).Single();
            var data = sender.GetData();
            Assert.That(data.enemyManager, Is.Not.Null); Assert.That(data.growManager, Is.Not.Null);
            var hp = roots.SelectMany(r => r.GetComponentsInChildren<HealthBarUI>(true))
                .Single(h => h.valueSource == HealthBarValueSource.EnemyPopulation);
            Assert.That(hp.enemyManager, Is.SameAs(data.enemyManager));
            Assert.That(data.enemyManager.loseWhenFieldFull, Is.True);
            AssertFill(hp.hpFill); AssertFill(data.growManager.growthFill);
            var general = EditorSceneManager.OpenScene("Assets/Scenes/GameGeneralScene.unity", OpenSceneMode.Single);
            var receiver = general.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GeneralBattleGaugeUI>(true)).Single();
            AssertFill(receiver.hp.hpFill); AssertFill(receiver.grow);
            Assert.That(receiver.hp.valueSource, Is.EqualTo(HealthBarValueSource.EnemyPopulation));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void PlantAssetsUseNutrientTargetsAndOneVisualWithoutObsoletePurchaseOrGrowthStageFields()
    {
        var assets = AssetDatabase.FindAssets("t:PlantData").Select(g => AssetDatabase.LoadAssetAtPath<PlantData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Assert.That(assets.Length, Is.GreaterThan(0));
        foreach (var asset in assets)
        {
            Assert.That(asset.visualSprite, Is.Not.Null, asset.name);
            Assert.That(asset.nutrientsRequired, Is.GreaterThanOrEqualTo(0));
            Assert.That(asset.nutrientsPerSecond, Is.EqualTo(1));
            var serialized = new SerializedObject(asset);
            foreach (var field in new[] { "seed", "growing", "grownUp", "growTime", "buyCosts" })
                Assert.That(serialized.FindProperty(field), Is.Null, asset.name + ": " + field);
        }
    }
}
