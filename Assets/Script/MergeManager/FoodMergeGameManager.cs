using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class FoodMergeGameManager : MergegameManager
{
    public bool TryBake(FoodMergeItemObject food, MergeItem burningItem) => TryMergeWithIngredient(food, burningItem);
}
