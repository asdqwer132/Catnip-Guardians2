using UnityEngine;

[CreateAssetMenu(fileName = "AreaDefinition", menuName = "04.Items/Effects/Direct Effects/Area/Definition")]
public sealed class AreaDefinition : ScriptableObject
{
    public string displayName;
    public string[] tags;
    [Header("영역 기본 스탯")]
    public ReactiveGroundStat stat = new ReactiveGroundStat();
    [Header("영역 수명 동안 표시할 비주얼")]
    [Tooltip("단위 지름으로 제작한 프리팹을 연결하세요. 등장/유지/종료 연출은 프리팹의 PrefabVisualAnimator에서 설정합니다.")]
    public GameObject visualPrefab;
    public bool scaleVisualByRadius = true;
    [Header("영역 생명주기 효과")]
    public ItemEffectData[] onStart;
    public ItemEffectData[] onEnter;
    public ItemEffectData[] onTick;
    public ItemEffectData[] onExit;
    [Tooltip("수명으로 자연 종료될 때만 실행합니다. 소비/취소 시 실행하지 않습니다.")]
    public ItemEffectData[] onNaturalEnd;
    public LayerMask targetMask = ~0;

    [Header("Enemy Residence")]
    [Tooltip("0이면 체류 발동을 사용하지 않습니다.")]
    [Min(0)] public float residenceThreshold;
    public bool cumulativeResidence;
    public bool resetResidenceOnExit = true;
    [Min(1)] public int maxResidenceTriggersPerTarget = 1;
    public bool removeAfterResidenceTrigger;
    public ItemEffectData[] onResidence;
}
