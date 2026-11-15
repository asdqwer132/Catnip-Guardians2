using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Uses local authored UI references. Battle statistics register independently of scene load order.
public class StatisticsUI : MonoBehaviour
{
    public GameStatisticsManager statisticsManager;
    public CurrencyUIGroup[] currencyUIGroup;
    [Header("Result Display")]
    public TMP_Text battleTimeText;
    public TMP_Text killCountText;
    public TMP_Text progressText;
    public TMP_Text stageText;
    public TMP_Text stageProgressText;
    public ImageFillUI progressFill;
    public Image plantIcon;
    private GameStatisticsManager boundStatistics;

    private void OnEnable()
    {
        GameStatisticsManager.InstanceChanged += Bind;
        Bind();
    }
    private void OnDisable()
    {
        GameStatisticsManager.InstanceChanged -= Bind;
        Unbind();
    }
    private void Unbind()
    {
        if (boundStatistics != null)
        {
            boundStatistics.OnChanged -= RefreshUI;
            boundStatistics.OnProgressChanged -= RefreshProgress;
        }
        boundStatistics = null;
    }
    private void Bind()
    {
        Unbind();
        boundStatistics = statisticsManager != null ? statisticsManager : GameStatisticsManager.Instance;
        if (boundStatistics != null)
        {
            boundStatistics.OnChanged += RefreshUI;
            boundStatistics.OnProgressChanged += RefreshProgress;
        }
        RefreshUI();
    }
    public void RefreshUI()
    {
        var source = boundStatistics != null ? boundStatistics : statisticsManager;
        if (killCountText != null) killCountText.text = (source != null ? source.RoundKillCount : 0).ToString();
        if (currencyUIGroup != null)
            foreach (var group in currencyUIGroup)
            {
                if (group == null) continue;
                foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
                    group.UpdateUI(type, source != null ? source.GetCurrency(type) : 0);
            }
        RefreshProgress();
    }
    private void RefreshProgress()
    {
        var source = boundStatistics != null ? boundStatistics : statisticsManager;
        int seconds = Mathf.FloorToInt(source != null ? source.BattleTimeSeconds : 0f);
        if (battleTimeText != null)
            battleTimeText.text = seconds >= 3600 ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}"
                : $"{seconds / 60:00}:{seconds % 60:00}";
        float progress = source != null ? source.GameProgress01 : 0f;
        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        if (stageText != null) stageText.text = source != null ? $"{source.CurrentStageNumber} / {source.TotalStages}" : "0 / 0";
        if (stageProgressText != null) stageProgressText.text = $"{Mathf.RoundToInt((source != null ? source.StageProgress01 : 0f) * 100f)}%";
        if (progressFill != null) progressFill.SetFill01(progress);
        if (plantIcon != null) plantIcon.sprite = source != null ? source.CurrentPlantIcon : null;
    }
}
