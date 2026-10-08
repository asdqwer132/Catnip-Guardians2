using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumeStatusEffect", menuName = "GameData/Items/Effects/Status/Consume Checked Status")]
public sealed class ConsumeStatusEffect : ItemEffectData
{
    [Tooltip("분기 조건에서 확인한 기존 상태만 제거합니다. 먼저 조건을 실행하세요.")]
    public HasStatusConditionData checkedCondition;
    public ItemEffectData[] afterConsumeEffects;

    public override void Prepare(ItemEffectContext context) => ItemEffectUtility.Prepare(context, afterConsumeEffects);

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || context.buffManager == null || checkedCondition == null) return;
        List<HasStatusConditionData.Match> matches = checkedCondition.TakeMatches(context);
        if (matches == null || matches.Count == 0) return;
        // 재부여된 상태와 새 상태는 같은 사용 중의 이전 상태 소비에 포함하지 않습니다.
        foreach (HasStatusConditionData.Match match in matches)
            if (match.buff == null || match.buff.IsExpired || match.buff.RegistrationVersion != match.version ||
                match.buff.StorageOwner != context.buffManager.Storage) return;
        List<ActiveBuff> checkedBuffs = new List<ActiveBuff>();
        foreach (HasStatusConditionData.Match match in matches) checkedBuffs.Add(match.buff);
        context.buffManager.RemoveBuffHandles(checkedBuffs, BuffRemovalReason.Consumed);
        ItemEffectUtility.Execute(afterConsumeEffects, context);
    }
}
