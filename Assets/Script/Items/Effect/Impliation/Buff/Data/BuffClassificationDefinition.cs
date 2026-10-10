using UnityEngine;

[CreateAssetMenu(fileName = "BuffClassification", menuName = "GameData/Buffs/Classification/Rules")]
public sealed class BuffClassificationDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("해로운 버프인지 분류합니다. 특수 상태의 식별 키나 수치 Modifier와 별개입니다.")]
    public bool harmful;
    [Tooltip("끄면 모든 Cleanse 필터에서 보호합니다.")]
    public bool dispellable = true;
    [InspectorName("Cleanse Classification Tags")]
    [Tooltip("정화 필터가 비교하는 에셋 태그입니다. Star·Arrow 같은 특수 상태 키가 아닙니다.")]
    public BuffFlagDefinition[] flags;
}
