using UnityEngine;

[CreateAssetMenu(fileName = "StatusDefinition", menuName = "GameData/Items/Status/Definition")]
public sealed class StatusDefinition : ScriptableObject
{
    [Tooltip("상태 전용 부여와 수치 버프가 공유하는 상태 정의입니다. 같은 표시 이름이어도 에셋 키가 다르면 서로 다른 상태입니다.")]
    public string displayName;
    public bool harmful;
    public bool dispellable = true;
    public string[] interactionTags;

    [Header("Legacy Player Status")]
    [Tooltip("켜면 PlayerStatus 대상에 부여한 이 상태를 기존 Has Buff 조건(arrow/star)에서도 인식합니다. StatusStat 수치는 변경하지 않습니다.")]
    public bool exposesPlayerStatus;
    public PlayerStatusList playerStatus;
}
