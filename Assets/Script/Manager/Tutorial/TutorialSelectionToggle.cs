using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialSelectionToggle : MonoBehaviour
{
    public Toggle toggle;
    public DataInfoUI label;
    private TutorialCanvasController controller;
    private string tutorialId;
    private void Awake() { if (toggle != null) toggle.onValueChanged.AddListener(OnSelected); }
    private void OnDestroy() { if (toggle != null) toggle.onValueChanged.RemoveListener(OnSelected); }
    public void Bind(TutorialCanvasController owner, TutorialPanelSequence tutorial)
    {
        controller = owner;
        tutorialId = tutorial?.id;
        if (label != null) label.SetData(tutorial?.data);
        SetSelected(false);
    }
    public void SetSelected(bool selected) { if (toggle != null) toggle.SetIsOnWithoutNotify(selected); }
    private void OnSelected(bool selected) { if (selected && controller != null) controller.SelectReviewTutorial(tutorialId); }
}
