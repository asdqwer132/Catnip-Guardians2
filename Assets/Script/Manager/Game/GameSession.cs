using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>씬이 바뀌어도 유지되는 아이템 전달 데이터입니다. 씬 오브젝트는 보관하지 않습니다.</summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance;
    public static event Action InstanceChanged;
    public event Action GameDataChanged;
    public event Action BattleItemsAvailable;

    [Header("Upgrade → General")]
    [SerializeField] private UpgradeToGeneralData generalData = new UpgradeToGeneralData();
    [SerializeField] private bool hasGeneralData;

    [Header("Game Item → Battle")]
    [Tooltip("배틀 씬에서 아직 받지 않은 아이템 묶음입니다. 먼저 보낸 순서로 전달됩니다.")]
    [SerializeField] private List<GameItemToBattleData> pendingBattleItems = new List<GameItemToBattleData>();
    [Tooltip("수신 즉시 대기 목록이 비워져도 마지막 송신 내용을 확인할 수 있습니다.")]
    [SerializeField] private GameItemToBattleData lastSentToBattle;
    [SerializeField] private GameItemToBattleData lastReceivedByBattle;

    public int GameDataRevision { get; private set; }
    public int PendingBattleBatchCount => pendingBattleItems.Count;
    public bool HasSkillItems =>
        generalData != null && generalData.skillItems != null && generalData.skillItems.Count > 0;
    public GameItemToBattleData LastSentToBattle => lastSentToBattle?.Copy();
    public GameItemToBattleData LastReceivedByBattle => lastReceivedByBattle?.Copy();

    public static GameSession GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        // 독립 씬 테스트에서도 Core/UI 전체를 로드하지 않습니다.
        return new GameObject(nameof(GameSession)).AddComponent<GameSession>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
        InstanceChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        InstanceChanged?.Invoke();
    }

    public void SaveUpgradeToGeneral(UpgradeToGeneralData data)
    {
        generalData = (data ?? new UpgradeToGeneralData()).Copy();
        hasGeneralData = true;
        GameDataRevision++;
        ClearBattleTransfers();
        GameDataChanged?.Invoke();
    }

    public bool TryReadGeneralData(out UpgradeToGeneralData data)
    {
        data = hasGeneralData ? generalData?.Copy() : null;
        return data != null;
    }

    public void LoadSkillItems(BuffSkillManager target)
    {
        if (target == null)
            return;

        target.ClearRegisteredBuffItems();
        if (!HasSkillItems)
            return;

        foreach (SkillItemSnapshot item in generalData.skillItems)
        {
            if (item != null && item.itemData != null)
                target.RegisterBuffItem(item.itemData, item.bagId);
        }
    }

    public void SendItemsToBattle(GameItemToBattleData data)
    {
        GameItemToBattleData copy = data?.Copy();
        if (copy == null || copy.items.Count == 0)
            return;

        pendingBattleItems.Add(copy);
        lastSentToBattle = copy.Copy();
        BattleItemsAvailable?.Invoke();
    }

    public bool TryTakeBattleItems(out GameItemToBattleData data)
    {
        if (pendingBattleItems.Count == 0)
        {
            data = null;
            return false;
        }

        data = pendingBattleItems[0];
        pendingBattleItems.RemoveAt(0);
        lastReceivedByBattle = data.Copy();
        return true;
    }

    public void ClearAll()
    {
        generalData = new UpgradeToGeneralData();
        hasGeneralData = false;
        GameDataRevision++;
        ClearBattleTransfers();
        GameDataChanged?.Invoke();
    }

    private void ClearBattleTransfers()
    {
        pendingBattleItems.Clear();
        lastSentToBattle = null;
        lastReceivedByBattle = null;
    }
}
