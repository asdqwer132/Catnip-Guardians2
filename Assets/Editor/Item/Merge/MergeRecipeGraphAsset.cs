using System;
using System.Collections.Generic;
using UnityEngine;

// 편집기 전용 배치 데이터입니다. 게임에서는 기존 MergeItem / ItemRecipeData만 사용합니다.
[CreateAssetMenu(fileName = "MergeRecipeGraph", menuName = "04.Items/Merge/Recipe Graph")]
public sealed class MergeRecipeGraphAsset : ScriptableObject
{
    public ItemSeries defaultSeries = ItemSeries.Weapon;
    [Tooltip("새 아이템마다 이 프리팹을 상속한 별도 Variant를 생성합니다.")]
    public GameObject defaultPrefab;
    public List<MergeGraphItemNode> items = new List<MergeGraphItemNode>();
    public List<MergeGraphRecipeNode> recipes = new List<MergeGraphRecipeNode>();
}

[Serializable]
public sealed class MergeGraphItemNode
{
    public MergeItem item;
    public Vector2 position;
}

[Serializable]
public sealed class MergeGraphRecipeNode
{
    public ItemRecipeData recipe;
    public Vector2 position;
}
