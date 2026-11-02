using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class GridCellSizeFitterTests
{
    private GameObject root;
    private RectTransform content;
    private GridLayoutGroup grid;
    private GridCellSizeFitter fitter;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Grid fitter test", typeof(RectTransform));
        content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        content.SetParent(root.transform, false);
        content.sizeDelta = new Vector2(500f, 320f);
        grid = content.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(10, 20, 15, 25);
        grid.spacing = new Vector2(10f, 20f);
        fitter = content.gameObject.AddComponent<GridCellSizeFitter>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    private RectTransform Child()
    {
        var child = new GameObject("Cell", typeof(RectTransform)).GetComponent<RectTransform>();
        child.SetParent(content, false);
        return child;
    }

    private void AssertSize(float width, float height)
    {
        Assert.That(grid.cellSize.x, Is.EqualTo(width).Within(0.001f));
        Assert.That(grid.cellSize.y, Is.EqualTo(height).Within(0.001f));
    }

    [Test]
    public void WidthFitsExactColumnsAfterSubtractingPaddingAndGaps()
    {
        fitter.count = 4;
        var cells = new RectTransform[5];
        for (int i = 0; i < cells.Length; i++) cells[i] = Child();
        fitter.Refresh();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        AssertSize(110f, 110f); // (500 - 10 - 20 - 10 * 3) / 4
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
        Assert.That(grid.constraintCount, Is.EqualTo(4));
        Assert.That(cells[0].rect.size, Is.EqualTo(new Vector2(110f, 110f)));
        Assert.That(cells[3].anchoredPosition.x - cells[0].anchoredPosition.x, Is.EqualTo(360f).Within(0.001f));
        Assert.That(cells[4].anchoredPosition.y, Is.LessThan(cells[0].anchoredPosition.y));
    }

    [Test]
    public void HeightFitsRowsAndFillsVertically()
    {
        fitter.fitAxis = GridCellSizeFitter.FitAxis.Height;
        fitter.count = 3;
        var first = Child(); Child(); Child(); var fourth = Child();
        fitter.Refresh();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        AssertSize(80f, 80f); // (320 - 15 - 25 - 20 * 2) / 3
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
        Assert.That(grid.startAxis, Is.EqualTo(GridLayoutGroup.Axis.Vertical));
        Assert.That(fourth.anchoredPosition.x - first.anchoredPosition.x, Is.EqualTo(90f).Within(0.001f));
        Assert.That(fourth.anchoredPosition.y, Is.EqualTo(first.anchoredPosition.y).Within(0.001f));
    }

    [TestCase(GridCellSizeFitter.FitAxis.Width, 110f, 73f)]
    [TestCase(GridCellSizeFitter.FitAxis.Height, 61f, 55f)]
    public void AxisOnlyPreservesTheOtherAuthoredDimension(GridCellSizeFitter.FitAxis axis, float width, float height)
    {
        fitter.fitAxis = axis;
        fitter.cellShape = GridCellSizeFitter.CellShape.AxisOnly;
        grid.cellSize = new Vector2(61f, 73f);
        fitter.Refresh();
        AssertSize(width, height);
    }

    [TestCase(GridCellSizeFitter.FitAxis.Width, 110f, 55f)]
    [TestCase(GridCellSizeFitter.FitAxis.Height, 110f, 55f)]
    public void AspectRatioKeepsWidthOverHeight(GridCellSizeFitter.FitAxis axis, float width, float height)
    {
        fitter.fitAxis = axis;
        fitter.cellShape = GridCellSizeFitter.CellShape.AspectRatio;
        fitter.aspectRatio = 2f;
        fitter.Refresh();
        AssertSize(width, height);
    }

    [Test]
    public void ExternalViewportIsMeasuredInContentUnitsAndDoesNotSetContentSize()
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
        viewport.SetParent(root.transform, false);
        viewport.sizeDelta = new Vector2(300f, 200f);
        viewport.localScale = Vector3.one * 2f;
        content.localScale = Vector3.one * 0.5f;
        fitter.sizeSource = viewport;
        fitter.Refresh();
        AssertSize(285f, 285f); // viewport width = 1200 in content units
        Assert.That(content.rect.size, Is.EqualTo(new Vector2(500f, 320f)));
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        fitter.SetLayoutHorizontal();
        AssertSize(185f, 185f);
    }

    [Test]
    public void SingleCellHasNoGapAndInsufficientSpaceNeverProducesNegativeSizes()
    {
        fitter.count = 1;
        fitter.Refresh();
        AssertSize(470f, 470f);
        fitter.count = 0; // Runtime callers cannot cause division by zero.
        fitter.Refresh();
        AssertSize(470f, 470f);
        fitter.count = 4;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 20f);
        fitter.Refresh();
        AssertSize(0f, 0f);
    }

    [Test]
    public void CustomConstraintAndAuthoredAlignmentCanBePreserved()
    {
        fitter.syncConstraint = false;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.LowerRight;
        grid.startCorner = GridLayoutGroup.Corner.LowerRight;
        fitter.Refresh();
        AssertSize(110f, 110f);
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.Flexible));
        Assert.That(grid.startAxis, Is.EqualTo(GridLayoutGroup.Axis.Vertical));
        Assert.That(grid.childAlignment, Is.EqualTo(TextAnchor.LowerRight));
        Assert.That(grid.startCorner, Is.EqualTo(GridLayoutGroup.Corner.LowerRight));
    }

    [Test]
    public void ContentSizeFitterCanGrowHeightWhileWidthControlsSquareCells()
    {
        var heightFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        heightFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        for (int i = 0; i < 5; i++) Child();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        AssertSize(110f, 110f);
        Assert.That(content.rect.height, Is.EqualTo(280f).Within(0.001f));

        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 700f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        AssertSize(160f, 160f);
        Assert.That(content.rect.height, Is.EqualTo(380f).Within(0.001f));
    }

    [Test]
    public void HeightAssignedByParentLayoutUpdatesRealChildSizeInTheSamePass()
    {
        var parentLayout = root.AddComponent<VerticalLayoutGroup>();
        parentLayout.childControlHeight = true;
        parentLayout.childControlWidth = true;
        parentLayout.childForceExpandHeight = true;
        // Let the parent assign both larger and smaller heights, independently
        // of the grid's preferred content size (which describes its children).
        var sizeOverride = content.gameObject.AddComponent<LayoutElement>();
        sizeOverride.minHeight = 0f;
        sizeOverride.preferredHeight = 0f;
        sizeOverride.flexibleHeight = 1f;
        var parentRect = (RectTransform)root.transform;
        parentRect.sizeDelta = new Vector2(500f, 640f);
        fitter.fitAxis = GridCellSizeFitter.FitAxis.Height;
        fitter.count = 3;
        var child = Child();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        AssertSize(560f / 3f, 560f / 3f);
        Assert.That(child.rect.height, Is.EqualTo(grid.cellSize.y).Within(0.001f));

        parentRect.sizeDelta = new Vector2(500f, 340f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        AssertSize(260f / 3f, 260f / 3f);
        Assert.That(child.rect.size, Is.EqualTo(grid.cellSize));
    }

    [UnityTest]
    public IEnumerator SourceAndGridSettingsRefreshWithoutCallingRefreshManually()
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
        viewport.SetParent(root.transform, false);
        viewport.sizeDelta = new Vector2(400f, 200f);
        fitter.sizeSource = viewport;
        yield return null;
        AssertSize(85f, 85f);

        viewport.sizeDelta = new Vector2(600f, 200f);
        grid.spacing = new Vector2(20f, 30f);
        fitter.count = 3;
        yield return null;
        AssertSize(530f / 3f, 530f / 3f);
    }
}
