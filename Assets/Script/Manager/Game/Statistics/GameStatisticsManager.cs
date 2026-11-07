using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStatisticsManager : MonoBehaviour
{
    public static GameStatisticsManager Instance { get; private set; }
    public static event Action InstanceChanged;

    public StatisticsUI ui;
    [Header("Battle Progress Sources (same scene)")]
    public PlantManager plantManager;
    public GrowManager growManager;
    [SerializeField] private float battleTimeSeconds;
    [SerializeField] private float gameProgress01;
    [SerializeField] private float stageProgress01;
    [SerializeField] private int currentStageNumber;
    [SerializeField] private int totalStages;
    [SerializeField] private Sprite currentPlantSprite;
    private bool roundStarted;
    public float BattleTimeSeconds => battleTimeSeconds;
    public float GameProgress01 => gameProgress01;
    public float StageProgress01 => stageProgress01;
    public int CurrentStageNumber => currentStageNumber;
    public int TotalStages => totalStages;
    public Sprite CurrentPlantIcon => currentPlantSprite;
    public event Action OnProgressChanged;


    [Header("Round Statistics")]
    [SerializeField] private int roundKillCount;
    [SerializeField] private bool roundFinished;
    [SerializeField] private bool rewardsConfirmed;

    [Header("Round Currency")]
    [SerializeField] private List<RoundCurrencyInfo> roundCurrencies = new List<RoundCurrencyInfo>();

    public int RoundKillCount => roundKillCount;
    public int RoundGold => GetCurrency(CurrencyType.CurrencyMain);
    public IReadOnlyList<RoundCurrencyInfo> RoundCurrencies => roundCurrencies;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitCurrencies();
        InstanceChanged?.Invoke();
    }
    private void OnEnable()
    {
        OnChanged += RefreshUI;
    }

    private void OnDisable()
    {
        OnChanged -= RefreshUI;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        InstanceChanged?.Invoke();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitCurrencies();
    }
#endif
    private void Start()
    {
        RefreshUI();
    }
    public void Confirm()
    {
        if (CurrencyManager.instance == null || rewardsConfirmed) return;
        rewardsConfirmed = true;
        foreach (var currency in roundCurrencies)
        {
            CurrencyManager.instance.AddCurrency(currency.currencyType, currency.amount);
        }
    }
    public void ResetRound()
    {
        roundFinished = false;
        roundStarted = true;
        battleTimeSeconds = 0f;
        CaptureProgress();
        rewardsConfirmed = false;
        roundKillCount = 0;

        InitCurrencies();

        for (int i = 0; i < roundCurrencies.Count; i++)
        {
            roundCurrencies[i].amount = 0;
        }

        OnChanged?.Invoke();
    }
    private void Update()
    {
        if (!roundStarted || roundFinished || Time.deltaTime <= 0f) return;
        battleTimeSeconds += Time.deltaTime;
        CaptureProgress();
        OnProgressChanged?.Invoke();
    }
    private void CaptureProgress()
    {
        var selectedPlant = plantManager != null ? plantManager.CurrentPlant : null;
        totalStages = selectedPlant != null ? 1 : 0;
        currentStageNumber = totalStages;
        stageProgress01 = growManager != null ? Mathf.Clamp01(growManager.GrowProgress01) : 0f;
        gameProgress01 = stageProgress01;
        var visual = plantManager != null && plantManager.plant != null ? plantManager.plant.plantUI : null;
        currentPlantSprite = visual != null ? visual.CurrentSprite
            : selectedPlant != null ? selectedPlant.GetGrowthSprite(stageProgress01) : null;
    }
    public void FinishRound()
    {
        if (roundFinished) return;
        CaptureProgress();
        roundFinished = true;
        OnChanged?.Invoke();
        OnProgressChanged?.Invoke();
    }
    private void RefreshUI() => ui?.RefreshUI();
    public void AddKill(int amount = 1)
    {
        if (amount <= 0 || roundFinished)
            return;

        roundKillCount = (int)Math.Min(int.MaxValue, (long)roundKillCount + amount);

        OnChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        AddCurrency(CurrencyType.CurrencyMain, amount);
    }

    public void AddCurrency(CurrencyType currencyType, int amount)
    {
        if (amount <= 0 || roundFinished)
            return;

        RoundCurrencyInfo currency = GetCurrencyInfo(currencyType);

        if (currency == null)
        {
            currency = new RoundCurrencyInfo(currencyType, 0);
            roundCurrencies.Add(currency);
        }

        currency.amount = (int)Math.Min(int.MaxValue, (long)currency.amount + amount);

        OnChanged?.Invoke();
    }

    public int GetCurrency(CurrencyType currencyType)
    {
        RoundCurrencyInfo currency = GetCurrencyInfo(currencyType);

        if (currency == null)
            return 0;

        return currency.amount;
    }

    private RoundCurrencyInfo GetCurrencyInfo(CurrencyType currencyType)
    {
        for (int i = 0; i < roundCurrencies.Count; i++)
        {
            if (roundCurrencies[i].currencyType == currencyType)
                return roundCurrencies[i];
        }

        return null;
    }

    private void InitCurrencies()
    {
        Array values = Enum.GetValues(typeof(CurrencyType));

        for (int i = 0; i < values.Length; i++)
        {
            CurrencyType currencyType = (CurrencyType)values.GetValue(i);

            if (GetCurrencyInfo(currencyType) == null)
            {
                roundCurrencies.Add(new RoundCurrencyInfo(currencyType, 0));
            }
        }

        SortCurrencies();
    }

    private void SortCurrencies()
    {
        roundCurrencies.Sort((a, b) => a.currencyType.CompareTo(b.currencyType));
    }
}

[Serializable]
public class RoundCurrencyInfo
{
    public CurrencyType currencyType;
    public int amount;

    public RoundCurrencyInfo(CurrencyType currencyType, int amount)
    {
        this.currencyType = currencyType;
        this.amount = amount;
    }
}
