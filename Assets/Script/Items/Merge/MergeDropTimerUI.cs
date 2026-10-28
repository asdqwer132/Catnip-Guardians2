using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 표시만 담당합니다. 실제 투입 주기는 MergeItemDropper에서 읽습니다.
public sealed class MergeDropTimerUI : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    public MergegameManager board;
    public TMP_Text timeText;
    public Image nextItemIcon;
    public Image progressImage;

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        var current = boxes != null ? boxes.SelectedBoard : board;
        if (timeText != null) timeText.SetText("{0:1}s", current != null ? current.DropCountdown : 0f);
        var item = current != null ? current.NextDropItem : null;
        if (nextItemIcon != null)
        {
            var sprite = item != null ? item.icon : null;
            if (nextItemIcon.sprite != sprite) nextItemIcon.sprite = sprite;
            nextItemIcon.enabled = sprite != null;
        }
        if (progressImage != null)
            progressImage.fillAmount = current != null ? 1f - Mathf.Clamp01(current.DropCountdown / current.DropInterval) : 0f;
    }
}
