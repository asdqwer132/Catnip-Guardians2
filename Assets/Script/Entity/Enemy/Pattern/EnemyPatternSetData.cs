using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPatternSetData", menuName = "GameData/Enemy/Enemy Pattern/Enemy Pattern Set Data")]
public class EnemyPatternSetData : ScriptableObject
{
    [Header("Pattern Cooldown")]
    [Min(0.05f)] public float patternCooldown = 4f;

    [Header("Random2 Option")]
    public bool useRandom2OnlyBelowHp = false;
    [Range(0f, 1f)] public float random2HpRatio = 0.5f;

    [Header("Execution")]
    public bool cancelDefaultAttackOnPatternStart = true;
    public bool stopMoveOnPatternStart = false;

    [Header("Debug")]
    public bool showLog = false;

    [Header("Patterns")]
    public List<EnemyPatternEntry> patterns = new List<EnemyPatternEntry>();
}
