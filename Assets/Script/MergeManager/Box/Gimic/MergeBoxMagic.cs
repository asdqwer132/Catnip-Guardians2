using UnityEngine;

public sealed class MergeBoxMagic : MergeBoxGimic
{
    [Min(0f)] public float unlimitedUseSeconds = 5f;
    [Min(0f)] public float rapidFeedingSeconds = 5f;
    [Min(1f)] public float feedingSpeedMultiplier = 3f;
    protected override void EnsureSkills() => SetDefaultSkills(
        new GimicSkill { id = "Magic.UnlimitedUse", type = GimicSkillType.Active, defaultSkillCool = 20f },
        new GimicSkill { id = "Magic.RapidFeeding", type = GimicSkillType.Active, defaultSkillCool = 20f });
    protected override float GetSkillDuration(int index) => index==0?unlimitedUseSeconds:rapidFeedingSeconds;
    protected override void OnSkillDurationEnded(int index)
    {
        if(!(Board is { } magic)) return;
        if(index==0) magic.ClearUnlimitedUse();else magic.ClearRapidFeeding();
    }
    protected override void OnActiveSkill(int index)
    {
        if (!(Board is { } magic)) return;
        if (index == 0) magic.ActivateUnlimitedUse(unlimitedUseSeconds);
        else if (index == 1) magic.ActivateRapidFeeding(rapidFeedingSeconds, feedingSpeedMultiplier);
    }
    public override void GimicStop()
    { if (Board is { } magic) magic.ClearMagic(); base.GimicStop(); }
}
