using System;
using System.Collections.Generic;
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
    [Tooltip("Second, third, fourth upcoming items, in UI order.")]
    public Image[] nextExtraItemIcons = Array.Empty<Image>();
    private readonly List<ItemData> upcoming = new List<ItemData>(4);
    public Image progressImage;

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        var current = boxes != null ? boxes.SelectedBoard : board;
        float countdown = current != null ? current.DropCountdown : 0f;
        if (timeText != null) timeText.SetText("{0:1}s", countdown);
        if (current != null) current.GetUpcomingDropItems(upcoming, 1 + (nextExtraItemIcons?.Length ?? 0));
        else upcoming.Clear();
        SetIcon(nextItemIcon, 0);
        if (nextExtraItemIcons != null)
            for (int i = 0; i < nextExtraItemIcons.Length; i++) SetIcon(nextExtraItemIcons[i], i + 1);
        if (progressImage != null)
            progressImage.fillAmount = current != null ? 1f - Mathf.Clamp01(countdown / current.DropInterval) : 0f;
    }
    private void SetIcon(Image image, int index)
    {
        if (image == null) return;
        var sprite = index < upcoming.Count && upcoming[index] != null ? upcoming[index].icon : null;
        if (image.sprite != sprite) image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
