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
    public int SelectedLevel => boxes != null && boxes.SelectedBoxManager != null ? boxes.SelectedBoxManager.CurrentLevel : startingLevel;
    public int StartingSeriesIndex => Math.Max(1, startingSeriesIndex) - 1;
    private readonly Dictionary<MergegameManager, int[]> configuredCounts = new Dictionary<MergegameManager, int[]>();
    private GameSession session, appliedSession;
    private int appliedRevision = -1, appliedRound = -1;
    private UpgradeToGeneralData roundData;
    private bool initialized, appliedDebugMode;

    private void Awake()
    {
        if (boxes != null) boxes.gameManager = this;
        CaptureConfiguredCounts();
    }
    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
        if (initialized && gimicManager != null && gimicManager.CurrentGimic != null && !gimicManager.CurrentGimic.Running)
            ActivateBox(SelectedIndex, SelectedLevel);
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
        if (!debugMode) session.TryReadGeneralData(out data);
        appliedSession = session;
        appliedRevision = session.GameDataRevision;
        appliedRound = session.RoundRevision;
        InitializeRound(data);
    }
    public void InitializeRound(UpgradeToGeneralData data)
    {
        roundData = data?.Copy();
        ApplyRoundStock();
        int index = SelectedIndex >= 0 ? SelectedIndex : StartingSeriesIndex;
        ActivateBox(index, ResolveLevel(index));
    }
    [ContextMenu("Restart Item Round")]
    public void RestartRound()
    {
        var data = roundData;
        if (session != null && !debugMode) session.TryReadGeneralData(out data);
        InitializeRound(data);
    }
    public void SetDebugMode(bool value)
    {
        if (debugMode == value) return;
        debugMode = value;
        RestartRound();
    }
    private void CaptureConfiguredCounts()
    {
        if (boxes == null || boxes.boxes == null) return;
        foreach (var entry in boxes.boxes)
        {
            var board = entry != null ? entry.board : null;
            if (board == null || configuredCounts.ContainsKey(board)) continue;
            var counts = new int[board.mergeMaterials.Count];
            for (int i = 0; i < counts.Length; i++) counts[i] = Math.Max(0, board.mergeMaterials[i]?.count ?? 0);
            configuredCounts.Add(board, counts);
        }
    }
    private void ApplyRoundStock()
    {
        CaptureConfiguredCounts();
        foreach (var entry in configuredCounts)
        {
            if (entry.Key == null) continue;
            if (debugMode) entry.Key.SetConfiguredMaterialCounts(entry.Value);
            else entry.Key.SetMaterialStock(roundData?.materialItems);
        }
        initialized = true;
        appliedDebugMode = debugMode;
    }
    public bool StartRound(int seriesIndex, int level)
    {
        ApplyRoundStock();
        return ActivateBox(seriesIndex, level);
    }
    private int ResolveLevel(int index)
    {
        int level = Math.Max(1, startingLevel);
        if (debugMode || !useSessionLevels || roundData?.boxLevels == null || boxes == null || index < 0 || index >= boxes.boxes.Length) return level;
        var board = boxes.boxes[index]?.board;
        if (board == null) return level;
        level = 1;
        foreach (var snapshot in roundData.boxLevels)
            if (snapshot != null && snapshot.series == board.itemSeries) level = Math.Max(level, snapshot.level);
        return level;
    }
    private bool ActivateBox(int index, int level)
    {
        if (boxes == null || boxes.boxes == null || index < 0 || index >= boxes.boxes.Length) return false;
        var entry = boxes.boxes[index];
        if (entry == null || entry.board == null || entry.boxRoot == null || entry.levelManager == null) return false;
        if (entry.levelManager.Board != entry.board) entry.levelManager.BindBoard(entry.board);
        if (!entry.levelManager.TrySetLevel(level)) return false;
        gimicManager?.StopGimic();
        SelectedIndex = index;
        foreach (var other in boxes.boxes)
        {
            if (other == null) continue;
            bool selected = other == entry;
            if (other.board != null) other.board.enabled = selected;
            if (other.boxRoot != null) other.boxRoot.gameObject.SetActive(selected);
        }
        boxes.ShowBox(index);
        gimicManager?.InitializeGimic(entry.board, entry.levelManager.CurrentBox);
        return true;
    }
    public void SetBox(int index) => ActivateBox(index, ResolveLevel(index));
    public void SetCurrentBoxLevel(int level) { if (SelectedIndex >= 0) ActivateBox(SelectedIndex, level); }
    public bool SetBoxLevel(ItemSeries series, int level)
    {
        if (boxes == null) return false;
        for (int i = 0; i < boxes.boxes.Length; i++)
            if (boxes.boxes[i]?.board != null && boxes.boxes[i].board.itemSeries == series) return ActivateBox(i, level);
        return false;
    }
    public bool UpgradeCurrentBox()
    {
        var manager = boxes != null ? boxes.SelectedBoxManager : null;
        if (manager == null) return false;
        int next = int.MaxValue;
        foreach (var entry in manager.levels)
            if (entry?.box != null && entry.minimumLevel > manager.CurrentLevel) next = Math.Min(next, entry.minimumLevel);
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
