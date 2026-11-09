using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class TutorialPanelSequence
{
    public string id;
    [Tooltip("Optional gameplay event, such as enemy.attacked-plant or merge.item.dropped.")]
    public string triggerEvent;
    [Tooltip("Optional source scene name. Empty accepts the event from any loaded scene.")]
    public string sourceScene;
    [Min(0f)] public float delaySeconds;
    [Tooltip("Request this tutorial when the scene controller starts.")]
    public bool startOnSceneEnter;
    [Tooltip("Request after this tutorial is completed or skipped. Review does not advance the sequence.")]
    public string afterTutorialId;
    public TutorialData data;
    public bool showOnce = true;
    public bool skipCountsAsCompleted = true;
    public string[] requiredTutorialIds;
    public UnityEvent onStarted = new UnityEvent();
    public UnityEvent onCompleted = new UnityEvent();
    public UnityEvent onSkipped = new UnityEvent();
}
