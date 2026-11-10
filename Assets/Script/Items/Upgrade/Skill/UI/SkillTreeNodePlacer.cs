using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Node Size")]
    public Vector2 nodeSize = new Vector2(80f, 80f);

    [Header("Line")]
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
    [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("nodeLayout")]
    private List<SkillNodePlacement> legacyLayout = new List<SkillNodePlacement>();
    public List<SkillNodeData> nodeDatas => layout != null ? layout.nodeDatas : legacyNodes;
    public List<SkillNodePlacement> nodeLayout => layout != null ? layout.nodeLayout : legacyLayout;
}
