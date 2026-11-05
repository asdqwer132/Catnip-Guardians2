using System;
using UnityEngine;
[Serializable]

[CreateAssetMenu(fileName = "EnemySetData", menuName = "01.Entities/Enemy/Enemy Set Data")]
public class EnemyDataSet : DefaultData
{
    [Header("Extra Data / Animation")]
    public RuntimeAnimatorController animatorController;
    [Header("Extra Data / Data")]
    public EnemyStatData statData;
    public EnemyPatternSetData patternData;

    [Header("Extra Data / Codex Discovery")]
    [Tooltip("Optional display data recorded on defeat. Empty uses this enemy data set.")]
    public EnemyDataSet codexEntry;

    [Header("Extra Data / Codex Effect Information")]
    public CodexEffectDescription[] codexEffects;
}
