using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Sizes existing GridLayoutGroup cells from the available local UI space.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(GridLayoutGroup))]
[AddComponentMenu("Layout/Grid Cell Size Fitter")]
public sealed class GridCellSizeFitter : UIBehaviour, ILayoutSelfController
{
    public enum FitAxis { Width, Height }
    public enum CellShape { Square, AxisOnly, AspectRatio }

    [Header("Available Space")]
    [Tooltip("Optional size reference, such as a ScrollRect viewport. Empty uses this content RectTransform. Measured in this grid's local UI units.")]
    public RectTransform sizeSource;
    [Tooltip("Width fits columns; Height fits rows.")]
    public FitAxis fitAxis = FitAxis.Width;
    [Min(1), Tooltip("Number of columns (Width) or rows (Height), regardless of the current number of children.")]
    public int count = 4;

    [Header("Cell Size")]
    [Tooltip("Square changes both dimensions. Axis Only keeps the other Cell Size value on GridLayoutGroup. Aspect Ratio keeps the specified width / height ratio.")]
    public CellShape cellShape = CellShape.Square;
    [Min(0.01f), Tooltip("Cell width divided by height. Used only in Aspect Ratio mode.")]
    public float aspectRatio = 1f;
    [Tooltip("Sets the grid's fixed column/row count and fill axis to match this fitter. Disable to keep your own grid constraint and fill order.")]
    public bool syncConstraint = true;

    private RectTransform content;
    private GridLayoutGroup grid;
    private readonly Vector3[] sourceCorners = new Vector3[4];
    private bool refreshing;
    private bool childSizeDirty;

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        Refresh();
    }

    // Also picks up spacing, padding, source size and Inspector changes without
    // registering another layout rebuild when the calculated size is unchanged.
    private void LateUpdate() => Refresh();

    [ContextMenu("Refresh Cell Size")]
    public void Refresh() => UpdateCellSize();

    public void SetLayoutHorizontal()
    {
        UpdateCellSize();
        // GridLayoutGroup sizes the children immediately after this controller.
        childSizeDirty = false;
    }

    public void SetLayoutVertical()
    {
        UpdateCellSize();
        if (childSizeDirty && grid != null && grid.isActiveAndEnabled)
        {
            // Parent layouts may assign the final height during the vertical pass.
            // GridLayoutGroup normally sizes its children only in its horizontal pass.
            grid.SetLayoutHorizontal();
            childSizeDirty = false;
        }
    }

    private void UpdateCellSize()
    {
        if (!isActiveAndEnabled || refreshing)
            return;

        if (content == null) content = (RectTransform)transform;
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (grid == null || !grid.isActiveAndEnabled)
            return;

        refreshing = true;
        try
        {
            bool width = fitAxis == FitAxis.Width;
            int slots = Mathf.Max(1, count);
            float available = GetAvailableSize(width);
            float padding = width ? grid.padding.horizontal : grid.padding.vertical;
            float spacing = width ? grid.spacing.x : grid.spacing.y;

            // N cells have N-1 gaps; padding is applied once at each outer edge.
            float side = Mathf.Max(0f, (available - padding - spacing * (slots - 1)) / slots);
            if (float.IsNaN(side) || float.IsInfinity(side))
                return;

            Vector2 size = grid.cellSize;
            if (width) size.x = side;
            else size.y = side;

            if (cellShape == CellShape.Square)
                size = new Vector2(side, side);
            else if (cellShape == CellShape.AspectRatio)
            {
                float ratio = aspectRatio > 0f && !float.IsInfinity(aspectRatio) ? aspectRatio : 1f;
                if (width) size.y = side / ratio;
                else size.x = side * ratio;
            }

            if (syncConstraint)
            {
                grid.constraint = width ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.FixedRowCount;
                grid.constraintCount = slots;
                grid.startAxis = width ? GridLayoutGroup.Axis.Horizontal : GridLayoutGroup.Axis.Vertical;
            }

            if (grid.cellSize == size)
                return;

            grid.cellSize = size;
            childSizeDirty = true;
        }
        finally
        {
            refreshing = false;
        }
    }

    private float GetAvailableSize(bool width)
    {
        if (sizeSource == null || sizeSource == content)
            return width ? content.rect.width : content.rect.height;

        // A viewport and its content can have different scales. Convert the
        // reference bounds to the units used by GridLayoutGroup.cellSize.
        sizeSource.GetWorldCorners(sourceCorners);
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (int i = 0; i < sourceCorners.Length; i++)
        {
            Vector3 local = content.InverseTransformPoint(sourceCorners[i]);
            float value = width ? local.x : local.y;
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }
        return max - min;
    }
}
