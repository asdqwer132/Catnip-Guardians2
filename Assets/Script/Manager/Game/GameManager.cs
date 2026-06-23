using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager")]
    public InitManager initManager;
    public ShopManager shopManager;
    public GameEndManager endManager;
    public RoundManager roundManager;
    public TutorialEventManager tutorialEventManager;

    private void Start()
    {
        initManager.GameInit();
        RetryGame();
    }

    public void Victory()
    {
        if (endManager != null && endManager.IsGameEnded)
            return;

        if (initManager != null)
            initManager.ResetEntity();

        if (roundManager != null)
            roundManager.Victory();
    }

    public void GameOver()
    {
        if (endManager != null && endManager.IsGameEnded)
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

        if (endManager != null)
            endManager.ResetGameEnd();

        if (initManager != null)
        {
            initManager.isInited = false;
            initManager.InitAll();
        }

        if (tutorialEventManager != null)
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstEnemyAttack);

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBgm("Ambient 7 ");
    }

    /// <summary>
    /// 엔딩 패널의 Upgrade 버튼에 연결.
    /// </summary>
    public void OpenUpgradePanelFromEnd()
    {
        if (endManager == null)
            return;

        if (!endManager.IsGameEnded)
            return;

        endManager.OpenUpgradePanelFromEnd();

        if (shopManager != null)
            shopManager.InitShop();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBgm("Ambient 6 ");
    }

    /// <summary>
    /// 업그레이드 패널에서 다시 게임을 시작하는 버튼이 필요하면 이걸 연결.
    /// </summary>
    public void StartGameFromUpgrade()
    {
        RetryGame();
    }
}