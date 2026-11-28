using UnityEngine;


public partial class MergegameManager
{
    public bool TryBake(FoodMergeItemObject food, MergeItem burningItem) => TryMergeWithIngredient(food, burningItem);
}
