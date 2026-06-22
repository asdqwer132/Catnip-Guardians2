using System;
using System.Text;
using TMPro;
using UnityEngine;

public enum GameEndType
{
    None,
    Win,
    Lose
}

public class GameEndManager : MonoBehaviour
{

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;

    [Header("Result Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text currencyText;

    [Header("Upgrade Panel")]
    [SerializeField] private GameObject upgradePanel;

    [Header("Option")]
    [SerializeField] private bool hidePanelOnAwake = true;
    [SerializeField] private bool pauseGameOnEnd = true;

    private bool isGameEnded;
    private GameEndType currentEndType = GameEndType.None;

    public bool IsGameEnded => isGameEnded;
    public GameEndType CurrentEndType => currentEndType;

    public event Action<GameEndType> OnGameEnded;

    private void Awake()
    {

        if (hidePanelOnAwake)
        {
            HideResultPanel();
            HideUpgradePanel();
        }
    }


    public void EndGame(GameEndType endType)
    {
        if (isGameEnded)
            return;

        isGameEnded = true;
        currentEndType = endType;

        RefreshResultUI();
        ShowResultPanel();
        HideUpgradePanel();

        if (pauseGameOnEnd)
            Time.timeScale = 0f;

        SetCursorDefault();

        OnGameEnded?.Invoke(endType);
    }

    public void WinGame()
    {
        EndGame(GameEndType.Win);
    }

    public void LoseGame()
    {
        EndGame(GameEndType.Lose);
    }

    /// <summary>
    /// 게임 종료 상태만 초기화한다.
    /// 여기서 다음 라운드 시작, 업그레이드 시작 같은 흐름을 처리하면 안 됨.
    /// </summary>
    public void ResetGameEnd()
    {
        isGameEnded = false;
        currentEndType = GameEndType.None;

        Time.timeScale = 1f;

        HideResultPanel();
        HideUpgradePanel();
    }

    /// <summary>
    /// 엔딩 패널의 Upgrade 버튼에서 호출될 함수.
    /// </summary>
    public void OpenUpgradePanelFromEnd()
    {
        if (!isGameEnded)
            return;

        Time.timeScale = 1f;

        HideResultPanel();
        ShowUpgradePanel();

        SetCursorDefault();
    }

    public void CloseUpgradePanel()
    {
        HideUpgradePanel();
        SetCursorAttack();
    }

    private void ShowResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    private void HideResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void ShowUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
    }

    private void HideUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    private void RefreshResultUI()
    {
        RefreshTitle();
        RefreshStatistics();
    }

    private void RefreshTitle()
    {
        if (titleText == null)
            return;

        switch (currentEndType)
        {
            case GameEndType.Win:
                titleText.text = "라운드 클리어";
                break;

            case GameEndType.Lose:
                titleText.text = "게임 오버";
                break;

            default:
                titleText.text = "결과";
                break;
        }
    }

    private void RefreshStatistics()
    {
        if (GameStatisticsManager.Instance == null)
            return;

        if (killCountText != null)
            killCountText.text = $"처치한 적 : {GameStatisticsManager.Instance.RoundKillCount}";

        if (goldText != null)
            goldText.text = $"획득 골드 : {GameStatisticsManager.Instance.RoundGold}";

        if (currencyText != null)
            currencyText.text = MakeCurrencyText();
    }

    private string MakeCurrencyText()
    {
        if (GameStatisticsManager.Instance == null)
            return "획득 재화 없음";

        StringBuilder builder = new StringBuilder();

        var currencies = GameStatisticsManager.Instance.RoundCurrencies;

        for (int i = 0; i < currencies.Count; i++)
        {
            RoundCurrencyInfo currency = currencies[i];

            if (currency == null)
                continue;

            if (currency.amount <= 0)
                continue;

            builder.Append(currency.currencyType);
            builder.Append(" : ");
            builder.Append(currency.amount);
            builder.AppendLine();
        }

        if (builder.Length == 0)
            return "획득 재화 없음";

        return builder.ToString();
    }

    private void SetCursorDefault()
    {
        if (CursorChanger.instance != null)
            CursorChanger.instance.SetCursor(CursorType.Default);
    }

    private void SetCursorAttack()
    {
        if (CursorChanger.instance != null)
            CursorChanger.instance.SetCursor(CursorType.Attack);
    }
}