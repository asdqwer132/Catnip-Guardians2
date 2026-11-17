using UnityEngine;

// Growth timing belongs to PlantMergeGameManager; this pooled item displays it.
public sealed class PlantMergeItemObject : MergeItemObject
{
    [SerializeField] private SpriteCooldownController growthFill;
    public float GrowthProgress01 { get; private set; }
    public bool IsGrowing { get; private set; }

    public override void Initialize(MergegameManager owner, MergeItem definition, uint serial, float mergeDelay)
    {
        base.Initialize(owner, definition, serial, mergeDelay);
        ClearGrowthProgress();
    }

    public void SetGrowthProgress(float progress)
    {
        IsGrowing = true;
        GrowthProgress01 = Mathf.Clamp01(progress);
        growthFill?.SetFill(GrowthProgress01);
    }

    public void ClearGrowthProgress()
    {
        IsGrowing = false;
        GrowthProgress01 = 0f;
        growthFill?.SetFill(0f);
    }

    protected override void ResetPoolState() => ClearGrowthProgress();
}
