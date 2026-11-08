using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class TutorialPanelSequence
{
    public string id;
    [Tooltip("Optional gameplay event, such as enemy.spawned or merge.item.dropped.")]
    public string triggerEvent;
    [Tooltip("Optional source scene name. Empty accepts the event from any loaded scene.")]
    public string sourceScene;
    public GameObject panelRoot;
    public GameObject[] pages;
    public bool showOnce = true;
    public bool skipCountsAsCompleted = true;
    public string[] requiredTutorialIds;
    public UnityEvent onStarted = new UnityEvent();
    public UnityEvent onCompleted = new UnityEvent();
    public UnityEvent onSkipped = new UnityEvent();
}
