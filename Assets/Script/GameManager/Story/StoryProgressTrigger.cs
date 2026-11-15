using UnityEngine;
using UnityEngine.UI;

// Optional adapter for authored buttons and UnityEvents in any scene.
public sealed class StoryProgressTrigger : MonoBehaviour, IStoryProgressSource
{
    public event System.Action<StoryProgressEvent> ProgressReported;
    private void OnEnable() => StoryProgressSources.Register(this);
    private void OnDisable() => StoryProgressSources.Unregister(this);
    public Button confirmButton;
    public string confirmationArea = StoryProgressManager.PotionResult;
    private void Awake() { if (confirmButton != null) confirmButton.onClick.AddListener(Confirm); }
    private void OnDestroy() { if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm); }
    public void Confirm() => StoryProgressManager.Instance?.ConfirmArea(confirmationArea);
    public void MeetCondition(string ruleId) => StoryProgressManager.Instance?.MeetCondition(ruleId);
    public void ReportCustomEvent(string eventId) => ProgressReported?.Invoke(new StoryProgressEvent(eventId));
}
