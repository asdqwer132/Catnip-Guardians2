using System;
using System.Collections.Generic;
using UnityEngine;

// 투입 주기 · 재료 선택 · 재고 예약/환불만 담당합니다. 설정과 풀은 매니저가 주입합니다.
internal sealed class MergeItemDropper
{
    private readonly MergegameManager owner;
    private readonly Collider2D[] overlap = new Collider2D[32];
    private readonly Dictionary<MergeItemMaterial, int> roundBonuses = new Dictionary<MergeItemMaterial, int>();
    private GameSession materialSession;
    private bool usesSessionMaterials, dropping;
    private float nextDropAt, nextAutomaticDropAt;
    private int bonusRevision;
    public MergeItemDropper(MergegameManager owner) => this.owner = owner;
    public void ResetCooldown() { nextDropAt = 0f; nextAutomaticDropAt = Time.time + AutomaticInterval; }
    public float Interval => AutomaticInterval;
    public float Countdown => !owner.isFeeding ? AutomaticInterval :
        Mathf.Max(0f, (owner.automaticDrop ? Mathf.Max(nextDropAt, nextAutomaticDropAt) : nextDropAt) - Time.time);
    public ItemData NextItem { get { EnsureSelection(); return owner.currentMaterial?.mergeItem?.item; } }

    public void Begin()
    {
        nextAutomaticDropAt = Time.time + AutomaticInterval;
        EnsureSelection();
    }

    private float AutomaticInterval => Mathf.Max(.01f, EffectStatUtility.Safe(owner.dropInterval, 0f, 600f, .5f));

    public void Tick()
    {
        if (!owner.automaticDrop || !owner.isFeeding || !owner.CanOperate ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay)))
        {
            nextAutomaticDropAt = Time.time + AutomaticInterval;
            return;
        }
        if (Time.time < nextAutomaticDropAt) return;
        // 막힌 투입구도 매 프레임 재시도하지 않고 다음 주기까지 기다립니다.
        nextAutomaticDropAt = Time.time + AutomaticInterval;
        EnsureSelection();
        var usedMaterial = owner.currentMaterial;
        if (TryDropCurrent() && owner.cycleAutomaticMaterials)
            owner.currentMaterial = NextFeedingMaterial(usedMaterial);
    }

    public void SetFeeding(bool value)
    {
        owner.isFeeding = value;
        nextAutomaticDropAt = Time.time + AutomaticInterval;
    }

    public void SetAutomaticDrop(bool value)
    {
        owner.automaticDrop = value;
        nextAutomaticDropAt = Time.time + AutomaticInterval;
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

    public void SetSessionMaterials(IReadOnlyList<InventoryItem> items, GameSession source = null)
    {
        materialSession = source;
        usesSessionMaterials = source != null;
        ResetRoundMaterials();
        RefreshSessionMaterials(items);
    }

    public void ResetRoundMaterials()
    {
        bonusRevision++;
        owner.ClearItems();
        if (owner.mergeMaterials == null) return;
        foreach (var material in owner.mergeMaterials)
        {
            if (material == null) continue;
            // 기본 수량의 설정값을 따로 기억해 재시도 때만 보너스를 다시 지급합니다.
            if (!roundBonuses.TryGetValue(material, out int bonus))
                roundBonuses.Add(material, bonus = Math.Max(0, material.defaultCount));
            material.defaultCount = bonus;
        }
        EnsureSelection();
        owner.RefreshUI();
    }

    public void RefreshSessionMaterials(IReadOnlyList<InventoryItem> items)
    {
        if (owner.mergeMaterials == null) return;
        foreach (var material in owner.mergeMaterials)
        {
            if (material?.mergeItem == null) continue;
            long count = 0;
            if (items != null && material.mergeItem.item != null && material.mergeItem.item.series == owner.itemSeries)
                foreach (var item in items)
                    if (item?.itemData == material.mergeItem.item) count += Math.Max(0, item.amount);
            material.count = (int)Math.Min(int.MaxValue, count);
        }
        EnsureSelection();
        owner.RefreshUI();
    }

    public bool TryDropCurrent()
    {
        if (!owner.isFeeding) return owner.Reject("투입이 중지되어 있습니다.");
        if (!owner.CanOperate || dropping) return owner.Reject("현재 생성할 수 없는 상태입니다.");
        if (owner.objectPool == null || owner.spawnPoint == null) return owner.Reject("Object Pool / Spawn Point 연결을 확인하세요.");
        if (Time.time < nextDropAt) return owner.Reject("다음 생성까지 대기 중입니다.");
        if (owner.ActiveItemCount >= Mathf.Max(1, owner.maximumActiveItems)) return owner.Reject("최대 활성 아이템 개수에 도달했습니다.");
        if (owner.dropZone != null && owner.dropZone.IsBlocked(owner)) return owner.Reject("아이템이 투입 제한선을 넘었습니다.");
        EnsureSelection();
        if (!CanFeedMaterial(owner.currentMaterial)) return owner.Reject("토글이 켜진 사용 가능한 재료가 없습니다.");
        var material = owner.currentMaterial;
        if (!MergegameManager.TryGetPrefab(material.mergeItem, out _))
            return owner.Reject("선택 재료의 MergeItem → Prefab에 활성 MergeItemObject가 있는 프리팹을 연결하세요.");
        float offset = UnityEngine.Random.Range(-owner.dropOffset, owner.dropOffset);
        Vector3 position = owner.spawnPoint.position + new Vector3(offset * owner.dropOffset, 0f, 0f);
        if (IsBlocked(position, material.mergeItem.Radius)) return owner.Reject("투입 위치가 막혀 있습니다.");
        dropping = true;
        int before = owner.Revision;
        bool committed = false;
        bool reserved = false;
        bool usedBonus = material.defaultCount > 0;
        bool useSessionStock = usesSessionMaterials;
        int reservedBonusRevision = bonusRevision;
        var source = materialSession;
        int sourceRevision = source != null ? source.GameDataRevision : -1;
        MergeItemObject piece = null;
        try
        {
            // 풀 콜백 전에 예약해 마지막 재료가 중복 투입되는 것을 막습니다.
            if (usedBonus) material.defaultCount--;
            else if (useSessionStock)
            {
                if (source == null || !source.TryConsumeMaterial(material.mergeItem.item))
                    return owner.Reject("남은 본재고가 없습니다.");
                material.count = source.GetMaterialCount(material.mergeItem.item);
            }
            else material.count--;
            reserved = true;
            if (before != owner.Revision || !owner.CanOperate) return false;
            piece = owner.SpawnPiece(material.mergeItem, position);
            if (piece == null || !piece.IsLive || before != owner.Revision || !owner.CanOperate)
                return owner.Reject("풀 생성이 취소되었거나 아이템이 비활성 상태입니다.");
            piece.SetSimulated(true);
            nextDropAt = Time.time + EffectStatUtility.Safe(owner.dropInterval, 0f, 600f, 0.5f);
            nextAutomaticDropAt = Time.time + AutomaticInterval;
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
                    if (usedBonus)
                    {
                        if (reservedBonusRevision == bonusRevision) material.defaultCount++;
                    }
                    else if (useSessionStock)
                    {
                        source?.RefundMaterial(material.mergeItem.item, sourceRevision);
                        if (source != null && source.GameDataRevision == sourceRevision)
                            material.count = source.GetMaterialCount(material.mergeItem.item);
                    }
                    else material.count++;
                    owner.RefreshUI();
                }
            }
            dropping = false;
        }
    }

    private bool IsBlocked(Vector2 position, float radius)
    {
        foreach (var piece in owner.ActiveItems)
            if (piece != null && piece.IsLive && (piece.Position - position).sqrMagnitude < Mathf.Pow(piece.Definition.Radius + radius, 2f)) return true;
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = false };
        var physics = owner.gameObject.scene.GetPhysicsScene2D();
        return !physics.IsValid() || physics.OverlapCircle(position, radius, filter, overlap) > 0;
    }
}
