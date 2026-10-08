using System.Collections.Generic;
using UnityEngine;

public enum AreaConsumeMode { FirstCheckedArea, AllCheckedAreas, AllMatchingOwnerAreas }

[CreateAssetMenu(fileName = "ConsumeAreaEffect", menuName = "GameData/Items/Effects/Area/Consume Checked Areas")]
public sealed class ConsumeAreaEffect : ItemEffectData
{
    public HasAreaConditionData checkedCondition;
    public AreaConsumeMode mode;
    public ItemEffectData[] afterConsumeEffects;
    public override void Prepare(ItemEffectContext context) => ItemEffectUtility.Prepare(context, afterConsumeEffects);

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || checkedCondition == null) return;
        List<ReactiveGroundArea> matches = checkedCondition.TakeMatches(context);
        if (matches == null || matches.Count == 0) return;
        // 확인한 장판이 이미 사라졌다면 새 장판을 대신 소비하지 않습니다.
        foreach (ReactiveGroundArea area in matches) if (area == null || !area.IsRegistered) return;
        if (mode == AreaConsumeMode.FirstCheckedArea)
            matches.RemoveRange(1, matches.Count - 1);
        else if (mode == AreaConsumeMode.AllMatchingOwnerAreas)
        {
            GameObject checkedOwner = matches[0].owner;
            List<ReactiveGroundArea> all = new List<ReactiveGroundArea>();
            AreaRegistry.Find(checkedCondition.definition, context, AreaQueryPosition.Anywhere,
                checkedCondition.ownerFilter, all);
            matches = all.FindAll(area => area.owner == checkedOwner);
        }
        if (matches.Count == 0) return;
        foreach (ReactiveGroundArea area in matches) area.Consume();
        ItemEffectUtility.Execute(afterConsumeEffects, context);
    }
}
