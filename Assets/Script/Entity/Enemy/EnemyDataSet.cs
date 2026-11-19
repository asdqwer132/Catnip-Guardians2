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
    public CodexEffectDescription[] codexEffects;
}
