using UnityEngine;

public interface IBuffTarget
{
    Object BuffTargetObject { get; }
    // 그룹 문자열 또는 그룹 필드의 기본값은 Inspector 자동 선택에 수집된다.
    // 동적으로 계산하는 추가 그룹은 BuffTargetGroups 특성으로 선언할 수 있다.
    string BuffTargetGroup { get; }
    string BuffTargetDebugName { get; }
    void RefreshBuffedStat();
}
