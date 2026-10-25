using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [Header("Manager")]
    public InitManager initManager;
    public RoundManager roundManager;
    public GameStatisticsManager statisticsManager;
    public TutorialEventManager tutorialEventManager;

    private void Awake()
    {
        instance = this;
       
    }
    private void Start()
    {
        RetryGame();
    }

    public void Victory()
    {
        if (GameEndManager.instance != null && GameEndManager.instance.IsGameEnded)
            return;

        if (initManager != null)
            initManager.ResetEntity();

        if (roundManager != null)
            roundManager.Victory();
    }

    public void GameOver()
    {
        if (GameEndManager.instance != null && GameEndManager.instance.IsGameEnded)
            return;

        if (initManager != null)
            initManager.ResetEntity();

        if (tutorialEventManager != null)
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstItemEquip);

        if (roundManager != null)
            roundManager.GameOver();
    }

    /// <summary>
    /// 엔딩 패널의 Retry Game 버튼에 연결.
    /// </summary>
    public void RetryGame()
    {
        Time.timeScale = 1f;

        if (GameEndManager.instance != null)
            GameEndManager.instance.ResetGameEnd();

        if (initManager != null)
        {
            initManager.isInited = false;
            initManager.InitAll();
        }

        if (tutorialEventManager != null)
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstEnemyAttack);

        SceneAudioChannel.PlayBgm(gameObject, "Ambient 7 ");
    }

    /// <summary>
    /// 엔딩 패널의 Upgrade 버튼에 연결.
    /// </summary>
    public void ConfirmEnd()
    {
        if (GameEndManager.instance == null)
            return;

        if (!GameEndManager.instance.IsGameEnded)
            return;


        if (statisticsManager != null)
            statisticsManager.Confirm();


        SceneAudioChannel.PlayBgm(gameObject, "Ambient 6 ");
        GameEndManager.instance.OpenUpgradePanelFromEnd();
    }

    /// <summary>
    /// 업그레이드 패널에서 다시 게임을 시작하는 버튼이 필요하면 이걸 연결.
    /// </summary>
    public void StartGameFromUpgrade()
    {
        RetryGame();
    }
}