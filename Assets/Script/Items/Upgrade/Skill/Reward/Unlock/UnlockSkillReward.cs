using UnityEngine;

[CreateAssetMenu(
    fileName = "UnlockSkillReward",
    menuName = "GameData/Skill Tree/Reward/Unlock"
)]
public class UnlockSkillReward : SkillRewardData
{
    [Header("Unlock")]
    [SerializeField] private DataType unlockType;
    [SerializeField] private string unlockId;

    public DataType UnlockType => unlockType;
    public string UnlockId => unlockId;

    public override void Apply(SkillApplyContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("SkillApplyContext가 없습니다.");
            return;
        }

        if (context.unlockManager == null)
        {
            Debug.LogWarning("UnlockManager가 없습니다.");
            return;
        }

        context.unlockManager.Unlock(unlockType, unlockId);
    }

    public override void Remove(SkillApplyContext context)
    {
        if (context == null || context.unlockManager == null)
            return;

        context.unlockManager.Lock(unlockType, unlockId);
    }
}