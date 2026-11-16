using UnityEngine;

public class FoodMergeItemObject : MergeItemObject
{
    [SerializeField] private int burnedCount;
    [SerializeField] private MergeItem burningItem;
    [SerializeField] private FoodMergeItemBurningEffect burningEffect;
    public int BurnedCount => burnedCount;
    public MergeItem BurningItem => burningItem;
    protected override void ResetPoolState() => burnedCount = 0;

    public override void Initialize(MergegameManager owner, MergeItem definition, uint serial, float mergeDelay)
    {
        base.Initialize(owner, definition, serial, mergeDelay);

        burningEffect?.Bake(burnedCount);
    }
    public void Bake(int burnsBeforeDestroyed)
    {
        if (!CanMerge) return;
        burnedCount++;
        burningEffect?.Bake(burnedCount);
        // Burning Item은 가상 재료입니다. 실제 오브젝트나 재고를 소비하지 않습니다.
        if (burningItem != null && Owner is FoodMergeGameManager foodBoard && foodBoard.TryBake(this, burningItem)) return;
        if (burnedCount >= Mathf.Max(1, burnsBeforeDestroyed)) ReturnToPool();
    }
}
