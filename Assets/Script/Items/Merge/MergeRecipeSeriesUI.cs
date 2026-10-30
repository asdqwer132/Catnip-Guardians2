using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class MergeRecipeConnection
{
    public ItemRecipeData recipe;
    public MergeRecipeNodeUI from;
    public MergeRecipeNodeUI to;
    public RecipeTreeLineUI line;
}

// UI 생성은 Editor의 컨텍스트 메뉴에서만 합니다. 실행 중에는 작성된 노드/선만 표시합니다.
[DisallowMultipleComponent]
public sealed class MergeRecipeSeriesUI : MonoBehaviour
{
    public ItemSeries series;
    public MergeRecipeBook recipeBook;
    public ScrollRect scrollView;

    [Header("Editor Layout")]
    public MergeRecipeNodeUI nodeTemplate;
    public RecipeTreeLineUI lineTemplate;
    public Vector2 nodeSpacing = new Vector2(80, 50);
    public Vector2 padding = new Vector2(40, 40);

    [SerializeField, HideInInspector] private RectTransform generatedRoot;
    [SerializeField, HideInInspector] private List<MergeRecipeNodeUI> nodes = new List<MergeRecipeNodeUI>();
    [SerializeField, HideInInspector] private List<MergeRecipeConnection> connections = new List<MergeRecipeConnection>();
    public RectTransform GeneratedRoot => generatedRoot;
    public IReadOnlyList<MergeRecipeNodeUI> Nodes => nodes;
    public IReadOnlyList<MergeRecipeConnection> Connections => connections;

    public void Refresh(bool onlyCrafted, MergeDiscoveryHistory history)
    {
        foreach (var node in nodes)
        {
            if (node == null) continue;
            node.RefreshIcon();
            node.gameObject.SetActive(node.item != null && (!onlyCrafted || (history != null && history.HasCrafted(node.item))));
        }
        foreach (var connection in connections)
        {
            if (connection?.line == null) continue;
            bool visible = connection.from != null && connection.to != null &&
                connection.from.gameObject.activeSelf && connection.to.gameObject.activeSelf;
            connection.line.SetVisible(visible);
            if (visible) connection.line.RefreshPosition();
        }
    }

#if UNITY_EDITOR
    public void SetEditorLayout(RectTransform root, List<MergeRecipeNodeUI> layoutNodes, List<MergeRecipeConnection> layoutConnections)
    {
        generatedRoot = root;
        nodes = layoutNodes;
        connections = layoutConnections;
    }
#endif
}
