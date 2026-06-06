using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffUISlot : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI stackText;

    [Header("Image")]
    public Image iconImage;
    public Image timeFillImage;

    [Header("Runtime")]
    public float refreshInterval = 0.1f;
    public bool hideWhenExpired = true;

    private ActiveBuff activeBuff;
    private string displayLabel;
    private float refreshTimer;

    public void Set(ActiveBuff activeBuff, string displayLabel)
    {
        this.activeBuff = activeBuff;
        this.displayLabel = displayLabel;
        refreshTimer = 0f;

        RefreshStaticInfo();
        RefreshRuntimeInfo();
    }

    private void Update()
    {
        if (activeBuff == null || activeBuff.IsExpired)
        {
            if (hideWhenExpired)
                gameObject.SetActive(false);
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        RefreshRuntimeInfo();
    }

    private void RefreshStaticInfo()
    {
        if (activeBuff == null)
            return;

        if (titleText != null)
            titleText.text = GetSourceName();

        if (targetText != null)
            targetText.text = GetTargetText();

        RefreshIcon();
    }

    private void RefreshRuntimeInfo()
    {
        if (activeBuff == null)
            return;

        if (timeText != null)
            timeText.text = GetRemainText();

        if (timeFillImage != null)
            timeFillImage.fillAmount = activeBuff.GetTimeRate();

        if (stackText != null)
            stackText.text = activeBuff.stack > 1 ? "x" + activeBuff.stack : "";
    }

    private string GetRemainText()
    {
        if (activeBuff.useLimitType == BuffUseLimitType.UseCount)
            return activeBuff.remainUseCount + "/" + activeBuff.maxUseCount;

        return activeBuff.remainTime.ToString("0.0");
    }

    private string GetSourceName()
    {
        if (activeBuff.sourceItemData != null)
            return activeBuff.sourceItemData.GetDataName();

        if (activeBuff.sourceEffectData != null)
            return activeBuff.sourceEffectData.name;

        return "Buff";
    }

    private string GetTargetText()
    {
        string targetName = activeBuff.target != null ? activeBuff.target.GetDebugName() : "Unknown";

        if (string.IsNullOrEmpty(displayLabel))
            return targetName;

        return displayLabel + " / " + targetName;
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        Sprite icon = activeBuff != null && activeBuff.sourceItemData != null ? activeBuff.sourceItemData.icon : null;
        iconImage.enabled = icon != null;

        if (icon != null)
            iconImage.sprite = icon;
    }
}
