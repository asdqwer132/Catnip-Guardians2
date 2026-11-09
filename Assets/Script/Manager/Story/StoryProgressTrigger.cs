using UnityEngine;
using UnityEngine.UI;

// Optional adapter for authored buttons and UnityEvents in any scene.
public sealed class StoryProgressTrigger : MonoBehaviour
{
    public Button confirmButton;
    public string confirmationArea = StoryProgressManager.PotionResult;
    private void Awake() { if (confirmButton != null) confirmButton.onClick.AddListener(Confirm); }
    private void OnDestroy() { if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm); }
    public void Confirm() => StoryProgressManager.Instance?.ConfirmArea(confirmationArea);
    public void MeetCondition(string ruleId) => StoryProgressManager.Instance?.MeetCondition(ruleId);
    public void ReportCustomEvent(string eventId) => StoryProgressManager.Instance?.ReportCustomEvent(eventId);
}
