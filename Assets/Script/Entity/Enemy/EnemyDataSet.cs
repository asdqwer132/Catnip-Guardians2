using System;
using UnityEngine;
[Serializable]

[CreateAssetMenu(fileName = "EnemySetData", menuName = "01.Entities/Enemy/Enemy Set Data")]
public class EnemyDataSet : ScriptableObject
{
    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    [Header("Data")]
    public EnemyStatData statData;
    public EnemyPatternSetData patternData;
}
