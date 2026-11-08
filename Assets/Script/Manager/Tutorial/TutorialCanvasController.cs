using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialCanvasController : MonoBehaviour
{
    [Tooltip("May be on the same object as this controller. The Canvas component is hidden, not its GameObject.")]
    public Canvas tutorialCanvas;
    public Button previousButton;
    public Button nextButton;
    public Button skipButton;
    [Tooltip("Empty uses this scene's name. Use different scopes for Upgrade and General.")]
    public string completionScope;
    public TutorialPanelSequence[] tutorials = Array.Empty<TutorialPanelSequence>();

    private readonly Queue<string> pending = new Queue<string>();
    private TutorialPanelSequence current;
    private int pageIndex = -1;
    public bool IsShowing => current != null;
    public string CurrentTutorialId => current?.id;
    public int CurrentPageIndex => pageIndex;
    private string Scope => string.IsNullOrWhiteSpace(completionScope) ? gameObject.scene.name : completionScope;

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(Previous);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
    }

    private void OnEnable()
    {
        RefreshView();
        TutorialSignals.Raised += HandleSignal;
        TutorialSignals.ReplaySeen(HandleSignal);
    }

    private void OnDisable()
    {
        TutorialSignals.Raised -= HandleSignal;
        pending.Clear();
        current = null;
        pageIndex = -1;
        RefreshView();
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(Previous);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }

    // UnityEvent entry point. Busy tutorials are queued without interrupting the current one.
    public void ShowTutorial(string id) => TryShowTutorial(id);
    public bool TryShowTutorial(string id)
    {
        if (!isActiveAndEnabled || id == CurrentTutorialId || pending.Contains(id)) return false;
        var tutorial = GetTutorial(id);
        if (!CanShow(tutorial)) return false;
        if (current != null) { pending.Enqueue(id); return true; }
        current = tutorial;
        pageIndex = 0;
        RefreshView();
        tutorial.onStarted?.Invoke();
        return true;
    }

    public void Previous()
    {
        if (current == null || pageIndex <= 0) return;
        pageIndex--;
        RefreshView();
    }

    public void Next()
    {
        if (current == null) return;
        if (pageIndex + 1 >= current.pages.Length) { Finish(false); return; }
        pageIndex++;
        RefreshView();
    }

    public void Skip() { if (current != null) Finish(true); }

    // Closing/unloading never records an unfinished tutorial as completed.
    public void Cancel()
    {
        pending.Clear();
        current = null;
        pageIndex = -1;
        RefreshView();
    }

    private void Finish(bool skipped)
    {
        var finished = current;
        if (!skipped || finished.skipCountsAsCompleted) TutorialCompletionHistory.Complete(Scope, finished.id);
        current = null;
        pageIndex = -1;
        RefreshView();
        if (skipped) finished.onSkipped?.Invoke(); else finished.onCompleted?.Invoke();
        while (current == null && pending.Count > 0) TryShowTutorial(pending.Dequeue());
    }

    private TutorialPanelSequence GetTutorial(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || tutorials == null) return null;
        TutorialPanelSequence found = null;
        foreach (var tutorial in tutorials)
        {
            if (tutorial == null || tutorial.id != id) continue;
            if (found != null) return null; // Duplicate IDs are ambiguous.
            found = tutorial;
        }
        return found;
    }

    private bool CanShow(TutorialPanelSequence tutorial)
    {
        if (tutorialCanvas == null || tutorial?.panelRoot == null || tutorial.pages == null || tutorial.pages.Length == 0) return false;
        // Keep the controller alive while switching content panels.
        if (transform.IsChildOf(tutorial.panelRoot.transform)) return false;
        foreach (var page in tutorial.pages) if (page == null || transform.IsChildOf(page.transform) || page == tutorial.panelRoot) return false;
        if (tutorial.showOnce && TutorialCompletionHistory.IsCompleted(Scope, tutorial.id)) return false;
        if (tutorial.requiredTutorialIds != null)
            foreach (string required in tutorial.requiredTutorialIds)
                if (!TutorialCompletionHistory.IsCompleted(Scope, required)) return false;
        return true;
    }

    private void HandleSignal(Scene source, string eventId)
    {
        if (tutorials == null) return;
        foreach (var tutorial in tutorials)
            if (tutorial != null && tutorial.triggerEvent == eventId &&
                (string.IsNullOrEmpty(tutorial.sourceScene) || tutorial.sourceScene == source.name))
                TryShowTutorial(tutorial.id);
    }

    private void RefreshView()
    {
        if (tutorials != null)
            foreach (var tutorial in tutorials)
            {
                if (tutorial?.panelRoot == null || transform.IsChildOf(tutorial.panelRoot.transform)) continue;
                if (tutorial.pages != null)
                    for (int i = 0; i < tutorial.pages.Length; i++)
                    {
                        var page = tutorial.pages[i];
                        if (page != null && page != tutorial.panelRoot && !transform.IsChildOf(page.transform))
                            page.SetActive(tutorial == current && i == pageIndex);
                    }
                tutorial.panelRoot.SetActive(tutorial == current);
            }
        if (tutorialCanvas != null) tutorialCanvas.enabled = IsShowing;
        if (previousButton != null) previousButton.interactable = IsShowing && pageIndex > 0;
        if (nextButton != null) nextButton.interactable = IsShowing;
        if (skipButton != null) skipButton.interactable = IsShowing;
    }
}
