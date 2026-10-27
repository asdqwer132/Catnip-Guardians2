using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MergeRecipeGraph", menuName = "04.Items/Merge/Recipe Graph")]
public sealed class MergeRecipeGraphAsset : ScriptableObject
{
    public ItemSeries defaultSeries = ItemSeries.Weapon;
    [Tooltip("새 머지 아이템마다 이 프리팹을 상속한 별도 Variant를 만듭니다.")]
    public GameObject defaultPrefab;
    public MergeRecipeBook recipeBook;
    public List<MergeGraphItemNode> items = new List<MergeGraphItemNode>();
    [SerializeField, HideInInspector, FormerlySerializedAs("recipes")]
    private List<MergeGraphRecipeNode> legacyRecipes = new List<MergeGraphRecipeNode>();
    internal List<MergeGraphRecipeNode> LegacyRecipes => legacyRecipes;
}

[Serializable]
public sealed class MergeGraphItemNode
{
    public MergeItem item;
    public Vector2 position;
    public List<MergeGraphInputPair> inputs = new List<MergeGraphInputPair> { new MergeGraphInputPair() };
    [SerializeField, HideInInspector] private ItemData itemData;
    [SerializeField, HideInInspector] private MergeItem legacyDefinition;
    internal ItemData LegacyData => itemData != null ? itemData : legacyDefinition != null ? legacyDefinition.item : null;
    internal MergeItem LegacyDefinition => legacyDefinition;
    internal void ClearLegacy() { itemData = null; legacyDefinition = null; }
}

[Serializable]
public sealed class MergeGraphInputPair
{
    public MergeItem first;
    public MergeItem second;
    [Min(0)] public int tier;
    [HideInInspector] public ItemRecipeData generatedRecipe;
    public bool IsEmpty => first == null && second == null;
}

// 이전 그래프의 레시피 카드 데이터는 열 때 직접 연결로 변환합니다.
[Serializable]
public sealed class MergeGraphRecipeNode
{
    public ItemRecipeData recipe;
    public Vector2 position;
}
