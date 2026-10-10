using UnityEngine;

[CreateAssetMenu(fileName = "SpecialStatusKey", menuName = "04.Items/Effects/Direct Effects/Special Status/Identity Key")]
public sealed class SpecialStatusDefinition : ScriptableObject
{
    [Tooltip("Star·Arrow처럼 특수 기능이 확인하는 상태의 식별 키입니다. 표시 이름이 같아도 다른 에셋이면 별개 상태입니다. 해로움·정화 가능 여부는 BuffClassificationDefinition에서 설정합니다.")]
    public string displayName;
    [Tooltip("특수 상태의 상호작용 정보입니다. 정화용 버프 분류 태그와 별개입니다.")]
    public string[] interactionTags;

    [Header("Legacy Player Status")]
    [Tooltip("켜면 PlayerStatus 대상에 부여한 이 상태를 기존 Has Buff 조건(arrow/star)에서도 인식합니다. StatusStat 수치는 변경하지 않습니다.")]
    public bool exposesPlayerStatus;
    public PlayerStatusList playerStatus;
}
