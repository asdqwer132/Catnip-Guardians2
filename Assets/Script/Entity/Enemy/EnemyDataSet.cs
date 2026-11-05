using System;
using UnityEngine;
[Serializable]

[CreateAssetMenu(fileName = "EnemySetData", menuName = "01.Entities/Enemy/Enemy Set Data")]
public class EnemyDataSet : DefaultData
{
    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    [Header("Data")]
    public EnemyStatData statData;
    public EnemyPatternSetData patternData;

    [Header("Codex Discovery")]
    [Tooltip("Optional display data recorded on defeat. Empty uses this enemy data set.")]
    public EnemyDataSet codexEntry;

    [Header("Codex Effect Information")]
    public CodexEffectDescription[] codexEffects;
}
