using UnityEngine;

[CreateAssetMenu(menuName = "Catnip/Board Gimmick Skill")]
public sealed class BoardGimmickSkillData : DefaultData
{
    public GimicSkillType skillType;
    [Min(0f)] public float cooldown;
    [Min(0f)] public float duration;

    public override string GetDescription(language selected)
    {
        bool korean = selected == language.korean;
        bool active = skillType == GimicSkillType.Active;
        string details = active ? (korean ? "액티브" : "Active") : (korean ? "패시브" : "Passive");
        if (cooldown > 0f)
        {
            string label = active ? (korean ? "재사용 대기시간" : "Cooldown") : (korean ? "반복 간격" : "Interval");
            details += $" · {label} {cooldown:0.##}{(korean ? "초" : " s")}";
        }
        if (duration > 0f)
            details += $" · {(korean ? "지속시간" : "Duration")} {duration:0.##}{(korean ? "초" : " s")}";
        string description = base.GetDescription(selected);
        return string.IsNullOrWhiteSpace(description) ? details : description + "\n" + details;
    }
}
