using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [Header("Manager")]
    public InitManager initManager;
    [SerializeField] private Player player;
    [SerializeField] private Plant mainPlant;
    public Plant MainPlant => mainPlant;
    [SerializeField] private MergeBoxEquipmentData defaultChest;
    private bool growthCompletionPending;
    public RoundManager roundManager;
    public GameStatisticsManager statisticsManager;

    private void Awake()
    {
        instance = this;
       
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        RetryGame();
    }

    public void OnPlantGrowthCompleted() => growthCompletionPending = true;

    private void LateUpdate()
    {
        if (!growthCompletionPending) return;
        growthCompletionPending = false;
        if (GameEndManager.instance != null && GameEndManager.instance.IsGameEnded) return;

        Victory();
    }

    internal PlantData PrepareBattle(UpgradeToGeneralData data)
    {
        var chest = data?.equippedChest != null ? data.equippedChest : defaultChest;
        player?.ApplyPlayerData(chest != null ? chest.player : null);
        return chest != null && chest.plant != null ? chest.plant : defaultChest != null ? defaultChest.plant : null;
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


        if (roundManager != null)
            roundManager.GameOver();
    }

    /// <summary>
    /// 엔딩 패널의 Retry Game 버튼에 연결.
    /// </summary>
    public void RetryGame()
    {
        growthCompletionPending = false;
        GamePause.SetResumeTimeScale(1f);

        // Pay the finished battle before ResetRound clears its rewards.
        if (GameEndManager.instance != null && GameEndManager.instance.IsGameEnded)
            (statisticsManager != null ? statisticsManager : GameStatisticsManager.Instance)?.Confirm();

        GameSession.GetOrCreate().BeginRound();

        if (GameEndManager.instance != null)
            GameEndManager.instance.ResetGameEnd();

        if (initManager != null)
        {
            initManager.isInited = false;
            initManager.InitAll(this);
        }


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