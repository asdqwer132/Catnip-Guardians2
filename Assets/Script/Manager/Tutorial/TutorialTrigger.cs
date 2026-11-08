using UnityEngine;
using UnityEngine.UI;

public enum TutorialTriggerMode { Manual, OnStart, OnEnable, ToggleSelected }

// Attach to an existing panel/toggle, or invoke Trigger from an arbitrary game event.
public sealed class TutorialTrigger : MonoBehaviour
{
    public TutorialCanvasController controller;
    public string tutorialId;
    public TutorialTriggerMode mode;
    public Toggle sourceToggle;
    private bool hasStarted;

    private void OnEnable()
    {
        if (sourceToggle != null) sourceToggle.onValueChanged.AddListener(TriggerWhenSelected);
        if (mode == TutorialTriggerMode.OnEnable) Trigger();
    }
    private void Start()
    {
        hasStarted = true;
        if (mode == TutorialTriggerMode.OnStart) Trigger();
    }
    private void OnDisable()
    {
        if (sourceToggle != null) sourceToggle.onValueChanged.RemoveListener(TriggerWhenSelected);
    }
    public void Trigger() { if (controller != null) controller.ShowTutorial(tutorialId); }
    public void TriggerWhenSelected(bool selected)
    {
        // Ignore ToggleGroup initialization before the first user-input frame.
        if (hasStarted && selected && mode == TutorialTriggerMode.ToggleSelected) Trigger();
    }
    public void TriggerIf(bool condition) { if (condition) Trigger(); }
}
