using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class FishingMergeGameManager : MergegameManager
{
    public int DropFish(WeightedMergeItem[] fish, int count, Collider2D dropArea)
    {
        int spawned = 0;
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            var definition = MergeGimmickUtility.ChooseItem(fish);
            if (definition == null || !MergeGimmickUtility.TryPoint(dropArea, out var position)) break;
            if (TrySpawnGimmickItem(definition, position, out _)) spawned++;
        }
        return spawned;
    }
}
