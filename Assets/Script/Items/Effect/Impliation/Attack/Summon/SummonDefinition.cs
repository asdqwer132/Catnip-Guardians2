using UnityEngine;

// 이름이나 공통 BuffTargetGroup 대신 에셋 참조로 소환 종류를 구분한다.
[CreateAssetMenu(fileName = "SummonDefinition", menuName = "04.Items/Effects/Direct Effects/Summons/Definition")]
public sealed class SummonDefinition : ScriptableObject
{
    public string[] tags;
    [Tooltip("같은 무리의 종류/개체 수를 셀 때 사용하는 그룹입니다. 비우면 무리 반응에 참여하지 않습니다.")]
    public SummonGroupDefinition summonGroup;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tags == null) return false;
        foreach (string candidate in tags)
            if (string.Equals(candidate, tag, System.StringComparison.Ordinal)) return true;
        return false;
    }
}
