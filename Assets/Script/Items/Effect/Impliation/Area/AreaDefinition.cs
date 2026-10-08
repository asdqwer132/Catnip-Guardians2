using UnityEngine;

[CreateAssetMenu(fileName = "AreaDefinition", menuName = "GameData/Items/Areas/Definition")]
public sealed class AreaDefinition : ScriptableObject
{
    public string displayName;
    public string[] tags;
    public ReactiveGroundStat stat = new ReactiveGroundStat();
    public GameObject visualPrefab;
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
