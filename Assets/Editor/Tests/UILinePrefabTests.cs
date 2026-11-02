using Catnip.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class UILinePrefabTests
{
    private GameObject root, prefab;
    private RectTransform from, to, parent;
    private UILinePrefabSettings settings;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("UI line test", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        from = Rect("From", new Vector2(-100f, 0f));
        to = Rect("To", new Vector2(100f, 100f));
        parent = Rect("Lines", Vector2.zero);
        settings = new UILinePrefabSettings();
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/SkillTree/UI/LineHor.prefab");
        Assert.That(prefab, Is.Not.Null);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    private RectTransform Rect(string name, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(root.transform, false);
        rect.sizeDelta = new Vector2(40f, 20f);
        rect.anchoredPosition = position;
        return rect;
    }

    private RectTransform Create() => UILinePrefabUtility.Create(from, to, parent, prefab, settings);

    private void AssertEnds(RectTransform line, Vector3 start, Vector3 end)
    {
        bool horizontal = settings.prefabAxis == UILinePrefabAxis.Horizontal;
        Vector3 actualStart = line.TransformPoint(horizontal ? new Vector2(line.rect.xMin, 0f) : new Vector2(0f, line.rect.yMin));
        Vector3 actualEnd = line.TransformPoint(horizontal ? new Vector2(line.rect.xMax, 0f) : new Vector2(0f, line.rect.yMax));
        Assert.That(Vector3.Distance(actualStart, start), Is.LessThan(0.001f), $"Start {actualStart}, expected {start}; position {line.anchoredPosition}, size {line.sizeDelta}, anchor {line.anchorMin}, driver {line.drivenByObject}");
        Assert.That(Vector3.Distance(actualEnd, end), Is.LessThan(0.001f), $"End {actualEnd}, expected {end}");
    }

    [Test]
    public void CreatesConnectedPrefabUnderRequestedParentWithoutChangingItsAsset()
    {
        Vector2 assetSize = prefab.GetComponent<RectTransform>().sizeDelta;
        var line = Create();
        Assert.That(line.parent, Is.SameAs(parent));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(line.gameObject), Is.SameAs(prefab));
        Assert.That(line.rect.height, Is.EqualTo(assetSize.y));
        Assert.That(line.GetComponent<SkillTreeLineUI>(), Is.Not.Null);
        Assert.That(line.GetComponent<SkillTreeLineUI>().lockedLine, Is.Not.Null);
        Assert.That(prefab.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(assetSize));
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
    }

    [Test]
    public void HandlesDifferentParentsPivotsRotationAndScaleInTheSameCanvas()
    {
        from.pivot = Vector2.zero;
        to.pivot = Vector2.one;
        var nodeParent = Rect("Node branch", new Vector2(50f, 20f));
        nodeParent.localScale = new Vector3(0.7f, 1.3f, 1f);
        nodeParent.localRotation = Quaternion.Euler(0f, 0f, 20f);
        from.SetParent(nodeParent, false);
        parent.pivot = new Vector2(0.2f, 0.7f);
        parent.anchoredPosition = new Vector2(100f, -50f);
        parent.localScale = new Vector3(2f, 3f, 1f);
        parent.localRotation = Quaternion.Euler(0f, 0f, 33f);
        var line = Create();
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
    }

    [Test]
    public void FacingEdgesAndGapsKeepTheLineOutsideBothNodes()
    {
        to.anchoredPosition = new Vector2(100f, 0f);
        settings.endpoint = UILineEndpoint.FacingEdge;
        settings.startGap = 5f;
        settings.endGap = 7f;
        var line = Create();
        AssertEnds(line, parent.TransformPoint(new Vector2(-75f, 0f)), parent.TransformPoint(new Vector2(73f, 0f)));
        Assert.That(line.rect.width, Is.EqualTo(148f).Within(0.001f));
    }

    [Test]
    public void VerticalPrefabCanUsePivotsOffsetsAndCustomThickness()
    {
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/SkillTree/UI/LineVer.prefab");
        settings.prefabAxis = UILinePrefabAxis.Vertical;
        settings.endpoint = UILineEndpoint.Pivot;
        settings.overrideThickness = true;
        settings.thickness = 17f;
        settings.startOffset = new Vector2(3f, 5f);
        settings.endOffset = new Vector2(-2f, -9f);
        from.pivot = Vector2.zero;
        var line = Create();
        Assert.That(line.rect.width, Is.EqualTo(17f));
        AssertEnds(line, from.position + (Vector3)settings.startOffset, to.position + (Vector3)settings.endOffset);
    }

    [Test]
    public void IgnoresParentLayoutAndCanBePlacedBehindExistingChildren()
    {
        var decoration = Rect("Existing decoration", Vector2.zero);
        decoration.SetParent(parent, false);
        parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        var line = Create();
        Assert.That(line.GetSiblingIndex(), Is.EqualTo(0));
        Assert.That(line.GetComponent<LayoutElement>().ignoreLayout, Is.True);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
    }

    [Test]
    public void CreationUndoAndRedoKeepGeometryAndPrefabConnection()
    {
        var line = Create();
        Vector2 size = line.sizeDelta;
        string name = line.name;
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();
        Assert.That(parent.childCount, Is.Zero);
        Undo.PerformRedo();
        Assert.That(parent.childCount, Is.EqualTo(1));
        line = (RectTransform)parent.GetChild(0);
        Assert.That(line.name, Is.EqualTo(name));
        Assert.That(line.sizeDelta, Is.EqualTo(size));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(line.gameObject), Is.SameAs(prefab));
        Assert.That(line.GetComponent<LayoutElement>().ignoreLayout, Is.True);
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
    }

    [Test]
    public void UpdatingAnExistingLineDoesNotDuplicateItAndCanBeUndone()
    {
        var line = Create();
        Vector2 originalSize = line.sizeDelta;
        Vector3 originalPosition = line.position;
        to.anchoredPosition += new Vector2(100f, 100f);
        UILinePrefabUtility.Update(line, from, to, parent, prefab, settings);
        Undo.FlushUndoRecordObjects();
        Assert.That(parent.childCount, Is.EqualTo(1));
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
        Undo.PerformUndo();
        Assert.That(line.sizeDelta, Is.EqualTo(originalSize));
        Assert.That(line.position, Is.EqualTo(originalPosition));
    }

    [Test]
    public void UpdatingSiblingOrderCanAlsoBeUndone()
    {
        Rect("Decoration", Vector2.zero).SetParent(parent, false);
        settings.sendToBack = false;
        var line = Create();
        Assert.That(line.GetSiblingIndex(), Is.EqualTo(1));
        settings.sendToBack = true;
        UILinePrefabUtility.Update(line, from, to, parent, prefab, settings);
        Undo.FlushUndoRecordObjects();
        Assert.That(line.GetSiblingIndex(), Is.Zero);
        Undo.PerformUndo();
        Assert.That(line.GetSiblingIndex(), Is.EqualTo(1));
    }

    [Test]
    public void FirstUpdateOfAPlainPrefabInstanceCanUndoTheAddedLayoutElementAndPose()
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        var line = instance.GetComponent<RectTransform>();
        line.anchoredPosition = new Vector2(-45f, 23f);
        line.sizeDelta = new Vector2(28f, 9f);
        Vector3 originalPosition = line.position;
        Assert.That(line.GetComponent<LayoutElement>(), Is.Null);
        UILinePrefabUtility.Update(line, from, to, parent, prefab, settings);
        Undo.FlushUndoRecordObjects();
        AssertEnds(line, from.TransformPoint(from.rect.center), to.TransformPoint(to.rect.center));
        Undo.PerformUndo();
        Assert.That(line.GetComponent<LayoutElement>(), Is.Null);
        Assert.That(line.position, Is.EqualTo(originalPosition));
        Assert.That(line.sizeDelta, Is.EqualTo(new Vector2(28f, 9f)));
    }

    [Test]
    public void InvalidInputsDoNotCreateAnythingOrMoveAnEndpoint()
    {
        Vector3 original = from.position;
        Assert.That(UILinePrefabUtility.TryGetEndpoints(from, from, parent, prefab, settings, out _, out _, out _), Is.False);
        settings.startGap = 10000f;
        Assert.Throws<System.ArgumentException>(() => Create());
        settings.startGap = 0f;
        Assert.That(UILinePrefabUtility.TryGetEndpoints(from, to, parent, from.gameObject, settings, out _, out _, out _), Is.False);
        var otherCanvas = Rect("Other canvas", Vector2.zero);
        otherCanvas.gameObject.AddComponent<Canvas>().overrideSorting = true;
        otherCanvas.SetParent(null, false);
        try
        {
            Assert.That(UILinePrefabUtility.TryGetEndpoints(from, to, otherCanvas, prefab, settings, out _, out _, out _), Is.False);
        }
        finally { Object.DestroyImmediate(otherCanvas.gameObject); }
        Assert.That(parent.childCount, Is.Zero);
        Assert.That(from.position, Is.EqualTo(original));
    }
}
