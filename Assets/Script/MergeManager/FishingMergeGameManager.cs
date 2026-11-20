using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class FishingMergeGameManager : MergegameManager { }

public partial class MergegameManager
{
    public int DropFish(WeightedMergeItem[] fish, int count, Collider2D dropArea)
    {
        int spawned = 0;
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            var definition = MergeGimmickUtility.ChooseItem(fish);
            if (definition == null || dropArea == null) break;
            if (TrySpawnGimmickItem(definition, dropArea.bounds.center, out _)) spawned++;
        }
        return spawned;
    }
}
