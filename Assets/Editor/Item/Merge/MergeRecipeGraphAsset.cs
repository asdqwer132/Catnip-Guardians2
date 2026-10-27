using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 레시피 편집과 배치만 저장합니다. 프리팹 / 반지름은 MergeItem 에디터가 관리합니다.
[CreateAssetMenu(fileName = "MergeRecipeGraph", menuName = "04.Items/Merge/Recipe Graph")]
public sealed class MergeRecipeGraphAsset : ScriptableObject
{
    public ItemSeries defaultSeries = ItemSeries.Weapon;
    public List<MergeGraphItemNode> items = new List<MergeGraphItemNode>();
    public List<MergeGraphRecipeNode> recipes = new List<MergeGraphRecipeNode>();

    public bool UpgradeLegacyNodes()
    {
        bool changed = false;
        foreach (var node in items) changed |= node.UpgradeLegacy();
        return changed;
    }
}

[Serializable]
public sealed class MergeGraphItemNode
{
    public ItemData itemData;
    public Vector2 position;
    [SerializeField, HideInInspector, FormerlySerializedAs("item")] private MergeItem legacyDefinition;

    internal bool UpgradeLegacy()
    {
        if (legacyDefinition == null) return false;
        if (itemData == null) itemData = legacyDefinition.item;
        legacyDefinition = null;
        return true;
    }
}

[Serializable]
public sealed class MergeGraphRecipeNode
{
    public ItemRecipeData recipe;
    public Vector2 position;
}
