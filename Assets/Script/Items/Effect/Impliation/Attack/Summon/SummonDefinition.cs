using UnityEngine;

// 이름이나 공통 BuffTargetGroup 대신 에셋 참조로 소환 종류를 구분한다.
[CreateAssetMenu(fileName = "SummonDefinition", menuName = "GameData/Items/Summons/Definition")]
public sealed class SummonDefinition : ScriptableObject
{
    public string[] tags;
    public bool enableHealth;
    [Min(0.01f)] public float maxHealth = 10f;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tags == null) return false;
        foreach (string candidate in tags)
            if (string.Equals(candidate, tag, System.StringComparison.Ordinal)) return true;
        return false;
    }
}
