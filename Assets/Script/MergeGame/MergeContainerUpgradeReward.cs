using UnityEngine;

[CreateAssetMenu(menuName = "03.Upgrade/Skills/Rewards/Merge Container", fileName = "ContainerUpgradeReward")]
public sealed class MergeContainerUpgradeReward : SkillRewardData
{
    [Tooltip("이 보상을 가진 SkillNodeData의 Data ID와 같게 설정합니다. 중복 구매/중복 적용 방지 키입니다.")]
    public string skillId;
    public string skillMapId = "MergeContainers";
    [Tooltip("비우면 모든 시리즈의 통에 적용합니다.")]
    public MergeContainerDefinition container;
    public MergeContainerUpgradeKind upgrade;
    [Tooltip("크기/주입속도/쿨다운은 0.2=20%, 주입구/드래그 반경은 더할 값입니다.")]
    [Min(0f)] public float amount = 0.2f;
    public override void Apply(SkillApplyContext context) => MergeGameSession.Ensure().upgrades.Add(this);
    public override void Remove(SkillApplyContext context)
    { if (MergeGameSession.Instance != null) MergeGameSession.Instance.upgrades.Remove(skillId); }
}
