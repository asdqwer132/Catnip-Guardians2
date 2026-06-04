using System;
using UnityEngine;
public enum TutorialEventType
{
    None,

    DialogueShow,
    DialogueHide,
    DialogueSetText,
    DialogueSetSpeaker,
    DialogueClear,

    HighlightShow,
    HighlightHide,

    ObjectShow,
    ObjectHide,
    ObjectShowOnly,
    ObjectHideAll,

    EnemyStop,
    EnemyStart,

    GamePause,
    GameResume,

    ProgressNext,
    ProgressPrev,
    ProgressSet,

    SceneLoad
}
[Serializable]
public class TutorialSequenceStep
{
    [Header("Step")]
    public string stepName;

    [Header("Event")]
    public TutorialEventType eventType;

    [Header("Parameter")]
    public int intValue;
    public string stringValue;
    public bool boolValue;

    [Header("Option")]
    public bool waitForNextInput = true;
    public float autoNextDelay = 0f;
}