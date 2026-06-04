using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TutorialProgressEvent
{
    [Header("Condition")]
    public TutorialProgress progress;

    [Header("Option")]
    public bool invokeOnlyOnce = true;

    [Header("Event")]
    public UnityEvent onReached;

    [HideInInspector]
    public bool hasInvoked;
}