using UnityEngine;

public class FoodMergeItemObject : MergeItemObject
{
    [SerializeField] private int burnedCount;
    [SerializeField] private MergeItem burningItem;
    public int BurnedCount => burnedCount;
    public MergeItem BurningItem => burningItem;
    protected override void ResetPoolState() => burnedCount = 0;

    public void Bake(int burnsBeforeDestroyed)
    {
        if (!CanMerge) return;
        burnedCount++;
        // Burning Item은 가상 재료입니다. 실제 오브젝트나 재고를 소비하지 않습니다.
        if (burningItem != null && Owner is FoodMergeGameManager foodBoard && foodBoard.TryBake(this, burningItem)) return;
        if (burnedCount >= Mathf.Max(1, burnsBeforeDestroyed)) ReturnToPool();
    }
}
