#if UNITY_EDITOR
using System.Collections.Generic;
using Catnip.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class UIObjectPlacementTests
{
    private GameObject host;
    private RectTransform reference;
    private Transform target;
    private UIObjectPlacementSettings settings;
    [SetUp]
    public void Setup()
    {
        host = new GameObject("UI placement test");
        reference = new GameObject("Reference", typeof(RectTransform)).GetComponent<RectTransform>();
        reference.SetParent(host.transform, false); reference.sizeDelta = new Vector2(20f, 10f);
        target = new GameObject("World target").transform; target.SetParent(host.transform, false);
        settings = new UIObjectPlacementSettings { mapping = UIPlacementMapping.RectWorldCoordinates };
    }
    [TearDown]
    public void Cleanup()
    {
        if (target != null) Undo.ClearUndo(target);
        if (host != null) Object.DestroyImmediate(host);
    }
    private bool Build(out List<UIObjectPlacementMove> moves, out string error, params Transform[] targets)
        => UIObjectPlacementUtility.TryBuildPlan(reference, null, null, targets, settings, out moves, out _, out error);
    [Test]
    public void PreviewUsesReferenceTransformAndOffsetsWithoutMovingAnySceneObject()
    {
        reference.pivot = Vector2.zero;
        reference.position = new Vector3(20f, 30f, 0f);
        reference.localScale = new Vector3(2f, 3f, 1f);
        settings.referenceAnchor = UIPlacementAnchor.TopRight;
        settings.uiOffset = new Vector2(1f, 2f); settings.worldOffset = new Vector2(3f, 4f); settings.worldZ = 7f;
        Vector3 original = target.position;
        Assert.That(Build(out var moves, out string error, target), Is.True, error);
        Assert.That(moves[0].toPivot.x, Is.EqualTo(65f).Within(.001f));
        Assert.That(moves[0].toPivot.y, Is.EqualTo(70f).Within(.001f));
        Assert.That(moves[0].toPivot.z, Is.EqualTo(7f));
        Assert.That(target.position, Is.EqualTo(original)); // 미리보기는 Transform을 변경하지 않습니다.
    }
    [Test]
    public void ColliderCornerAlignmentAccountsForChildOffsetsAndApplyIsUndoable()
    {
        target.position = new Vector3(5f, 6f, 0f);
        var child = new GameObject("Collider"); child.transform.SetParent(target, false);
        child.transform.localPosition = new Vector3(2f, 3f, 0f);
        child.AddComponent<BoxCollider2D>().size = new Vector2(4f, 2f);
        Physics2D.SyncTransforms();
        settings.referenceAnchor = UIPlacementAnchor.TopRight;
        settings.targetPivot = UIPlacementTargetPivot.Collider2DBounds;
        settings.objectAnchor = UIPlacementAnchor.BottomLeft;
        Assert.That(Build(out var moves, out string error, target), Is.True, error);
        UIObjectPlacementUtility.Apply(moves);
        Assert.That(target.position.x, Is.EqualTo(10f).Within(.001f));
        Assert.That(target.position.y, Is.EqualTo(3f).Within(.001f));
        Undo.PerformUndo();
        Assert.That(target.position, Is.EqualTo(new Vector3(5f, 6f, 0f)));
    }
    [Test]
    public void ScreenProjectionUsesSeparateCameraViewportsAndTheRequestedDepth()
    {
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        canvas.transform.SetParent(host.transform, false); canvas.renderMode = RenderMode.WorldSpace;
        reference.SetParent(canvas.transform, false);
        var sourceCamera = new GameObject("UI camera").AddComponent<Camera>(); sourceCamera.transform.SetParent(host.transform, false);
        sourceCamera.transform.position = new Vector3(0f, 0f, -10f);
        sourceCamera.orthographic = true; sourceCamera.orthographicSize = 5f;
        sourceCamera.pixelRect = new Rect(0f, 0f, 800f, 400f); sourceCamera.aspect = 2f;
        var worldCamera = new GameObject("World camera").AddComponent<Camera>(); worldCamera.transform.SetParent(host.transform, false);
        worldCamera.transform.position = new Vector3(0f, 0f, -10f);
        worldCamera.orthographic = true; worldCamera.orthographicSize = 10f;
        worldCamera.pixelRect = new Rect(400f, 0f, 400f, 400f); worldCamera.aspect = 1f;
        settings.mapping = UIPlacementMapping.ScreenToWorld; settings.worldZ = 2f;
        Assert.That(UIObjectPlacementUtility.TryBuildPlan(reference, sourceCamera, worldCamera, new[] { target }, settings,
            out var moves, out _, out string error), Is.True, error);
        Assert.That(moves[0].toPivot.x, Is.EqualTo(-10f).Within(.001f));
        Assert.That(moves[0].toPivot.y, Is.EqualTo(0f).Within(.001f));
        Assert.That(moves[0].toPivot.z, Is.EqualTo(2f).Within(.001f));
    }
    [Test]
    public void AxisLocksAndDepthKeepTheUnselectedCoordinates()
    {
        target.position = new Vector3(7f, 8f, 9f);
        settings.referenceAnchor = UIPlacementAnchor.TopRight;
        settings.moveX = false; settings.keepTargetZ = true; settings.worldOffset = new Vector2(100f, .6f);
        settings.snapToWorldGrid = true; settings.snapStep = new Vector2(1f, 1f);
        Assert.That(Build(out var moves, out string error, target), Is.True, error);
        Assert.That(moves[0].position, Is.EqualTo(new Vector3(7f, 6f, 9f)));
    }
    [Test]
    public void InvalidHierarchyAndMissingBoundsFailBeforeMovingAnything()
    {
        var child = new GameObject("Child").transform; child.SetParent(target, false);
        Assert.That(Build(out _, out _, target, child), Is.False);
        Assert.That(Build(out _, out _, host.transform), Is.False); // 기준 UI 부모 이동 금지
        settings.targetPivot = UIPlacementTargetPivot.Collider2DBounds;
        Assert.That(Build(out _, out _, target), Is.False);
        Assert.That(target.position, Is.EqualTo(Vector3.zero));
    }
    [Test]
    public void ChangedTargetRejectsTheEntireStalePlan()
    {
        Assert.That(Build(out var moves, out string error, target), Is.True, error);
        target.position = Vector3.one;
        Assert.Throws<System.InvalidOperationException>(() => UIObjectPlacementUtility.Apply(moves));
        Assert.That(target.position, Is.EqualTo(Vector3.one));
    }
}
#endif
