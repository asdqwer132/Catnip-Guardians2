using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffUISlot : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI stackText;

    [Header("Images")]
    public Image iconImage;
    public Image timeFillImage;

    [Header("Options")]
    public string defaultBuffName = "Buff";

    private ActiveBuff activeBuff;

    public void Set(
        ActiveBuff activeBuff,
        string displayLabel
    )
    {
        this.activeBuff = activeBuff;


        RefreshStaticInfo();
        RefreshTimeInfo();
    }

    private void Update()
    {
        if (activeBuff == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (activeBuff.IsExpired)
        {
            gameObject.SetActive(false);
            return;
        }

        RefreshTimeInfo();
    }

    private void RefreshStaticInfo()
    {
        if (activeBuff == null)
            return;

        RefreshIcon();
    }

    private void RefreshTimeInfo()
    {
        if (activeBuff == null)
            return;

        if (timeText != null)
            timeText.text = activeBuff.remainTime.ToString("0.0");

        if (timeFillImage != null)
            timeFillImage.fillAmount = activeBuff.GetTimeRate();

        if (stackText != null)
        {
            if (activeBuff.stack > 1)
                stackText.text = "x" + activeBuff.stack;
            else
                stackText.text = "";
        }
    }

    private string GetBuffName()
    {
        if (activeBuff == null)
            return defaultBuffName;

        if (activeBuff.sourceEffectData != null)
            return activeBuff.sourceEffectData.name;

        return defaultBuffName;
    }
    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        Sprite icon = null;

        if (activeBuff != null &&
            activeBuff.sourceItemData != null)
        {
            icon = activeBuff.sourceItemData.icon;
        }

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }
}