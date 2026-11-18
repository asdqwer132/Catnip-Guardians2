using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 투입 주기 · 재료 선택 · 재고 예약/환불만 담당합니다. 설정과 풀은 매니저가 주입합니다.
internal sealed class MergeItemDropper
{
    private readonly MergegameManager owner;
    private readonly List<Toggle> observedToggles = new List<Toggle>();
    private readonly Dictionary<MergeItemMaterial, long> previewStock = new Dictionary<MergeItemMaterial, long>();
    private bool sharedItemStock, dropping;
    private float nextDropAt, nextAutomaticDropAt;
    private float previousInterval = -1f;
    private int stockRevision;
    private bool hadSelectedMaterial;
    public MergeItemDropper(MergegameManager owner) => this.owner = owner;
    public void ResetCooldown() { nextDropAt = 0f; RestartAutomaticTimer(); }
    private void RestartAutomaticTimer()
    {
        previousInterval = AutomaticInterval;
        nextAutomaticDropAt = Time.time + previousInterval;
    }
    public float Interval => AutomaticInterval;
    public float Countdown
    {
        get
        {
            if (!RefreshSelectionState() || !owner.IsFeedingEnabled) return AutomaticInterval;
            return Mathf.Max(0f, (owner.UsesAutomaticFeeding ? Mathf.Max(nextDropAt, nextAutomaticDropAt) : nextDropAt) - Time.time);
        }
    }
    public bool IsTimerRunning => owner.UsesAutomaticFeeding && owner.IsFeedingEnabled && RefreshSelectionState();
    public ItemData NextItem { get { RefreshSelectionState(); return owner.currentMaterial?.mergeItem?.item; } }

    // Forecast with virtual stock only: never advance the real selection or consume inventory.
    public void GetUpcomingItems(List<ItemData> result, int maximum)
    {
        result.Clear();
        RefreshSelectionState();
        if (maximum <= 0 || owner.mergeMaterials == null) return;
        previewStock.Clear();
        foreach (var material in owner.mergeMaterials)
            if (CanFeedMaterial(material))
                previewStock[material] = Math.Max(0, material.count);
        var current = owner.currentMaterial;
        bool cycle = owner.UsesAutomaticFeeding && owner.cycleAutomaticMaterials;
        for (int i = 0; i < maximum && current != null; i++)
        {
            if (!previewStock.TryGetValue(current, out long remaining) || remaining <= 0) break;
            result.Add(current.mergeItem.item);
            previewStock[current] = remaining - 1;
            if (sharedItemStock)
                foreach (var slot in owner.mergeMaterials)
                    if (slot != null && slot != current && slot.mergeItem != null && slot.mergeItem.item == current.mergeItem.item && previewStock.ContainsKey(slot))
                        previewStock[slot] = remaining - 1;
            if (!cycle && remaining > 1) continue;
            int start = owner.mergeMaterials.IndexOf(current);
            current = null;
            for (int offset = 1; offset <= owner.mergeMaterials.Count; offset++)
            {
                var candidate = owner.mergeMaterials[(start + offset) % owner.mergeMaterials.Count];
                if (candidate != null && previewStock.TryGetValue(candidate, out long stock) && stock > 0)
                { current = candidate; break; }
            }
        }
    }

    public void SetAllMaterialsSelected(bool selected)
    {
        RefreshMaterialSelection();
        if (owner.mergeMaterials == null) return;
        foreach (var material in owner.mergeMaterials)
        {
            var toggle = material?.mergeUI != null ? material.mergeUI.toggle : null;
            if (toggle != null) toggle.isOn = selected && MergeTree.IsAvailable(material);
        }
        RefreshSelectionState();
    }

    public void Begin()
    {
        hadSelectedMaterial = false;
        RestartAutomaticTimer();
        RefreshMaterialSelection();
    }

    public void End()
    {
        foreach (var toggle in observedToggles)
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnMaterialSelectionChanged);
        observedToggles.Clear();
    }

    // 주입/세션 갱신으로 슬롯 연결이 바뀌어도 현재 토글들만 관찰합니다.
    public void RefreshMaterialSelection()
    {
        End();
        if (owner.isActiveAndEnabled && owner.mergeMaterials != null)
            foreach (var material in owner.mergeMaterials)
            {
                var toggle = material?.mergeUI != null ? material.mergeUI.toggle : null;
                if (toggle == null || observedToggles.Contains(toggle)) continue;
                observedToggles.Add(toggle);
                toggle.onValueChanged.AddListener(OnMaterialSelectionChanged);
            }
        RefreshSelectionState();
    }

    private void OnMaterialSelectionChanged(bool _) => RefreshSelectionState();

    private bool RefreshSelectionState()
    {
        float interval = AutomaticInterval;
        if (previousInterval >= 0f && !Mathf.Approximately(previousInterval, interval))
        {
            float change = interval - previousInterval;
            if (nextDropAt > Time.time) nextDropAt += change;
            if (nextAutomaticDropAt > Time.time) nextAutomaticDropAt += change;
        }
        previousInterval = interval;
        EnsureSelection();
        bool selected = false;
        if (owner.mergeMaterials != null)
            foreach (var material in owner.mergeMaterials)
                if (CanFeedMaterial(material))
                { selected = true; break; }
        // 0개일 때는 초기값으로 정지, 0→1개일 때만 다시 시작합니다. 추가 선택은 시각을 바꾸지 않습니다.
        if (!selected || !hadSelectedMaterial) RestartAutomaticTimer();
        hadSelectedMaterial = selected;
        return selected;
    }

    private float AutomaticInterval => Mathf.Max(EffectStatUtility.Safe(owner.minimumDropInterval, 0f, 600f, .01f), EffectStatUtility.Safe(owner.dropInterval, 0f, 600f, .5f) / Mathf.Max(.01f, owner.FeedingSpeedMultiplier));

    public void Tick()
    {
        if (!RefreshSelectionState()) return;
        if (!owner.UsesAutomaticFeeding || !owner.IsFeedingEnabled || !owner.CanOperate ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay)))
        {
            RestartAutomaticTimer();
            return;
        }
        if (Time.time < nextAutomaticDropAt) return;
        // 준비된 투입은 막혀 있어도 유지합니다. 성공했을 때만 다음 주기를 시작합니다.
        EnsureSelection();
        var usedMaterial = owner.currentMaterial;
        if (TryDropCurrent() && owner.cycleAutomaticMaterials)
            owner.currentMaterial = NextFeedingMaterial(usedMaterial);
    }

    public void SetFeeding(bool value)
    {
        owner.isFeeding = value;
        RestartAutomaticTimer();
    }

    public void SetAutomaticDrop(bool value)
    {
        owner.automaticDrop = value;
        RestartAutomaticTimer();
    }

    private static bool CanFeedMaterial(MergeItemMaterial material)
        => MergeTree.IsAvailable(material) && material.mergeUI != null &&
            material.mergeUI.toggle != null && material.mergeUI.toggle.isOn;

    private MergeItemMaterial NextFeedingMaterial(MergeItemMaterial current)
    {
        if (owner.mergeMaterials == null || owner.mergeMaterials.Count == 0) return null;
        int start = owner.mergeMaterials.IndexOf(current);
        for (int offset = 1; offset <= owner.mergeMaterials.Count; offset++)
        {
            var candidate = owner.mergeMaterials[(start + offset) % owner.mergeMaterials.Count];
            if (CanFeedMaterial(candidate)) return candidate;
        }
        return null;
    }

    private void EnsureSelection()
    {
        if (owner.mergeMaterials != null && owner.currentMaterial != null)
        {
            if (owner.mergeMaterials.Contains(owner.currentMaterial) && CanFeedMaterial(owner.currentMaterial)) return;
            foreach (var material in owner.mergeMaterials)
                if (CanFeedMaterial(material) && material.mergeItem == owner.currentMaterial.mergeItem) { owner.currentMaterial = material; return; }
        }
        owner.currentMaterial = NextFeedingMaterial(owner.currentMaterial);
    }

    // 가방에서 복사한 라운드 재고입니다. 같은 데이터의 중복 슬롯도 한 수량을 공유합니다.
    public void SetMaterialStock(IReadOnlyList<InventoryItem> items)
    {
        stockRevision++;
        sharedItemStock = true;
        if (owner.mergeMaterials != null)
            foreach (var material in owner.mergeMaterials)
            {
                if (material?.mergeItem == null) continue;
                long count = 0;
                if (items != null && material.mergeItem.item != null && material.mergeItem.item.series == owner.itemSeries)
                    foreach (var item in items)
                        if (item?.itemData == material.mergeItem.item) count += Math.Max(0, item.amount);
                material.count = (int)Math.Min(int.MaxValue, count);
            }
        ResetBoard();
    }

    // 디버그는 매니저에 작성한 슬롯별 수량을 그대로 사용합니다.
    public void SetConfiguredMaterialCounts(IReadOnlyList<int> counts)
    {
        stockRevision++;
        sharedItemStock = false;
        if (owner.mergeMaterials != null)
            for (int i = 0; i < owner.mergeMaterials.Count; i++)
                if (owner.mergeMaterials[i] != null)
                    owner.mergeMaterials[i].count = counts != null && i < counts.Count ? Math.Max(0, counts[i]) : 0;
        ResetBoard();
    }

    private void ResetBoard()
    {
        owner.ClearItems();
        EnsureSelection();
        owner.RefreshUI();
    }

    private void SetRemaining(MergeItemMaterial material, int count)
    {
        material.count = Math.Max(0, count);
        if (!sharedItemStock || owner.mergeMaterials == null) return;
        foreach (var slot in owner.mergeMaterials)
            if (slot?.mergeItem != null && slot.mergeItem.item == material.mergeItem.item) slot.count = material.count;
    }

    public bool TryDropCurrent()
    {
        if (!owner.IsFeedingEnabled) return owner.Reject("투입이 중지되어 있습니다.");
        if (!owner.CanOperate || dropping) return owner.Reject("현재 생성할 수 없는 상태입니다.");
        if (owner.objectPool == null || owner.dropZone == null || owner.dropZone.spawnPoint == null) return owner.Reject("Object Pool / Spawn Point 연결을 확인하세요.");
        RefreshSelectionState();
        if (Time.time < nextDropAt) return owner.Reject("다음 생성까지 대기 중입니다.");
        if (owner.ActiveItemCount >= Mathf.Max(1, owner.maximumActiveItems)) return owner.Reject("최대 활성 아이템 개수에 도달했습니다.");
        EnsureSelection();
        if (!CanFeedMaterial(owner.currentMaterial)) return owner.Reject("토글이 켜진 사용 가능한 재료가 없습니다.");
        var material = owner.currentMaterial;
        if (!MergegameManager.TryGetPrefab(material.mergeItem, out _))
            return owner.Reject("선택 재료의 MergeItem → Prefab에 활성 MergeItemObject가 있는 프리팹을 연결하세요.");
        Vector3 position = owner.dropZone.spawnPoint.position;
        dropping = true;
        int before = owner.Revision;
        bool committed = false;
        bool reserved = false;
        int reservedStockRevision = stockRevision;
        MergeItemObject piece = null;
        try
        {
            // 지정된 투입 위치에 즉시 생성하고 라운드 재고만 소비합니다.
            piece = owner.SpawnPiece(material.mergeItem, position);
            if (piece == null || !piece.IsLive || before != owner.Revision || !owner.CanOperate) return false;
            SetRemaining(material, material.count - 1);
            reserved = true;
            if (before != owner.Revision || !owner.CanOperate) return false;
            piece.SetSimulated(true);
            nextDropAt = Time.time + AutomaticInterval;
            RestartAutomaticTimer();
            committed = true;
            owner.RefreshUI();
            EnsureSelection();
            owner.Report($"낙하: {owner.GetItemDisplayName(material.mergeItem.item)}, 해당 재료 잔량: {material.count}");
            return true;
        }
        finally
        {
            if (!committed)
            {
                if (piece != null) piece.ReturnToPool();
                if (reserved)
                {
                    if (reservedStockRevision == stockRevision) SetRemaining(material, material.count + 1);
                    owner.RefreshUI();
                }
            }
            dropping = false;
            if (committed) TutorialSignals.Raise(owner.gameObject.scene, TutorialSignals.ItemDropped);
        }
    }

}
