using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPatternSetData", menuName = "GameData/Enemy/Enemy Pattern/Enemy Pattern Set Data")]
public class EnemyPatternSetData : ScriptableObject
{
    [Header("Pattern Cooldown Random")]
    [Min(0.05f)] public float minPatternCooldown = 3f;
    [Min(0.05f)] public float maxPatternCooldown = 5f;

    [Header("Execution")]
    public bool cancelDefaultAttackOnPatternStart = true;
    public bool stopMoveOnPatternStart = false;

    [Header("Debug")]
    public bool showLog = false;

    [Header("Patterns")]
    public List<EnemyPatternEntry> patterns = new List<EnemyPatternEntry>();

    private void OnValidate()
    {
        minPatternCooldown = Mathf.Max(0.05f, minPatternCooldown);
        maxPatternCooldown = Mathf.Max(0.05f, maxPatternCooldown);

    }

}