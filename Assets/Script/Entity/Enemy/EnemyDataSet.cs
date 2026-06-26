using System;
using UnityEditor.Animations;
using UnityEngine;
[Serializable]

[CreateAssetMenu(fileName = "EnemySetData", menuName = "GameData/Enemy/Enemy Set Data")]
public class EnemyDataSet : ScriptableObject
{
    [Header("Animation")]
    public AnimatorController animatorController;
    [Header("Data")]
    public EnemyStatData statData;
    public EnemyPatternSetData patternData;
}
