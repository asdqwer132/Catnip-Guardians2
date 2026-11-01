using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillLineBendMode
{
    HorizontalThenVertical,
    VerticalThenHorizontal
}

[Serializable]
public sealed class SkillNodePlacement
{
    public SkillNodeData node;
    public Vector2 position;
}

[ExecuteAlways]
public partial class SkillTreeNodePlacer : MonoBehaviour
{
    [Header("Parent")]
    public RectTransform nodeArea;
    public RectTransform lineArea;

    [Header("Prefab")]
    public SkillNodeUI nodePrefab;
    public RectTransform horizontalLinePrefab;
    public RectTransform verticalLinePrefab;

    [Header("Initial Grid Layout")]
    public Vector2 startPosition = Vector2.zero;
    public Vector2 spacing = new Vector2(180f, 120f);

    [Header("Line")]
    public SkillLineBendMode bendMode = SkillLineBendMode.HorizontalThenVertical;
    public float nodeHalfSize = 40f;
    public bool useReadableName = true;

    [Header("Node Data")]
    public List<SkillNodeData> nodeDatas = new List<SkillNodeData>();
    [Tooltip("Initial grid coordinates. Once placed, Saved Node Layout stores the freely edited positions.")]
    public List<Vector2Int> nodePositions = new List<Vector2Int>();

    [Header("Saved Node Layout (UI Units)")]
    public bool autoRememberPositions = true;
    public List<SkillNodePlacement> nodeLayout = new List<SkillNodePlacement>();
}
