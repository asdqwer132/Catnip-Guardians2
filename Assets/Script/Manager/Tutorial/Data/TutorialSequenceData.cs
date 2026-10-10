using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialSequenceData",
    menuName = "02.Settings/Tutoria Sequence Data"
)]
public class TutorialSequenceData : ScriptableObject
{
    [Header("Progress")]
    public TutorialProgress progress;

    [Header("Steps")]
    public TutorialSequenceStep[] steps;
}