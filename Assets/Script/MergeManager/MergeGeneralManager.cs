using System;
using System.Collections.Generic;
using UnityEngine;

// 아이템 씬의 라운드/상자 선택/재료 주입을 소유합니다. 가방이나 세션 재고를 소비하지 않습니다.
[DefaultExecutionOrder(-80), DisallowMultipleComponent]
public sealed class MergeGeneralManager : MonoBehaviour
{
    [Header("아이템 씬 연결")]
    public MergeBoxSceneController boxes;
    public MergeGimicManager gimicManager;
    [Header("시작 설정")]
    [Tooltip("상자 목록의 순서입니다. 1부터 시작합니다.")]
    [Min(1)] public int startingSeriesIndex = 1;
    [Min(1)] public int startingLevel = 1;
    public bool useSessionLevels = true;
    [Header("재료 주입")]
    [Tooltip("켜면 가방 수량을 무시하고 각 머지 매니저에 작성된 Count로 라운드를 시작합니다.")]
    public bool debugMode;

    public int SelectedIndex { get; private set; } = -1;
    public int SelectedLevel => boxes != null && SelectedIndex >= 0 ? boxes.SelectedLevel : startingLevel;
    public int StartingSeriesIndex => Math.Max(1, startingSeriesIndex) - 1;
    private readonly Dictionary<MergegameManager, List<MergeItemMaterial>> configuredMaterials = new Dictionary<MergegameManager, List<MergeItemMaterial>>();
    private GameSession session, appliedSession;
    private int appliedRevision = -1, appliedRound = -1;
    private UpgradeToGeneralData roundData;
    private bool initialized, appliedDebugMode;
    private bool UsesLoadout => !debugMode && roundData?.equipment != null;
    private readonly List<int> equippedProfileIndices = new List<int>(2);
    private readonly List<MergeBoxGimic> equippedModules = new List<MergeBoxGimic>(2);

    private void Awake()
    {
        if (boxes != null) boxes.gameManager = this;
        CaptureConfiguredMaterials();
    }
    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
        if (initialized && SelectedIndex >= 0 && gimicManager != null && !(gimicManager.CurrentGimic?.Running ?? false))
            ActivateBox(SelectedIndex, SelectedLevel, true);
    }
    private void Start()
    {
        GameSession.GetOrCreate();
        BindSession();
        if (!initialized) InitializeRound(null);
    }
    private void Update()
    {
        if (initialized && appliedDebugMode != debugMode) RestartRound();
    }
    private void BindSession()
    {
        UnbindSession();
        session = GameSession.Instance;
        if (session != null)
        {
            session.GameDataChanged += ApplySessionRound;
            session.RoundStarted += ApplySessionRound;
        }
        ApplySessionRound();
    }
    private void ApplySessionRound()
    {
        if (session == null) return;
        if (initialized && appliedSession == session && appliedRevision == session.GameDataRevision && appliedRound == session.RoundRevision && appliedDebugMode == debugMode) return;
        UpgradeToGeneralData data = null;
        if (!debugMode) session.TryReadItemRound(out data);
        appliedSession = session;
        appliedRevision = session.GameDataRevision;
        appliedRound = session.RoundRevision;
        InitializeRound(data);
    }
    public void InitializeRound(UpgradeToGeneralData data)
    {
        roundData = data?.Copy();
        ResolveEquippedProfiles();
        ApplyRoundStock();
        int index = UsesLoadout ? (equippedProfileIndices.Count > 0 ? equippedProfileIndices[0] : StartingSeriesIndex) :
            SelectedIndex >= 0 ? SelectedIndex : StartingSeriesIndex;
        if (boxes != null && boxes.seriesDropdown != null) boxes.seriesDropdown.interactable = !UsesLoadout;
        ActivateBox(index, ResolveLevel(index), true);
    }
    [ContextMenu("Restart Item Round")]
    public void RestartRound()
    {
        var data = roundData;
        if (session != null && !debugMode) session.TryReadItemRound(out data);
        InitializeRound(data);
    }
    public void SetDebugMode(bool value)
    {
        if (debugMode == value) return;
        debugMode = value;
        RestartRound();
    }
    private void CaptureConfiguredMaterials()
    {
        if (boxes == null) return;
        foreach (var entry in boxes.profiles)
        {
            var board = entry != null ? entry.board : null;
            if (board == null || configuredMaterials.ContainsKey(board)) continue;
            var defaults = new List<MergeItemMaterial>();
            foreach (var material in board.mergeMaterials)
                if (material?.mergeItem != null)
                    defaults.Add(new MergeItemMaterial(material.mergeItem, null, material.count));
            configuredMaterials.Add(board, defaults);
        }
    }

    private void ResolveEquippedProfiles()
    {
        equippedProfileIndices.Clear();
        equippedModules.Clear();
        if (!UsesLoadout) return;
        var loadout = roundData.equipment;
        foreach (int number in loadout.equippedChests)
        {
            int index = number - 1;
            if (number < 1 || number > 8 || (number != 1 && !roundData.unlockedMergeBoxes.Contains(number)) ||
                equippedProfileIndices.Contains(index) || boxes == null || boxes.GetProfile(index) == null)
                continue;
            equippedProfileIndices.Add(index);
            if (equippedProfileIndices.Count == 2) break;
        }
        foreach (int index in equippedProfileIndices)
            equippedModules.Add(boxes?.GetProfile(index)?.gimmick);
    }

    private void ApplyRoundStock()
    {
        CaptureConfiguredMaterials();
        int limit = boxes != null && boxes.card != null ? Mathf.Max(1, boxes.card.maximumMaterials) : int.MaxValue;
        foreach (var entry in configuredMaterials)
        {
            var board = entry.Key;
            if (board == null) continue;
            board.SetRoundRecipeSources(UsesLoadout ? boxes.profiles : null);
            var materials = new List<MergeItemMaterial>();
            if (debugMode)
            {
                foreach (var material in entry.Value)
                {
                    if (materials.Count >= limit) break;
                    materials.Add(new MergeItemMaterial(material.mergeItem, null, material.count));
                }
                board.mergeMaterials = materials;
                var counts = new int[materials.Count];
                for (int i = 0; i < counts.Length; i++) counts[i] = materials[i].count;
                board.SetConfiguredMaterialCounts(counts);
                continue;
            }

            var added = new HashSet<ItemData>();
            var stockItems = roundData?.materialItems;
            if (stockItems != null)
            {
                foreach (var stock in stockItems)
                {
                    if (materials.Count >= limit) break;
                    if (UsesLoadout || stock?.itemData?.series == board.itemSeries) AddMaterial(stock);
                }
            }

            void AddMaterial(InventoryItem stock)
            {
                var item = stock?.itemData;
                if (item == null || stock.amount <= 0 || !added.Add(item)) return;
                var definition = board.GetDefinitionAtMergeItem(item)
                    ?? entry.Value.Find(material => material.mergeItem.item == item)?.mergeItem;
                if (definition == null && UsesLoadout)
                    foreach (var defaults in configuredMaterials.Values)
                    {
                        definition = defaults.Find(material => material.mergeItem.item == item)?.mergeItem;
                        if (definition != null) break;
                    }
                if (MergegameManager.TryGetPrefab(definition, out _))
                    materials.Add(new MergeItemMaterial(definition, null, 0));
            }
            board.mergeMaterials = materials;
            board.SetMaterialStock(roundData?.materialItems);
        }
        initialized = true;
        appliedDebugMode = debugMode;
    }

    public bool StartRound(int seriesIndex, int level)
    {
        if (UsesLoadout && equippedProfileIndices.Count > 0) seriesIndex = equippedProfileIndices[0];
        if (boxes == null || !boxes.CanShowBox(seriesIndex)) return false;
        ApplyRoundStock();
        return ActivateBox(seriesIndex, level, true);
    }

    // Public round input uses the displayed, one-based box number.
    public bool StartBoxRound(int boxNumber, int level)
        => boxNumber > 0 && StartRound(boxNumber - 1, level);

    private int ResolveLevel(int index)
    {
        int level = Math.Max(1, startingLevel);
        var board = boxes != null ? boxes.GetProfile(index)?.board : null;
        if (board == null || debugMode || !useSessionLevels || roundData?.boxLevels == null) return level;
        level = 1;
        foreach (var snapshot in roundData.boxLevels)
            if (snapshot != null && snapshot.series == board.itemSeries) level = Math.Max(level, snapshot.level);
        return level;
    }

    private bool ActivateBox(int index, int level, bool restart = false)
    {
        if (boxes == null || !boxes.CanShowBox(index)) return false;
        if (!restart && SelectedIndex == index)
        {
            if (SelectedLevel != Math.Max(1, level)) boxes.SetDeadlineLevel(level);
            return true;
        }

        gimicManager?.StopGimic();
        boxes.ShowBox(index, level, restart);
        SelectedIndex = index;
        if (!debugMode) gimicManager?.InitializeModules(boxes.SelectedBoard, equippedModules);
        else gimicManager?.InitializeModule(boxes.SelectedBoard, boxes.SelectedProfile.gimmick);
        return true;
    }

    public void SetBox(int index)
    {
        if (!UsesLoadout) ActivateBox(index, ResolveLevel(index));
    }
    public void SetCurrentBoxLevel(int level) { if (SelectedIndex >= 0) ActivateBox(SelectedIndex, level); }

    public bool SetBoxLevel(ItemSeries series, int level)
    {
        if (boxes == null) return false;
        if (UsesLoadout && boxes.SelectedBoard?.itemSeries != series) return false;
        for (int i = 0; i < boxes.profiles.Length; i++)
            if (boxes.GetProfile(i)?.board != null && boxes.GetProfile(i).board.itemSeries == series)
                return ActivateBox(i, level);
        return false;
    }

    public bool UpgradeCurrentBox()
    {
        int next = boxes != null ? boxes.NextLevel(SelectedLevel) : int.MaxValue;
        return next != int.MaxValue && ActivateBox(SelectedIndex, next);
    }
    private void UnbindSession()
    {
        if (session == null) return;
        session.GameDataChanged -= ApplySessionRound;
        session.RoundStarted -= ApplySessionRound;
    }
    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        UnbindSession();
        session = null;
        gimicManager?.StopGimic();
    }
}
