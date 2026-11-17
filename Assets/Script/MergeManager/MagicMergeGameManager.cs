using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class MagicMergeGameManager : MergegameManager
{
    private float retainUntil, rapidUntil;
    private float rapidMultiplier = 1f;
    public void ActivateUnlimitedUse(float seconds) => retainUntil = Mathf.Max(Time.time, retainUntil) + Mathf.Max(0f, seconds);
    public void ActivateRapidFeeding(float seconds, float multiplier)
    {
        rapidUntil = Mathf.Max(Time.time, rapidUntil) + Mathf.Max(0f, seconds);
        rapidMultiplier = Mathf.Max(1f, multiplier); RefreshFeedingTiming();
    }
    public void ClearMagic() { retainUntil = rapidUntil = 0f; rapidMultiplier = 1f; RefreshFeedingTiming(); }
    protected override void OnDisable() { ClearMagic(); base.OnDisable(); }
    public override bool RetainSelectedItems => Time.time < retainUntil;
    public override bool ForceAutomaticFeeding => Time.time < rapidUntil;
    public override float FeedingSpeedMultiplier => ForceAutomaticFeeding ? rapidMultiplier : 1f;
}
