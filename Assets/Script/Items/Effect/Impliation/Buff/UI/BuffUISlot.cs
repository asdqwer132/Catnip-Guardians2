using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffUISlot : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI stackText;
    public Image iconImage;
    public Image timeFillImage;
    public float refreshInterval = 0.1f;

    private ActiveBuff activeBuff;
    private float refreshTimer;

    public void Set(ActiveBuff activeBuff, string displayLabel)
    {
        this.activeBuff = activeBuff;
        RefreshIcon();
        RefreshTimeInfo();
    }

    private void Update()
    {
        if (activeBuff == null || activeBuff.IsExpired)
        {
            gameObject.SetActive(false);
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        RefreshTimeInfo();
    }

    private void RefreshTimeInfo()
    {
        if (activeBuff == null)
            return;

        if (timeText != null)
            timeText.text = activeBuff.useLimitType == BuffUseLimitType.UseCount ? activeBuff.remainUseCount.ToString() : activeBuff.remainTime.ToString("0.0");

        if (timeFillImage != null)
            timeFillImage.fillAmount = activeBuff.GetTimeRate();

        if (stackText != null)
            stackText.text = activeBuff.stack > 1 ? "x" + activeBuff.stack : "";
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