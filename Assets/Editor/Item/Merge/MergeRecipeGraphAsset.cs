using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MergeRecipeGraph", menuName = "04.Items/Merge/Recipe Graph")]
public sealed class MergeRecipeGraphAsset : ScriptableObject
{
    public MergeRecipeBook recipeBook;
    public List<MergeGraphItemNode> items = new List<MergeGraphItemNode>();
    [SerializeField, HideInInspector] private int inputLayoutVersion;
    internal int InputLayoutVersion { get => inputLayoutVersion; set => inputLayoutVersion = value; }
    [Header("자동 정렬 · 노드 테두리 사이 간격")]
    public Vector2 nodeSpacing = new Vector2(140, 45);
    [Header("노드 티어 색상")]
    public Gradient tierColors = DefaultTierColors();
    [Min(0)] public int colorMinTier;
    [Min(1)] public int colorMaxTier = 10;
    public Color GetTierColor(int tier)
        => tierColors.Evaluate(Mathf.InverseLerp(colorMinTier, Mathf.Max(colorMinTier + 1, colorMaxTier), tier));
    private static Gradient DefaultTierColors()
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] {
            new GradientColorKey(new Color(.16f, .3f, .43f), 0),
            new GradientColorKey(new Color(.2f, .4f, .26f), .5f),
            new GradientColorKey(new Color(.5f, .24f, .13f), 1)
        }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return gradient;
    }
    [SerializeField, HideInInspector, FormerlySerializedAs("recipes")]
    private List<MergeGraphRecipeNode> legacyRecipes = new List<MergeGraphRecipeNode>();
    internal List<MergeGraphRecipeNode> LegacyRecipes => legacyRecipes;
}

[Serializable]
public sealed class MergeGraphItemNode
{
    public const int DefaultInputCount = 4;
    [HideInInspector] public string id = Guid.NewGuid().ToString("N");
    public MergeItem item;
    public Vector2 position;
    [Min(0)] public int tier;
    // 입력 하나가 재료 한 개입니다. 같은 아이템을 여러 입력에 연결하면 수량으로 합산합니다.
    public List<MergeItem> ingredients = new List<MergeItem>(new MergeItem[DefaultInputCount]);
    // 같은 MergeItem을 쓰는 복제 노드도 각각 다른 출력에 연결할 수 있습니다. 이전 그래프는 첫 출력을 사용합니다.
    [HideInInspector] public List<string> ingredientNodeIds = new List<string>();
    public bool HasIngredients => ingredients.Exists(source => source != null);
    [HideInInspector] public ItemRecipeData generatedRecipe;
    [SerializeField, HideInInspector, FormerlySerializedAs("inputs")]
    private List<MergeGraphInputPair> legacyInputs = new List<MergeGraphInputPair>();
    internal List<MergeGraphInputPair> LegacyInputs => legacyInputs;
    [SerializeField, HideInInspector] private ItemData itemData;
    [SerializeField, HideInInspector] private MergeItem legacyDefinition;
    internal ItemData LegacyData => itemData != null ? itemData : legacyDefinition != null ? legacyDefinition.item : null;
    internal MergeItem LegacyDefinition => legacyDefinition;
    internal void ClearLegacy() { itemData = null; legacyDefinition = null; }
}

// 이전 A/B 고정 입력을 열 때 N개 입력 노드로 변환하기 위한 직렬화 형식입니다.
[Serializable]
public sealed class MergeGraphInputPair
{
    public MergeItem first;
    public MergeItem second;
    public int tier;
    public ItemRecipeData generatedRecipe;
}
[Serializable]
public sealed class MergeGraphRecipeNode
{
    public ItemRecipeData recipe;
    public Vector2 position;
}
