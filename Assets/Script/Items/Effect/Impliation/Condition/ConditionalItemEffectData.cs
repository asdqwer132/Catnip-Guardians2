using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalItemEffect", menuName = "GameData/Items/Effects/Branch/Conditional")]
public class ConditionalItemEffectData : ItemEffectData
{
    [Header("Branches")]
    [Tooltip("조건을 만족하면 실행할 A 이펙트. 비어 있으면 해당 분기는 실행하지 않습니다.")]
    public ItemEffectData effectWhenTrue;
    [Tooltip("조건을 만족하지 않으면 실행할 B 이펙트. 비어 있으면 해당 분기는 실행하지 않습니다.")]
    public ItemEffectData effectWhenFalse;
    [Tooltip("켜면 사용 전 준비 단계에서 분기를 고정합니다. 실행 중 새로 부여한 상태가 이 분기를 바꾸지 않습니다.")]
    public bool freezeBranchAtPrepare;

    protected override bool CanStart(ItemEffectContext context) => true;

    public override void Prepare(ItemEffectContext context)
    {
        if (context == null) return;
        context.plan.Prepare(SelectBranch(context), context);
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

        ItemEffectData selectedEffect = SelectBranch(context);
        if (selectedEffect != null)
            selectedEffect.Execute(context);
    }

    private ItemEffectData SelectBranch(ItemEffectContext context)
    {
        ItemEffectData selected;
        if (freezeBranchAtPrepare && context.plan.TryGetBranch(this, out selected)) return selected;
        selected = AreConditionsSatisfied(context) ? effectWhenTrue : effectWhenFalse;
        if (freezeBranchAtPrepare) context.plan.SetBranch(this, selected);
        return selected;
    }
}
