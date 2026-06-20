using UnityEngine;

[CreateAssetMenu(
    fileName = "BuffSkillReward",
    menuName = "GameData/Skill Tree/Reward/Buff"
)]
public class BuffSkillReward : SkillRewardData
{
    [Header("Buff Item")]
    public ItemData buffItemData;

    [Header("Target Bag")]
    public string bagId;

    public override void Apply(SkillApplyContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("SkillApplyContext가 없습니다.");
            return;
        }

        if (buffItemData == null)
        {
            Debug.LogWarning("등록할 버프 아이템이 없습니다.");
            return;
        }

        if (context.buffSkillManager == null)
        {
            Debug.LogWarning("SkillApplyContext에 BuffSkillManager가 없습니다.");
            return;
        }

        context.buffSkillManager.RegisterBuffItem(buffItemData, bagId);
    }
}