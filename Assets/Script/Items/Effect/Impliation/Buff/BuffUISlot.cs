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
    public TextMeshProUGUI statText;
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
                stackText.text = "x" + activeBuff.stack;
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
            return "";

        if (activeBuff.buffStat.attackBuffStat == null)
            return "";

        AttackBuffStat stat = activeBuff.buffStat.attackBuffStat;

        string result = "";

        if (stat.attackPower != 0f)
            result += "공격력 +" + stat.attackPower + " ";

        if (stat.attackPowerM != 0f)
            result += "공격력 x" + (1f + stat.attackPowerM).ToString("0.##") + " ";

        if (stat.attackRange != 0f)
            result += "범위 +" + stat.attackRange + " ";

        if (stat.attackRangeM != 0f)
            result += "범위 x" + (1f + stat.attackRangeM).ToString("0.##") + " ";

        if (stat.attackLifeTime != 0f)
            result += "지속 +" + stat.attackLifeTime + " ";

        if (stat.attackLifeTimeM != 0f)
            result += "지속 x" + (1f + stat.attackLifeTimeM).ToString("0.##") + " ";

        if (stat.damageInterval != 0f)
            result += "간격 +" + stat.damageInterval + " ";

        if (stat.damageIntervalM != 0f)
            result += "간격 x" + (1f + stat.damageIntervalM).ToString("0.##") + " ";

        if (string.IsNullOrEmpty(result))
            result = "스탯 변화 없음";

        return result;
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