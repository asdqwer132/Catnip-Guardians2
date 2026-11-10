using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillLineBendMode
{
    HorizontalThenVertical,
    VerticalThenHorizontal
}

public enum SkillLineShape { Elbow, Straight, Curve }
public enum SkillLineEndpoint { NodeEdge, NodeCenter }

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
    public SkillLineShape lineShape = SkillLineShape.Elbow;
    public SkillLineEndpoint lineEndpoint = SkillLineEndpoint.NodeEdge;
    [Range(4, 64)] public int curveSegments = 20;
    [Range(0f, 1f)] public float curveBend = .5f;
    public bool useReadableName = true;

    [Header("Tree Layout")]
    public SkillTreeLayoutData layout;
    public bool autoRememberPositions = true;

    [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("nodeDatas")]
    private List<SkillNodeData> legacyNodes = new List<SkillNodeData>();
    [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("nodePositions")]
    private List<Vector2Int> legacyGrid = new List<Vector2Int>();
    [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("nodeLayout")]
    private List<SkillNodePlacement> legacyLayout = new List<SkillNodePlacement>();
    public List<SkillNodeData> nodeDatas => layout != null ? layout.nodeDatas : legacyNodes;
    public List<Vector2Int> nodePositions => legacyGrid;
    public List<SkillNodePlacement> nodeLayout => layout != null ? layout.nodeLayout : legacyLayout;
}
