using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyPrefabBatchCreatorTests
{
    private const string Output = "Assets/__EnemyGenerationTests";
    private EnemyPrefabBatchCreator creator;

    private void Set(string field, object value)
        => typeof(EnemyPrefabBatchCreator).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(creator, value);

    private void Generate()
    {
        try
        {
            typeof(EnemyPrefabBatchCreator).GetMethod("CreateEnemyPrefabs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(creator, null);
        }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(AssetDatabase.IsValidFolder(Output), Is.False, "Test output must not overwrite existing assets.");
        AssetDatabase.CreateFolder("Assets", "__EnemyGenerationTests");
        creator = ScriptableObject.CreateInstance<EnemyPrefabBatchCreator>();
        Set("templateFolder", AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Enemy/Default"));
        Set("resourceRootFolder", AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Resources/Enemy"));
        Set("outputFolder", AssetDatabase.LoadAssetAtPath<DefaultAsset>(Output));
        Set("logFrameCount", false);
    }

    [TearDown]
    public void TearDown()
    {
        if (creator != null) Object.DestroyImmediate(creator);
        AssetDatabase.DeleteAsset(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void AllAuthoredEnemiesGenerateWithSeparateActionsAndKeepReferencesWhenRegenerated()
    {
        Generate();
        var sourceFolders = AssetDatabase.GetSubFolders("Assets/Resources/Enemy");
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { Output }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.That(prefabPaths.Length, Is.EqualTo(sourceFolders.Length));
        Assert.That(prefabPaths.Length, Is.GreaterThan(0));
        foreach (string folder in sourceFolders)
        {
            string enemyName = Path.GetFileName(folder);
            string destination = Output + "/" + enemyName;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(destination + "/" + enemyName + ".prefab");
            Assert.That(prefab, Is.Not.Null, enemyName);
            var set = AssetDatabase.LoadAssetAtPath<EnemyDataSet>(destination + "/" + enemyName + " SetData.asset");
            Assert.That(set, Is.Not.Null, enemyName);
            Assert.That(set.statData, Is.SameAs(prefab.GetComponent<Enemy>().statData));
            Assert.That(set.animatorController, Is.SameAs(prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController));
            Assert.That(set.patternData, Is.SameAs(prefab.GetComponent<EnemyPatternRunner>().patternData));
            Assert.That(AssetDatabase.GetAssetPath(set.patternData), Does.StartWith(destination));
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true).sprite, Is.Not.Null, enemyName);
            var controller = (AnimatorController)set.animatorController;
            var states = controller.layers[0].stateMachine.states;
            foreach (string name in new[] { "Idle", "Attack", "Hit", "Die", "Walk" })
            {
                var state = states.Single(s => s.state.name == name).state;
                var clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null, enemyName + ": " + name);
                Assert.That(AssetDatabase.GetAssetPath(clip), Does.StartWith(destination));
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b => b.type == typeof(SpriteRenderer));
                var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                Assert.That(frames.Length, Is.GreaterThan(0));
                Assert.That(clip.length, Is.EqualTo(frames.Length / clip.frameRate).Within(.0001f), enemyName + ": " + name);
                Assert.That(frames.All(f => f.value != null && AssetDatabase.GetAssetPath(f.value).StartsWith(folder + "/", StringComparison.Ordinal)), Is.True);
            }
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                string sheet = Path.GetFileNameWithoutExtension(path);
                if (!sheet.StartsWith(enemyName + "-", StringComparison.Ordinal)) continue;
                string action = sheet.Substring(enemyName.Length + 1);
                if (!System.Text.RegularExpressions.Regex.IsMatch(action, @"^Pattern[1-9][0-9]*$")) continue;
                var clip = states.Single(s => s.state.name == action).state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null, sheet);
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                Assert.That(AnimationUtility.GetObjectReferenceCurve(clip, binding).All(f => AssetDatabase.GetAssetPath(f.value).Replace('\\', '/') == path.Replace('\\', '/')), Is.True,
                    "Each variant/effect sheet must have its own frames.");
            }
        }

        var guids = AssetDatabase.FindAssets("", new[] { Output }).ToDictionary(AssetDatabase.GUIDToAssetPath, guid => guid);
        var sample = AssetDatabase.LoadAssetAtPath<EnemyDataSet>(Output + "/01 Werewolf/01 Werewolf SetData.asset");
        sample.statData.maxHp = 77;
        var customPattern = ScriptableObject.CreateInstance<EnemyPatternSetData>();
        AssetDatabase.CreateAsset(customPattern, Output + "/CustomPattern.asset"); sample.patternData = customPattern;
        EditorUtility.SetDirty(sample); EditorUtility.SetDirty(sample.statData); AssetDatabase.SaveAssets();
        Generate();
        foreach (var entry in guids) Assert.That(AssetDatabase.AssetPathToGUID(entry.Key), Is.EqualTo(entry.Value), entry.Key);
        Assert.That(sample.statData.maxHp, Is.EqualTo(77), "Rebuilding art must preserve authored enemy stats.");
        Assert.That(sample.patternData, Is.SameAs(customPattern));
    }

    [Test]
    public void UserNamedPatternSheetsBecomeSeparatePatternStatesWithoutMixingFrames()
    {
        AssetDatabase.CreateFolder(Output, "Input");
        AssetDatabase.CreateFolder(Output + "/Input", "01 Armored Orc");
        AssetDatabase.CreateFolder(Output, "Generated");
        string input = Output + "/Input/01 Armored Orc";
        string source = "Assets/Resources/Enemy/01 Armored Orc/01 Armored Orc";
        foreach (string action in new[] { "Idle", "Attack01", "Hit", "Die", "Walk" })
            Assert.That(AssetDatabase.CopyAsset(source + "-" + action + ".png", input + "/01 Armored Orc-" + action + ".png"), Is.True);
        Assert.That(AssetDatabase.CopyAsset(source + "-Attack02.png", input + "/01 Armored Orc-Pattern1.png"), Is.True);
        Assert.That(AssetDatabase.CopyAsset(source + "-Block.png", input + "/01 Armored Orc-Pattern2.png"), Is.True);
        Set("resourceRootFolder", AssetDatabase.LoadAssetAtPath<DefaultAsset>(Output + "/Input"));
        Set("outputFolder", AssetDatabase.LoadAssetAtPath<DefaultAsset>(Output + "/Generated"));
        Generate();
        var data = AssetDatabase.LoadAssetAtPath<EnemyDataSet>(Output + "/Generated/01 Armored Orc/01 Armored Orc SetData.asset");
        var states = ((AnimatorController)data.animatorController).layers[0].stateMachine.states;
        foreach (string stateName in new[] { "Pattern1", "Pattern2" })
        {
            var state = states.Single(s => s.state.name == stateName).state;
            var clip = (AnimationClip)state.motion;
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            Assert.That(AnimationUtility.GetObjectReferenceCurve(clip, binding)
                .All(f => AssetDatabase.GetAssetPath(f.value) == input + "/01 Armored Orc-" + stateName + ".png"), Is.True);
            Assert.That(state.transitions.Single().destinationState.name, Is.EqualTo("Idle"));
        }
    }
}
