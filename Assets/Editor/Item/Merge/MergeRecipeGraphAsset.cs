using System;
using System.Collections.Generic;
using UnityEngine;

public enum MergeGraphArrangeDirection { Vertical, Horizontal }

[CreateAssetMenu(fileName = "MergeRecipeGraph", menuName = "Items/Merge/Recipe Graph")]
public sealed class MergeRecipeGraphAsset : ScriptableObject
{
    public MergeRecipeBook recipeBook;
    public List<MergeGraphItemNode> items = new List<MergeGraphItemNode>();
    [SerializeField, HideInInspector] private int inputLayoutVersion;
    internal int InputLayoutVersion { get => inputLayoutVersion; set => inputLayoutVersion = value; }
    [Header("자동 정렬 · 노드 테두리 사이 간격")]
    public MergeGraphArrangeDirection arrangeDirection;
    public bool keepBranchRows = true;
    public Vector2 nodeSpacing = new Vector2(140, 45);

}

[Serializable]
public sealed class MergeGraphItemNode
{
    public const int DefaultInputCount = 4;
    [HideInInspector] public string id = Guid.NewGuid().ToString("N");
    public MergeItem item;
    public Vector2 position;
    public bool overrideRow;
    [Min(0)] public int row;
    public bool overrideStartDepth;
    [Min(0)] public int startDepth;
    // 입력 하나가 재료 한 개입니다. 같은 아이템을 여러 입력에 연결하면 수량으로 합산합니다.
    public List<MergeItem> ingredients = new List<MergeItem>(new MergeItem[DefaultInputCount]);
    // 같은 MergeItem을 쓰는 복제 노드도 각각 다른 출력에 연결할 수 있습니다. 이전 그래프는 첫 출력을 사용합니다.
    [HideInInspector] public List<string> ingredientNodeIds = new List<string>();
    public bool HasIngredients => ingredients.Exists(source => source != null);
    [HideInInspector] public ItemRecipeData generatedRecipe;
}
