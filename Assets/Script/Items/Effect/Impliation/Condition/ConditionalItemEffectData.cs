using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalItemEffect", menuName = "GameData/Items/Effects/Branch/Conditional")]
public class ConditionalItemEffectData : ItemEffectData
{
    [Header("Branches")]
    [Tooltip("조건을 만족하면 실행할 A 이펙트. 비어 있으면 해당 분기는 실행하지 않습니다.")]
    public ItemEffectData effectWhenTrue;
    [Tooltip("조건을 만족하지 않으면 실행할 B 이펙트. 비어 있으면 해당 분기는 실행하지 않습니다.")]
    public ItemEffectData effectWhenFalse;

    protected override bool CanStart(ItemEffectContext context) => true;

    public override void Prepare(ItemEffectContext context)
    {
        context.plan.Prepare(AreConditionsSatisfied(context) ? effectWhenTrue : effectWhenFalse, context);
    }

    protected override void ExecuteWithConditions(ItemEffectContext context)
    {
        // 이 에셋의 조건은 실행 차단이 아닌 A/B 선택에 사용한다.
        // 분기 에셋 자체의 Impact VFX는 재생하지 않고 선택한 이펙트만 실행한다.
        ExecuteEffect(context);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null)
            return;

        bool conditionsSatisfied = AreConditionsSatisfied(context);
        ItemEffectData selectedEffect = conditionsSatisfied ? effectWhenTrue : effectWhenFalse;
        if (selectedEffect != null)
            selectedEffect.Execute(context);
    }
}
