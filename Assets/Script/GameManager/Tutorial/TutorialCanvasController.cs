using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialCanvasController : MonoBehaviour
{
    public Canvas tutorialCanvas;
    public TutorialPageView tutorialView;
    public TutorialPageView reviewView;
    [Header("Review Selection")]
    public Transform selectionParent;
    public TutorialSelectionToggle selectionTemplate;
    public ToggleGroup selectionGroup;
    [SerializeField] private List<TutorialSelectionToggle> selectionSlots = new List<TutorialSelectionToggle>();
    [Tooltip("Empty uses this scene's name. Use different scopes for Upgrade and General.")]
    public string completionScope;
    public TutorialPanelSequence[] tutorials = Array.Empty<TutorialPanelSequence>();

    private readonly Queue<string> pending = new Queue<string>();
    private readonly Dictionary<string, Coroutine> scheduled = new Dictionary<string, Coroutine>();
    private TutorialPanelSequence current;
    private int pageIndex = -1;
    public bool IsShowing => current != null || IsReviewing;
    public bool IsReviewing { get; private set; }
    public string CurrentTutorialId => current?.id;
    public int CurrentPageIndex => pageIndex;
    public IReadOnlyList<TutorialSelectionToggle> SelectionSlots => selectionSlots;
    private string Scope => string.IsNullOrWhiteSpace(completionScope) ? gameObject.scene.name : completionScope;

    private void Awake()
    {
        ConnectButtons(tutorialView, true);
        ConnectButtons(reviewView, true);
    }
    private void ConnectButtons(TutorialPageView view, bool add)
    {
        if (view == null) return;
        if (add)
        {
            if (view.previousButton != null) view.previousButton.onClick.AddListener(Previous);
            if (view.nextButton != null) view.nextButton.onClick.AddListener(Next);
            if (view.closeButton != null) view.closeButton.onClick.AddListener(Skip);
        }
        else
        {
            if (view.previousButton != null) view.previousButton.onClick.RemoveListener(Previous);
            if (view.nextButton != null) view.nextButton.onClick.RemoveListener(Next);
            if (view.closeButton != null) view.closeButton.onClick.RemoveListener(Skip);
        }
    }
    private void OnEnable()
    {
        RefreshView();
        TutorialSignals.Raised += HandleSignal;
        TutorialSignals.ReplaySeen(HandleSignal);
    }
    private void Start()
    {
        if (tutorials == null) return;
        foreach (var tutorial in tutorials)
            if (tutorial != null && (tutorial.startOnSceneEnter ||
                (!string.IsNullOrEmpty(tutorial.afterTutorialId) && TutorialCompletionHistory.IsCompleted(Scope, tutorial.afterTutorialId))))
                RequestTutorial(tutorial.id);
    }
    private void OnDisable()
    {
        TutorialSignals.Raised -= HandleSignal;
        Cancel();
    }
    private void OnDestroy()
    {
        GamePause.Release(this);
        ConnectButtons(tutorialView, false);
        ConnectButtons(reviewView, false);
    }

    public void ShowTutorial(string id) => RequestTutorial(id);

    public bool RequestTutorial(string id)
    {
        if (!isActiveAndEnabled || (!IsReviewing && id == CurrentTutorialId) || pending.Contains(id) ||
            string.IsNullOrWhiteSpace(id) || scheduled.ContainsKey(id)) return false;
        var tutorial = GetTutorial(id);
        if (!CanAccept(tutorial)) return false;
        float delay = EffectStatUtility.Safe(tutorial.delaySeconds, 0f, 86400f, 0f);
        if (delay <= 0f && PrerequisitesComplete(tutorial)) return TryShowTutorial(id);
        scheduled.Add(id, StartCoroutine(ShowAfterDelay(id, delay)));
        return true;
    }

    private IEnumerator ShowAfterDelay(string id, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var tutorial = GetTutorial(id);
        while (isActiveAndEnabled && CanAccept(tutorial) && !PrerequisitesComplete(tutorial)) yield return null;
        scheduled.Remove(id);
        if (CanAccept(tutorial)) TryShowTutorial(id);
    }

    public void CancelRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (scheduled.TryGetValue(id, out var routine))
        {
            StopCoroutine(routine);
            scheduled.Remove(id);
        }
        int count = pending.Count;
        for (int i = 0; i < count; i++)
        {
            string next = pending.Dequeue();
            if (next != id) pending.Enqueue(next);
        }
    }
    public bool TryShowTutorial(string id)
    {
        if (!isActiveAndEnabled || (!IsReviewing && id == CurrentTutorialId) || pending.Contains(id)) return false;
        var tutorial = GetTutorial(id);
        if (!CanShow(tutorial)) return false;
        if (IsShowing) { pending.Enqueue(id); return true; }
        current = tutorial;
        pageIndex = 0;
        TutorialCompletionHistory.RecordSeen(Scope, tutorial.id);
        RefreshView();
        tutorial.onStarted?.Invoke();
        return true;
    }

    public void OpenReview()
    {
        if (!isActiveAndEnabled || tutorialCanvas == null || reviewView == null || (!IsReviewing && current != null)) return;
        IsReviewing = true;
        current = null;
        pageIndex = -1;
        int count = 0;
        if (tutorials != null)
            foreach (var tutorial in tutorials)
            {
                if (!IsValid(tutorial) || !TutorialCompletionHistory.HasSeen(Scope, tutorial.id)) continue;
                var slot = GetSelectionSlot(count++);
                if (slot != null) { slot.Bind(this, tutorial); slot.gameObject.SetActive(true); }
                if (current == null) { current = tutorial; pageIndex = 0; }
            }
        for (int i = count; i < selectionSlots.Count; i++)
            if (selectionSlots[i] != null) { selectionSlots[i].Bind(this, null); selectionSlots[i].gameObject.SetActive(false); }
        RefreshView();
        RefreshSelection();
    }
    public void ToggleReview() { if (IsReviewing) CloseReview(); else OpenReview(); }
    public void SelectReviewTutorial(string id)
    {
        if (!IsReviewing) return;
        var tutorial = GetTutorial(id);
        if (!IsValid(tutorial) || !TutorialCompletionHistory.HasSeen(Scope, id)) return;
        current = tutorial;
        pageIndex = 0;
        RefreshView();
        RefreshSelection();
    }
    private void RefreshSelection()
    {
        if (!IsReviewing) return;
        // Slots follow the filtered tutorial order, not the full registration array.
        int index = 0;
        if (tutorials != null)
            foreach (var tutorial in tutorials)
                if (IsValid(tutorial) && TutorialCompletionHistory.HasSeen(Scope, tutorial.id))
                {
                    if (index < selectionSlots.Count && selectionSlots[index] != null)
                        selectionSlots[index].SetSelected(current == tutorial);
                    index++;
                }
    }
    public void CloseReview()
    {
        if (!IsReviewing) return;
        IsReviewing = false;
        current = null;
        pageIndex = -1;
        RefreshView();
        PlayPending();
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
        if (pageIndex + 1 >= current.data.pages.Length)
        {
            if (!IsReviewing) Finish(false);
            return;
        }
        pageIndex++;
        RefreshView();
    }
    public void Skip()
    {
        if (IsReviewing) CloseReview();
        else if (current != null) Finish(true);
    }
    public void Cancel()
    {
        foreach (var routine in scheduled.Values) StopCoroutine(routine);
        scheduled.Clear();
        pending.Clear();
        current = null;
        IsReviewing = false;
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
        if (tutorials != null)
            foreach (var tutorial in tutorials)
                if (tutorial != null && tutorial.afterTutorialId == finished.id) RequestTutorial(tutorial.id);
        PlayPending();
    }
    private void PlayPending()
    {
        while (!IsShowing && pending.Count > 0) TryShowTutorial(pending.Dequeue());
    }
    private TutorialPanelSequence GetTutorial(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || tutorials == null) return null;
        TutorialPanelSequence found = null;
        foreach (var tutorial in tutorials)
        {
            if (tutorial == null || tutorial.id != id) continue;
            if (found != null) return null;
            found = tutorial;
        }
        return found;
    }
    private bool IsValid(TutorialPanelSequence tutorial) => tutorial != null && !string.IsNullOrWhiteSpace(tutorial.id)
        && tutorial.data != null && tutorial.data.HasPages && GetTutorial(tutorial.id) == tutorial;
    private bool CanAccept(TutorialPanelSequence tutorial)
    {
        if (tutorialCanvas == null || tutorialView == null || !IsValid(tutorial)) return false;
        return !tutorial.showOnce || !TutorialCompletionHistory.IsCompleted(Scope, tutorial.id);
    }
    private bool PrerequisitesComplete(TutorialPanelSequence tutorial)
    {
        if (tutorial.requiredTutorialIds != null)
            foreach (string required in tutorial.requiredTutorialIds)
                if (!TutorialCompletionHistory.IsCompleted(Scope, required)) return false;
        return true;
    }
    private bool CanShow(TutorialPanelSequence tutorial) => CanAccept(tutorial) && PrerequisitesComplete(tutorial);
    private void HandleSignal(Scene source, string eventId)
    {
        if (tutorials == null) return;
        foreach (var tutorial in tutorials)
            if (tutorial != null && tutorial.triggerEvent == eventId &&
                (string.IsNullOrEmpty(tutorial.sourceScene) || tutorial.sourceScene == source.name)) RequestTutorial(tutorial.id);
    }
    private TutorialSelectionToggle GetSelectionSlot(int index)
    {
        if (index < selectionSlots.Count) return selectionSlots[index];
        if (selectionTemplate == null || selectionParent == null) return null;
        var slot = Instantiate(selectionTemplate, selectionParent);
        if (slot.toggle != null) slot.toggle.group = selectionGroup;
        selectionSlots.Add(slot);
        return slot;
    }
    private void RefreshView()
    {
        if (IsShowing) GamePause.Acquire(this);
        else GamePause.Release(this);
        Display(tutorialView, !IsReviewing && current != null);
        Display(reviewView, IsReviewing);
        if (tutorialCanvas != null) tutorialCanvas.enabled = IsShowing;
    }
    private void Display(TutorialPageView view, bool visible)
    {
        if (view == null) return;
        if (!visible) view.Clear();
        view.gameObject.SetActive(visible);
        if (visible && current != null) view.Show(current.data, pageIndex);
        else if (visible) view.Clear();
        if (view.previousButton != null) view.previousButton.interactable = visible && pageIndex > 0;
        if (view.nextButton != null) view.nextButton.interactable = visible && current != null &&
            (!IsReviewing || pageIndex + 1 < current.data.pages.Length);
        if (view.closeButton != null) view.closeButton.interactable = visible;
    }
}
