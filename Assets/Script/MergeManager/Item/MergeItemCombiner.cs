using System.Collections.Generic;
using UnityEngine;

// 핀 접촉과 굽기에서 호출하는 레시피 합성입니다.
internal sealed class MergeItemCombiner
{
    private readonly MergegameManager owner;
    public MergeItemCombiner(MergegameManager owner) => this.owner = owner;
    private bool Usable(MergeItemObject item) => owner.CanOperate && item != null && item.Owner == owner && item.CanMerge;

    public bool TryMergeAtPin(MergeItemObject seed, Vector2 center, float radius)
    {
        float searchRadiusSquared = radius * radius;
        if (!Usable(seed) || (seed.Position - center).sqrMagnitude > searchRadiusSquared) return false;
        foreach (var recipe in owner.GetRoundRecipes())
        {
            if (!MergeTree.TryGetIngredients(recipe, out var needed) ||
                !needed.TryGetValue(seed.item, out int seedCount)) continue;
            needed[seed.item] = seedCount - 1;
            var ingredients = new List<MergeItemObject> { seed };
            foreach (var candidate in owner.ActiveItems)
            {
                if (candidate == seed || !Usable(candidate) || !needed.TryGetValue(candidate.item, out int count) ||
                    count <= 0 || (candidate.Position - center).sqrMagnitude > searchRadiusSquared) continue;
                ingredients.Add(candidate);
                needed[candidate.item] = count - 1;
            }

            bool complete = true;
            foreach (int count in needed.Values) if (count > 0) { complete = false; break; }
            if (complete && TryMerge(recipe, ingredients)) return true;
        }
        return false;
    }

    public bool TryBake(MergeItemObject food, MergeItem burningItem)
    {
        if (!Usable(food) || burningItem == null) return false;
        foreach (var recipe in owner.GetRoundRecipes())
            if (MergeTree.MatchesPair(recipe, food.item, burningItem.item))
                return TryMerge(recipe, new List<MergeItemObject> { food });
        return false;
    }

    private bool TryMerge(ItemRecipeData recipe, List<MergeItemObject> ingredients)
    {
        var definition = owner.GetDefinitionAtMergeItem(recipe.resultItem);
        if (owner.objectPool == null || !MergegameManager.TryGetPrefab(definition, out _)) return false;
        var lives = new List<int>();
        int before = owner.Revision;
        MergeItemObject result = null;
        bool committed = false;
        try
        {
            Vector3 position = Vector3.zero; Vector2 momentum = Vector2.zero; float mass = 0;
            foreach (var item in ingredients)
            {
                if (!item.Reserve()) return false;
                lives.Add(item.LifeId);
                position += item.transform.position; momentum += item.Velocity * item.body.mass; mass += item.body.mass;
            }
            result = owner.SpawnPiece(definition, position / ingredients.Count);
            if (result == null || !result.IsLive || before != owner.Revision || !owner.CanOperate) return false;
            for (int i = 0; i < ingredients.Count; i++)
                if (ingredients[i] == null || ingredients[i].LifeId != lives[i] || !ingredients[i].IsLive) return false;
            foreach (var item in ingredients)
            { item.PlayPresentation(MergeItemPresentationEvent.MergeConsumed); item.ReturnToPool(); }
            result.Velocity = momentum / Mathf.Max(.001f, mass); result.SetSimulated(true); committed = true;
            result.PlayPresentation(MergeItemPresentationEvent.MergeCreated);
            CodexDiscoveryHistory.Current.RecordItem(recipe.resultItem);
            if (owner.discoveryHistory != null) owner.discoveryHistory.RecordCrafted(recipe.resultItem);
            owner.Report($"합성: {owner.GetItemDisplayName(recipe.resultItem)}");
            return true;
        }
        finally
        {
            if (!committed && result != null) result.ReturnToPool();
            for (int i = 0; i < lives.Count; i++)
                if (ingredients[i] != null && ingredients[i].LifeId == lives[i]) ingredients[i].Unreserve();
        }
    }
}
