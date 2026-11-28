using UnityEngine;


public partial class MergegameManager
{
    private float retainUntil, rapidUntil;
    private float rapidMultiplier = 1f;
    public void ActivateUnlimitedUse(float seconds) => retainUntil = Mathf.Max(Time.time, retainUntil) + Mathf.Max(0f, seconds);
    public void ActivateRapidFeeding(float seconds, float multiplier)
    {
        rapidUntil = Mathf.Max(Time.time, rapidUntil) + Mathf.Max(0f, seconds);
        rapidMultiplier = Mathf.Max(1f, multiplier);
    }
    public void ClearUnlimitedUse() => retainUntil=0f;
    public void ClearRapidFeeding() { rapidUntil=0f;rapidMultiplier=1f; }
    public void ClearMagic() { retainUntil = rapidUntil = 0f; rapidMultiplier = 1f;  }
    public virtual bool RetainSelectedItems => Time.time < retainUntil;
    public virtual bool ForceAutomaticFeeding => Time.time < rapidUntil;
    public virtual float FeedingSpeedMultiplier => ForceAutomaticFeeding ? rapidMultiplier : 1f;
}
