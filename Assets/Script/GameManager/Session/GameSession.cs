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
    public event Action RoundStarted;

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
    public int RoundRevision { get; private set; }
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
        ReloadUpgradeProgress();
        InstanceChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        InstanceChanged?.Invoke();
    }

    public void CaptureUpgradeProgress(MergeBoxUpgradeManager mergeBoxUpgrades, PotionEquipmentManager potionEquipment,
        InventoryManager materialInventory, BuffSkillManager buffSkillManager)
    {
        var data = new UpgradeToGeneralData();
        if (mergeBoxUpgrades != null)
        {
            data.boxLevels = mergeBoxUpgrades.CaptureLevels();
            data.mainPlantIndex = mergeBoxUpgrades.MainPlantIndex;
            data.unlockedSkillNodes = mergeBoxUpgrades.CaptureUnlockedNodes();
        }
        if (potionEquipment != null)
        {
            data.potionSlots = potionEquipment.CaptureSlots();
            EquipmentSave.StorePotions(data.potionSlots);
        }
        else data.potionSlots = EquipmentSave.RestorePotions();
        data.equipment = EquipmentSave.Current.Copy();
        var inventory = materialInventory;
        if (inventory != null)
        {
            data.materialItems = GameSessionItemCopy.Copy(inventory.items);
            // Crafting slots are temporary reservations. Recover their quantities in the save only.
            if (inventory == InventoryManager.instance && PotionCraftManager.instance != null)
                foreach (var held in PotionCraftManager.instance.GetMaterials())
                {
                    if (held?.itemData == null || held.amount <= 0) continue;
                    var item = data.materialItems.Find(entry => entry.itemData == held.itemData);
                    if (item == null) data.materialItems.Add(new InventoryItem(held.itemData, held.amount));
                    else item.amount = (int)Math.Min(int.MaxValue, (long)item.amount + held.amount);
                }
        }

        if (buffSkillManager != null)
        {
            foreach (RegisteredBuffSkillItem item in buffSkillManager.RegisteredBuffItems)
            {
                if (item != null && item.itemData != null)
                    data.skillItems.Add(new SkillItemSnapshot(item));
            }
        }
        data.unlockedMergeBoxes = MergeBoxUpgradeManager.CaptureChestUnlocks(data.unlockedSkillNodes);
        SaveUpgradeToGeneral(data);
    }

    public void ReloadUpgradeProgress()
    {
        var data = UpgradeProgressSave.Read();
        if (data == null)
        {
            if (!EquipmentSave.Exists) return;
            data = new UpgradeToGeneralData { legacySkillsOnly = true };
        }
        var savedPotions = EquipmentSave.RestorePotions();
        if (savedPotions.Count > 0) data.potionSlots = savedPotions;
        else EquipmentSave.StorePotions(data.potionSlots);
        UpgradeProgressSave.RefreshSkillValues(data);
        data.equipment = EquipmentSave.Current.Copy();
        data.unlockedMergeBoxes = MergeBoxUpgradeManager.CaptureChestUnlocks(data.unlockedSkillNodes);
        SaveUpgradeToGeneral(data);
    }

    public void SaveUpgradeToGeneral(UpgradeToGeneralData data)
    {
        generalData = (data ?? new UpgradeToGeneralData()).Copy();
        var progress = generalData.Copy();
        progress.equipment = null;
        if (EquipmentSave.Current.potions.Count > 0) progress.potionSlots.Clear();
        UpgradeProgressSave.Write(progress);
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

    public void UpdateEquipment(EquipmentData equipment)
    {
        if (!hasGeneralData || generalData == null) return;
        generalData.equipment = equipment?.Copy();
        GameDataRevision++;
        GameDataChanged?.Invoke();
    }

    public void BeginRound()
    {
        RoundRevision++;
        RoundStarted?.Invoke();
    }

    // 업그레이드 인벤토리의 저장 스냅샷입니다. 라운드의 소비 수량은 여기에 쓰지 않습니다.
    public bool TryRestoreUpgradeInventory(InventoryManager inventory)
    {
        if (inventory == null || !TryReadGeneralData(out var data) || data.legacySkillsOnly) return false;
        inventory.items = GameSessionItemCopy.Copy(data.materialItems);
        inventory.onInventoryChanged?.Invoke();
        return true;
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
                target.RegisterBuffItem(item.itemData);
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
