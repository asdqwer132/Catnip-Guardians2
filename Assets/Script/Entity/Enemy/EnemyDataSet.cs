using System;
using UnityEngine;
[Serializable]

[CreateAssetMenu(fileName = "EnemySetData", menuName = "Entities/Enemy/Enemy Set Data")]
public class EnemyDataSet : DefaultData
{
    [Space(10)]
    public RuntimeAnimatorController animatorController;
    [Space(10)]
    public EnemyStatData statData;
    public EnemyPatternSetData patternData;

    [Space(10)]
    [Tooltip("Optional display data recorded on defeat. Empty uses this enemy data set.")]
    public EnemyDataSet codexEntry;

    [Space(10)]
    public CodexEffectDescription[] codexEffects;
}
