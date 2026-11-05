using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public abstract class SkillTreeLayoutFixture
{
    protected List<Object> owned;
    protected GameObject Host(string name, Transform parent = null)
    {
        var host = new GameObject(name, typeof(RectTransform)); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    protected T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    protected SkillNodeData Data(string id)
    {
        var data = Asset<SkillNodeData>(); data.name = data.dataId = id; data.costs = Array.Empty<Cost>();
        data.skillMap = Asset<SkillMapData>(); data.skillMap.dataId = "Map " + id; return data;
    }
    protected SkillTreeLineUI Line(string name, Transform parent)
    {
        var line = Host(name, parent).AddComponent<SkillTreeLineUI>();
        line.lockedLine = Host("Lock Line", line.transform);
        line.unlockedLine = Host("Unlock Line", line.transform);
        return line;
    }
    protected SkillTreeNodePlacer Placer()
    {
        var placer = Host("Skill tree").AddComponent<SkillTreeNodePlacer>();
        placer.nodeArea = (RectTransform)Host("Nodes", placer.transform).transform;
        placer.lineArea = (RectTransform)Host("Lines", placer.transform).transform;
        placer.nodeArea.sizeDelta = placer.lineArea.sizeDelta = new Vector2(1000, 1000);
        placer.nodePrefab = Host("Node template").AddComponent<SkillNodeUI>();
        placer.horizontalLinePrefab = (RectTransform)Line("Horizontal template", null).transform;
        placer.horizontalLinePrefab.sizeDelta = new Vector2(150, 10);
        placer.verticalLinePrefab = (RectTransform)Line("Vertical template", null).transform;
        placer.verticalLinePrefab.sizeDelta = new Vector2(10, 150);
        var a = Data("A"); var b = Data("B"); var c = Data("C");
        b.requiredSkills.Add(a); c.requiredSkills.Add(b);
        placer.nodeDatas.AddRange(new[] { a, b, c });
        placer.nodePositions.AddRange(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2 });
        return placer;
    }
    protected static SkillNodeUI View(SkillTreeNodePlacer placer, SkillNodeData data)
        => placer.nodeArea.GetComponentsInChildren<SkillNodeUI>(true).Single(node => node.SkillNodeData == data);
    protected static void Set(Object obj, string field, Object value)
    {
        var serialized = new SerializedObject(obj); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    protected void Dispose()
    {
        Selection.objects = Array.Empty<Object>(); Undo.ClearAll();
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null && !EditorUtility.IsPersistent(owned[i])) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
}

public sealed class SkillTreeLayoutTests : SkillTreeLayoutFixture
{
    private string temporaryFolder;
    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        owned = new List<Object>(); temporaryFolder = "Assets/__SkillLayoutTest_" + Guid.NewGuid().ToString("N");
    }
    [TearDown]
    public void TearDown()
    {
        Dispose();
        if (AssetDatabase.IsValidFolder(temporaryFolder)) AssetDatabase.DeleteAsset(temporaryFolder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    [Test]
    public void FreeMovesAreRememberedAndRegenerationKeepsTheActualNodeAndItsPosition()
    {
        var placer = Placer(); placer.Generate(); var data = placer.nodeDatas[1];
        var node = View(placer, data); var rect = (RectTransform)node.transform;
        var connection = placer.lineArea.GetComponentInChildren<SkillTreeConnectionUI>(true);
        Selection.activeGameObject = node.gameObject;
        Undo.RecordObject(rect, "Move authored node"); rect.anchoredPosition = new Vector2(237.25f, -93.75f);
        Undo.FlushUndoRecordObjects(); SkillTreeLayoutRecorder.RememberSelection();
        Assert.That(placer.nodeLayout.Single(entry => entry.node == data).position, Is.EqualTo(rect.anchoredPosition));
        placer.Generate();
        Assert.That(View(placer, data), Is.SameAs(node));
        Assert.That(placer.lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true).Contains(connection), Is.True);
        Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(237.25f, -93.75f)));
        placer.nodeDatas.Reverse(); placer.Generate();
        Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(237.25f, -93.75f)), "Positions follow the data, not the array index.");
        var saved = placer.nodeLayout.Select(entry => entry.position).ToArray();
        placer.transform.position += new Vector3(500, 250, 0); placer.transform.localScale = Vector3.one * 2;
        placer.CaptureNodePositions(); Assert.That(placer.nodeLayout.Select(entry => entry.position), Is.EqualTo(saved));
    }
    [Test]
    public void CoordinateEditsAndTransformEditsUndoTogetherAndRedoKeepsTheConnections()
    {
        var placer = Placer(); placer.Generate(); var data = placer.nodeDatas[1]; var rect = (RectTransform)View(placer, data).transform;
        var saved = placer.nodeLayout.Single(entry => entry.node == data);
        saved.position = new Vector2(155.5f, 222.25f); placer.ApplySavedLayout();
        Assert.That(rect.anchoredPosition, Is.EqualTo(saved.position));
        Undo.ClearAll(); Undo.IncrementCurrentGroup();
        Vector2 before = rect.anchoredPosition;
        Selection.activeGameObject = rect.gameObject;
        Undo.RecordObject(rect, "Move node"); rect.anchoredPosition = new Vector2(400.5f, -120.25f);
        Undo.FlushUndoRecordObjects(); SkillTreeLayoutRecorder.RememberSelection();
        Undo.PerformUndo();
        Assert.That(rect.anchoredPosition, Is.EqualTo(before));
        Assert.That(placer.nodeLayout.Single(entry => entry.node == data).position, Is.EqualTo(before));
        Undo.PerformRedo();
        Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(400.5f, -120.25f)));
        Assert.That(placer.nodeLayout.Single(entry => entry.node == data).position, Is.EqualTo(rect.anchoredPosition));
        Assert.That(placer.lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true).All(line => line.from != null && line.to != null), Is.True);
    }
    [TestCase(SkillLineBendMode.HorizontalThenVertical)]
    [TestCase(SkillLineBendMode.VerticalThenHorizontal)]
    public void MovingAnAlignedNodeCreatesACornerWithNoGapAndKeepsBothLineStyles(SkillLineBendMode mode)
    {
        var placer = Placer(); placer.bendMode = mode; placer.Generate();
        var connection = placer.lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true).First();
        var target = (RectTransform)connection.to.transform;
        target.anchoredPosition = ((RectTransform)connection.from.transform).anchoredPosition + new Vector2(250, -150);
        connection.RefreshGeometry();
        var h = (RectTransform)connection.horizontalLine.transform; var v = (RectTransform)connection.verticalLine.transform;
        Assert.That(h.gameObject.activeSelf && v.gameObject.activeSelf, Is.True);
        Vector2 hCorner = h.anchoredPosition + Vector2.right * h.sizeDelta.x * (mode == SkillLineBendMode.HorizontalThenVertical ? .5f : -.5f);
        Vector2 vCorner = v.anchoredPosition + Vector2.up * v.sizeDelta.y * (mode == SkillLineBendMode.HorizontalThenVertical ? .5f : -.5f);
        Assert.That(Vector2.Distance(hCorner, vCorner), Is.LessThan(.001f));
        connection.SetUnlocked(true);
        Assert.That(connection.horizontalLine.unlockedLine.activeSelf, Is.True);
        Assert.That(connection.horizontalLine.lockedLine.activeSelf, Is.False);
    }
    [Test]
    public void ApplyingSeveralSavedPositionsDoesNotRecaptureNodesBeforeTheirTurn()
    {
        var placer = Placer(); placer.Generate();
        foreach (var saved in placer.nodeLayout) saved.position += new Vector2(33.25f, -71.5f);
        var expected = placer.nodeLayout.ToDictionary(entry => entry.node, entry => entry.position);
        placer.ApplySavedLayout(); Undo.FlushUndoRecordObjects();
        foreach (var pair in expected)
            Assert.That(((RectTransform)View(placer, pair.Key).transform).anchoredPosition, Is.EqualTo(pair.Value));
        placer.ApplyGridPositions(); Undo.FlushUndoRecordObjects();
        for (int i = 0; i < placer.nodeDatas.Count; i++)
            Assert.That(((RectTransform)View(placer, placer.nodeDatas[i]).transform).anchoredPosition, Is.EqualTo(new Vector2(i * 180, 0)));
    }
    [Test]
    public void PrefabSaveReloadAndClearGenerateRoundTripKeepFreePositions()
    {
        var placer = Placer(); placer.Generate(); var data = placer.nodeDatas[1];
        ((RectTransform)View(placer, data).transform).anchoredPosition = new Vector2(51.5f, 73.25f);
        placer.CaptureNodePositions();
        placer.ClearGeneratedObjects(); Assert.That(placer.nodeArea.childCount, Is.Zero);
        placer.Generate();
        Assert.That(((RectTransform)View(placer, data).transform).anchoredPosition, Is.EqualTo(new Vector2(51.5f, 73.25f)));
        AssetDatabase.CreateFolder("Assets", temporaryFolder.Substring("Assets/".Length));
        foreach (var node in placer.nodeDatas)
        {
            AssetDatabase.CreateAsset(node.skillMap, temporaryFolder + "/Map_" + node.name + ".asset");
            AssetDatabase.CreateAsset(node, temporaryFolder + "/" + node.name + ".asset");
        }
        placer.nodePrefab = PrefabUtility.SaveAsPrefabAsset(placer.nodePrefab.gameObject, temporaryFolder + "/Node.prefab").GetComponent<SkillNodeUI>();
        placer.horizontalLinePrefab = PrefabUtility.SaveAsPrefabAsset(placer.horizontalLinePrefab.gameObject, temporaryFolder + "/Horizontal.prefab").GetComponent<RectTransform>();
        placer.verticalLinePrefab = PrefabUtility.SaveAsPrefabAsset(placer.verticalLinePrefab.gameObject, temporaryFolder + "/Vertical.prefab").GetComponent<RectTransform>();
        var path = temporaryFolder + "/Tree.prefab";
        PrefabUtility.SaveAsPrefabAsset(placer.gameObject, path);
        var copy = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var restored = copy.GetComponent<SkillTreeNodePlacer>();
            Assert.That(restored.nodeLayout.Single(entry => entry.node == data).position, Is.EqualTo(new Vector2(51.5f, 73.25f)));
            Assert.That(((RectTransform)View(restored, data).transform).anchoredPosition, Is.EqualTo(new Vector2(51.5f, 73.25f)));
        }
        finally { PrefabUtility.UnloadPrefabContents(copy); }
    }
}

public sealed class SkillTreeLayoutAuthoringTests
{
    [Test]
    public void ExistingSkillGraphsHaveSavedFreePositionsSeparateLineStatesAndExplicitNodeSprites()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        var placers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SkillTreeNodePlacer>(true)).ToArray();
        Assert.That(placers.Length, Is.GreaterThan(0));
        foreach (var placer in placers)
        {
            var nodes = placer.nodeArea.GetComponentsInChildren<SkillNodeUI>(true);
            foreach (var node in nodes)
            {
                if (node.SkillNodeData == null || !placer.nodeDatas.Contains(node.SkillNodeData)) continue;
                var saved = placer.nodeLayout.Single(entry => entry.node == node.SkillNodeData);
                Vector2 position = (Vector2)placer.nodeArea.InverseTransformPoint(node.transform.position) - placer.nodeArea.rect.center;
                Assert.That(Vector2.Distance(saved.position, position), Is.LessThan(.01f));
                var serialized = new SerializedObject(node);
                Assert.That(serialized.FindProperty("iconImage").objectReferenceValue != null, Is.True);
            }
            foreach (var connection in placer.lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true))
            {
                Assert.That(connection.from != null && connection.to != null, Is.True);
                Assert.That(connection.from.Lines.Contains(connection.gameObject), Is.True);
                foreach (var line in new[] { connection.horizontalLine, connection.verticalLine })
                {
                    Assert.That(line != null && line.lockedLine != null && line.unlockedLine != null, Is.True);
                    Assert.That(line.lockedLine != line.unlockedLine, Is.True);
                }
            }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}

public sealed class SkillTreeStateTests : SkillTreeLayoutFixture
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
    }
    [UnityTearDown]
    public IEnumerator TearDown() { Dispose(); yield return new ExitPlayMode(); }
    private Sprite Sprite()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite); return sprite;
    }
    [Test]
    public void EnabledTreeBindsLateManagerAndUpdatesIconsWithoutManualInitialization()
    {
        var root = Host("Tree"); root.SetActive(false);
        root.AddComponent<SkillTreeUI>();
        var node = Host("Node", root.transform).AddComponent<SkillNodeUI>();
        var image = node.gameObject.AddComponent<Image>(); Set(node, "iconImage", image);
        var data = Data("Late manager node"); data.icon = Sprite(); data.unlockedIcon = Sprite();
        node.SetData(data);
        root.SetActive(true);
        Host("Wallet").AddComponent<CurrencyManager>();
        var manager = Host("Late manager").AddComponent<SkillTreeManager>();
        Assert.That(image.sprite, Is.SameAs(data.icon));
        Assert.That(manager.UnlockSkill(data), Is.True);
        Assert.That(image.sprite, Is.SameAs(data.unlockedIcon));
        manager.ClearAllSkills();
        Assert.That(image.sprite, Is.SameAs(data.icon));
        data.unlockedIcon = null;
        manager.RestoreUnlockedSkills(new[] { data });
        Assert.That(image.sprite, Is.SameAs(data.icon));
    }

    [Test]
    public void LateWalletRefreshesTheBuyButtonWithoutReinitializingTheSkillTree()
    {
        var tree = Host("Manager").AddComponent<SkillTreeManager>();
        var root = Host("Tree"); var ui = root.AddComponent<SkillTreeUI>();
        var node = Host("Node", root.transform).AddComponent<SkillNodeUI>();
        var button = node.gameObject.AddComponent<Button>(); Set(node, "button", button);
        node.SetData(Data("Root"), null); ui.Init();
        Assert.That(button.interactable, Is.False);
        Host("Late wallet").AddComponent<CurrencyManager>();
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(tree.IsUnlocked(node.SkillNodeData.dataId), Is.True);
    }
    [Test]
    public void UnlockResetAndReenableSwitchNodeSpritesAndSourceOwnedLinesWithoutNullErrors()
    {
        var root = Host("Skill tree"); var ui = root.AddComponent<SkillTreeUI>();
        Host("Wallet").AddComponent<CurrencyManager>(); var tree = Host("Manager").AddComponent<SkillTreeManager>();
        var setting = new SerializedObject(tree); setting.FindProperty("debugLog").boolValue = false; setting.ApplyModifiedPropertiesWithoutUndo();
        var a = Data("A"); var b = Data("B"); b.requiredSkills.Add(a);
        var source = Host("A", root.transform).AddComponent<SkillNodeUI>();
        var target = Host("B", root.transform).AddComponent<SkillNodeUI>();
        var locked = Sprite(); var unlocked = Sprite();
        a.icon = b.icon = locked; a.unlockedIcon = b.unlockedIcon = unlocked;
        foreach (var node in new[] { source, target })
        {
            var image = Host("Frame", node.transform).AddComponent<Image>();
            Set(node, "iconImage", image);
            Set(node, "nodeRootObject", image.gameObject);
        }
        var connection = Host("Connection", root.transform).AddComponent<SkillTreeConnectionUI>();
        connection.from = source; connection.to = target;
        connection.horizontalLine = Line("Horizontal", connection.transform); connection.verticalLine = Line("Vertical", connection.transform);
        source.SetData(a, new[] { connection.gameObject, null }); target.SetData(b, null);
        ui.Init();
        var sourceFrame = source.GetComponentInChildren<Image>(true); var targetFrame = target.GetComponentInChildren<Image>(true);
        Assert.That(sourceFrame.sprite, Is.SameAs(locked)); Assert.That(targetFrame.gameObject.activeSelf, Is.True);
        Assert.That(tree.UnlockSkill(a), Is.True);
        Assert.That(sourceFrame.sprite, Is.SameAs(unlocked)); Assert.That(targetFrame.sprite, Is.SameAs(locked));
        Assert.That(connection.horizontalLine.unlockedLine.activeSelf, Is.True);
        Assert.That(connection.horizontalLine.lockedLine.activeSelf, Is.False);
        root.SetActive(false); tree.ClearAllSkills(); root.SetActive(true);
        Assert.That(sourceFrame.sprite, Is.SameAs(locked)); Assert.That(connection.horizontalLine.lockedLine.activeSelf, Is.True);
        tree.RestoreUnlockedSkills(new[] { a, b });
        Assert.That(targetFrame.sprite, Is.SameAs(unlocked));
        Assert.DoesNotThrow(target.Refresh);
    }
}
