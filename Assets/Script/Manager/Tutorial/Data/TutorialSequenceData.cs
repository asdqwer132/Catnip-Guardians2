using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialSequenceData",
    menuName = "Game/Tutorial/Sequence Data"
)]
public class TutorialSequenceData : ScriptableObject
{
    [Header("Progress")]
    public TutorialProgress progress;

    [Header("Steps")]
    public TutorialSequenceStep[] steps;
}