using UnityEngine;

// 에셋은 조건 설정만 보관한다. 대상이나 남은 시간 등의 실행 상태를 저장하지 않는다.
public abstract class ItemEffectConditionData : ScriptableObject
{
    // 구현 시 상태 조회만 수행하고 버프 소비, 피해, 연출 등의 동작을 실행하지 않는다.
    public abstract bool IsSatisfied(ItemEffectContext context);
}
