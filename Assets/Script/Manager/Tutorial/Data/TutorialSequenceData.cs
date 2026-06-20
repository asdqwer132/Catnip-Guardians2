using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialSequenceData",
    menuName = "GameData/Tutoria Sequence Data"
)]
public class TutorialSequenceData : ScriptableObject
{
    [Header("Progress")]
    public TutorialProgress progress;

    [Header("Steps")]
    public TutorialSequenceStep[] steps;
}