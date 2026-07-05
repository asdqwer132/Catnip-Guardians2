using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SkillLineBendMode
{
    HorizontalThenVertical,
    VerticalThenHorizontal
}

[ExecuteAlways]
public class SkillTreeNodePlacer : MonoBehaviour
{
    [Header("Parent")]
    public RectTransform nodeArea;
    public RectTransform lineArea;

    [Header("Prefab")]
    public SkillNodeUI nodePrefab;
    public RectTransform horizontalLinePrefab;
    public RectTransform verticalLinePrefab;

    [Header("Layout")]
    public Vector2 startPosition = Vector2.zero;
    public Vector2 spacing = new Vector2(180f, 120f);

    [Header("Line")]
    public SkillLineBendMode bendMode = SkillLineBendMode.HorizontalThenVertical;

    [Tooltip("라인이 노드 중앙에서 살짝 떨어져 시작하게 만들 때 사용")]
    public float nodeHalfSize = 40f;

    public bool useReadableName = true;

    [Header("Node Data")]
    public List<SkillNodeData> nodeDatas = new List<SkillNodeData>();

    [Header("Node Position")]
    public List<Vector2Int> nodePositions = new List<Vector2Int>();

    private readonly Dictionary<SkillNodeData, SkillNodeUI> createdNodeMap =
        new Dictionary<SkillNodeData, SkillNodeUI>();

    private readonly Dictionary<SkillNodeData, List<GameObject>> lineMap =
        new Dictionary<SkillNodeData, List<GameObject>>();

#if UNITY_EDITOR
    [ContextMenu("Generate Skill Tree")]
    public void Generate()
    {
        if (!ValidateSetting())
            return;

        ClearGeneratedObjects();

        createdNodeMap.Clear();
        lineMap.Clear();

        CreateNodes();
        CreateLines();
        ApplyLinesToNodes();

        EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Clear Generated Objects")]
    public void ClearGeneratedObjects()
    {
        ClearChildren(nodeArea);
        ClearChildren(lineArea);
    }

    private bool ValidateSetting()
    {
        if (nodeArea == null)
        {
            Debug.LogWarning("nodeArea가 없습니다.");
            return false;
        }

        if (lineArea == null)
        {
            Debug.LogWarning("lineArea가 없습니다.");
            return false;
        }

        if (nodePrefab == null)
        {
            Debug.LogWarning("nodePrefab이 없습니다.");
            return false;
        }

        if (horizontalLinePrefab == null)
        {
            Debug.LogWarning("horizontalLinePrefab이 없습니다.");
            return false;
        }

        if (verticalLinePrefab == null)
        {
            Debug.LogWarning("verticalLinePrefab이 없습니다.");
            return false;
        }

        if (nodeDatas == null || nodeDatas.Count == 0)
        {
            Debug.LogWarning("nodeDatas가 비어있습니다.");
            return false;
        }

        if (nodePositions == null || nodePositions.Count == 0)
        {
            Debug.LogWarning("nodePositions가 비어있습니다.");
            return false;
        }

        if (nodeDatas.Count != nodePositions.Count)
        {
            Debug.LogWarning(
                $"nodeDatas와 nodePositions 개수가 다릅니다. nodeDatas: {nodeDatas.Count}, nodePositions: {nodePositions.Count}"
            );
        }

        return true;
    }

    private void CreateNodes()
    {
        int count = Mathf.Min(nodeDatas.Count, nodePositions.Count);

        for (int i = 0; i < count; i++)
        {
            SkillNodeData nodeData = nodeDatas[i];

            if (nodeData == null)
                continue;

            if (createdNodeMap.ContainsKey(nodeData))
            {
                Debug.LogWarning("중복된 SkillNodeData가 있습니다: " + nodeData.name);
                continue;
            }

            SkillNodeUI nodeUI = PrefabUtility.InstantiatePrefab(nodePrefab, nodeArea) as SkillNodeUI;

            if (nodeUI == null)
                continue;

            RectTransform rect = nodeUI.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition = GetAnchoredPosition(nodePositions[i]);
                rect.localScale = Vector3.one;
            }

            if (useReadableName)
                nodeUI.gameObject.name = "Node_" + GetNodeName(nodeData);

            nodeUI.SetData(nodeData, Array.Empty<GameObject>());

            createdNodeMap.Add(nodeData, nodeUI);
            lineMap.Add(nodeData, new List<GameObject>());

            EditorUtility.SetDirty(nodeUI);
        }
    }

    private void CreateLines()
    {
        int count = Mathf.Min(nodeDatas.Count, nodePositions.Count);

        for (int i = 0; i < count; i++)
        {
            SkillNodeData childNode = nodeDatas[i];

            if (childNode == null)
                continue;

            if (childNode.requiredSkills == null)
                continue;

            for (int j = 0; j < childNode.requiredSkills.Count; j++)
            {
                SkillNodeData parentNode = childNode.requiredSkills[j];

                if (parentNode == null)
                    continue;

                if (!createdNodeMap.ContainsKey(parentNode))
                {
                    Debug.LogWarning(
                        $"requiredSkills에 들어있는 부모 노드가 배치 목록에 없습니다. Parent: {parentNode.name}, Child: {childNode.name}"
                    );
                    continue;
                }

                if (!createdNodeMap.ContainsKey(childNode))
                    continue;

                RectTransform parentRect = createdNodeMap[parentNode].GetComponent<RectTransform>();
                RectTransform childRect = createdNodeMap[childNode].GetComponent<RectTransform>();

                if (parentRect == null || childRect == null)
                    continue;

                Vector2 parentPos = parentRect.anchoredPosition;
                Vector2 childPos = childRect.anchoredPosition;

                List<GameObject> createdLines = CreateConnectionLines(
                    parentNode,
                    childNode,
                    parentPos,
                    childPos
                );

                if (!lineMap.ContainsKey(parentNode))
                    lineMap.Add(parentNode, new List<GameObject>());

                // 중요:
                // parentNode가 해금될 때 parentNode -> childNode 라인이 켜져야 하므로
                // 라인은 부모 노드 쪽에 넣는다.
                lineMap[parentNode].AddRange(createdLines);
            }
        }
    }

    private List<GameObject> CreateConnectionLines(
        SkillNodeData parentNode,
        SkillNodeData childNode,
        Vector2 parentPos,
        Vector2 childPos)
    {
        List<GameObject> result = new List<GameObject>();

        Vector2 start = parentPos;
        Vector2 end = childPos;

        if (Mathf.Approximately(start.x, end.x))
        {
            GameObject vertical = CreateVerticalLine(parentNode, childNode, start, end);

            if (vertical != null)
                result.Add(vertical);

            return result;
        }

        if (Mathf.Approximately(start.y, end.y))
        {
            GameObject horizontal = CreateHorizontalLine(parentNode, childNode, start, end);

            if (horizontal != null)
                result.Add(horizontal);

            return result;
        }

        Vector2 corner;

        if (bendMode == SkillLineBendMode.HorizontalThenVertical)
            corner = new Vector2(end.x, start.y);
        else
            corner = new Vector2(start.x, end.y);

        GameObject firstLine;
        GameObject secondLine;

        if (bendMode == SkillLineBendMode.HorizontalThenVertical)
        {
            firstLine = CreateHorizontalLine(parentNode, childNode, start, corner);
            secondLine = CreateVerticalLine(parentNode, childNode, corner, end);
        }
        else
        {
            firstLine = CreateVerticalLine(parentNode, childNode, start, corner);
            secondLine = CreateHorizontalLine(parentNode, childNode, corner, end);
        }

        if (firstLine != null)
            result.Add(firstLine);

        if (secondLine != null)
            result.Add(secondLine);

        return result;
    }

    private GameObject CreateHorizontalLine(
        SkillNodeData parentNode,
        SkillNodeData childNode,
        Vector2 start,
        Vector2 end)
    {
        float direction = Mathf.Sign(end.x - start.x);

        Vector2 fixedStart = start;
        Vector2 fixedEnd = end;

        fixedStart.x += nodeHalfSize * direction;
        fixedEnd.x -= nodeHalfSize * direction;

        float distance = Mathf.Abs(fixedEnd.x - fixedStart.x);

        if (distance <= 0.01f)
            return null;

        RectTransform line = PrefabUtility.InstantiatePrefab(horizontalLinePrefab, lineArea) as RectTransform;

        if (line == null)
            return null;

        line.anchoredPosition = new Vector2(
            (fixedStart.x + fixedEnd.x) * 0.5f,
            fixedStart.y
        );

        Vector2 size = line.sizeDelta;
        size.x = distance;
        line.sizeDelta = size;

        line.localScale = Vector3.one;
        line.gameObject.SetActive(false);

        if (useReadableName)
            line.gameObject.name = $"Line_H_{GetNodeName(parentNode)}_To_{GetNodeName(childNode)}";

        EditorUtility.SetDirty(line);

        return line.gameObject;
    }

    private GameObject CreateVerticalLine(
        SkillNodeData parentNode,
        SkillNodeData childNode,
        Vector2 start,
        Vector2 end)
    {
        float direction = Mathf.Sign(end.y - start.y);

        Vector2 fixedStart = start;
        Vector2 fixedEnd = end;

        fixedStart.y += nodeHalfSize * direction;
        fixedEnd.y -= nodeHalfSize * direction;

        float distance = Mathf.Abs(fixedEnd.y - fixedStart.y);

        if (distance <= 0.01f)
            return null;

        RectTransform line = PrefabUtility.InstantiatePrefab(verticalLinePrefab, lineArea) as RectTransform;

        if (line == null)
            return null;

        line.anchoredPosition = new Vector2(
            fixedStart.x,
            (fixedStart.y + fixedEnd.y) * 0.5f
        );

        Vector2 size = line.sizeDelta;
        size.y = distance;
        line.sizeDelta = size;

        line.localScale = Vector3.one;
        line.gameObject.SetActive(false);

        if (useReadableName)
            line.gameObject.name = $"Line_V_{GetNodeName(parentNode)}_To_{GetNodeName(childNode)}";

        EditorUtility.SetDirty(line);

        return line.gameObject;
    }

    private void ApplyLinesToNodes()
    {
        foreach (var pair in createdNodeMap)
        {
            SkillNodeData nodeData = pair.Key;
            SkillNodeUI nodeUI = pair.Value;

            if (nodeUI == null)
                continue;

            GameObject[] lines = Array.Empty<GameObject>();

            if (lineMap.TryGetValue(nodeData, out List<GameObject> lineList))
                lines = lineList.ToArray();

            nodeUI.SetData(nodeData, lines);

            EditorUtility.SetDirty(nodeUI);
        }
    }

    private Vector2 GetAnchoredPosition(Vector2Int gridPosition)
    {
        return startPosition + new Vector2(
            gridPosition.x * spacing.x,
            gridPosition.y * spacing.y
        );
    }

    private void ClearChildren(RectTransform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private string GetNodeName(SkillNodeData node)
    {
        if (node == null)
            return "NULL";

        if (!string.IsNullOrEmpty(node.GetDataName()))
            return node.GetDataName();

        if (!string.IsNullOrEmpty(node.dataId))
            return node.dataId;

        return node.name;
    }
#endif
}