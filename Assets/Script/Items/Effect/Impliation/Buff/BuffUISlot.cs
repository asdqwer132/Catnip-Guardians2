using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffUISlot : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI displayLabelText;
    public TextMeshProUGUI buffNameText;
    public TextMeshProUGUI sourceText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI stackText;
    public TextMeshProUGUI statText;

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

        if (displayLabelText != null)
            displayLabelText.text = displayLabel;

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

        if (buffNameText != null)
            buffNameText.text = GetBuffName();

        if (sourceText != null)
            sourceText.text = GetSourceText();

        if (statText != null)
            statText.text = GetStatText();

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

    private string GetSourceText()
    {
        if (activeBuff == null)
            return "출처 없음";

        return "출처: " +
               activeBuff.GetSourceBagName() +
               " / " +
               activeBuff.GetSourceItemName();
    }

    private string GetStatText()
    {
        if (activeBuff == null)
            return "";

        if (activeBuff.buffStat == null)
            return "스탯 변화 없음";

        return activeBuff.buffStat.GetSummaryText();
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