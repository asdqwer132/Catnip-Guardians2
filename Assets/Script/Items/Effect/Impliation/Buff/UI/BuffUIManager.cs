using System.Collections.Generic;
using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    [Header("References")]
    public BuffManager buffManager;
    public Transform contentParent;
    public BuffUISlot slotPrefab;

    [Header("Display")]
    public BuffUIDisplayMode displayMode = BuffUIDisplayMode.All;
    public bool refreshOnEnable = true;

    [Header("Item Targets")]
    public EquipmentBag targetBag;
    public ItemData targetItemData;
    public ItemSeries targetItemSeries = ItemSeries.None;

    [Header("Buff Target")]
    [Tooltip("IBuffTarget을 구현한 MonoBehaviour를 넣으면 됨. Enemy, Player, EnemySpawner, Plant, Tower 등")]
    public MonoBehaviour targetBuffTargetObject;

    [Tooltip("Enemy, Player, EnemySpawner, Plant, Tower 같은 그룹 이름")]
    public string targetGroup;

    private IBuffTarget targetBuffTarget;
    private readonly List<BuffUISlot> spawnedSlots = new List<BuffUISlot>();

    private void OnEnable()
    {
        if (refreshOnEnable)
            RefreshCurrentMode();
    }

    public void Init()
    {
        RefreshCurrentMode();
    }

    public void RefreshCurrentMode()
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        CacheTargetBuffTarget();

        switch (displayMode)
        {
            case BuffUIDisplayMode.Bag:
                DisplayBagBuffs(targetBag);
                break;

            case BuffUIDisplayMode.Item:
                DisplayItemBuffs(targetItemData);
                break;

            case BuffUIDisplayMode.ItemSeries:
                DisplayItemSeriesBuffs(targetItemSeries);
                break;

            case BuffUIDisplayMode.Target:
                DisplayTargetBuffs(targetBuffTarget);
                break;

            case BuffUIDisplayMode.Group:
                DisplayGroupBuffs(targetGroup);
                break;

            default:
                DisplayAllBuffs();
                break;
        }
    }

    public void DisplayAllBuffs()
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.All;
        RefreshSlots(buffManager.GetAllVisibleBuffs(), "전체 버프");
    }

    public void DisplayBagBuffs(EquipmentBag bag)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.Bag;
        targetBag = bag;

        string label = bag != null ? "가방 버프: " + bag.name : "가방 버프";
        RefreshSlots(buffManager.GetVisibleBagBuffsAsList(bag), label);
    }

    public void DisplayItemBuffs(ItemData itemData)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.Item;
        targetItemData = itemData;

        string itemName = itemData != null ? itemData.GetDataName() : "";
        string label = itemData != null ? "아이템 버프: " + itemName : "아이템 버프";

        RefreshSlots(buffManager.GetVisibleItemBuffsAsList(itemData), label);
    }

    public void DisplayItemSeriesBuffs(ItemSeries series)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.ItemSeries;
        targetItemSeries = series;

        string label = series != ItemSeries.None ? "시리즈 버프: " + series : "시리즈 버프";
        RefreshSlots(buffManager.GetVisibleItemSeriesBuffsAsList(series), label);
    }

    public void DisplayTargetBuffs(IBuffTarget target)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.Target;
        targetBuffTarget = target;

        string label = target != null
            ? "대상 버프: " + target.BuffTargetDebugName
            : "대상 버프";

        RefreshSlots(buffManager.GetVisibleTargetBuffsAsList(target), label);
    }

    public void DisplayGroupBuffs(string group)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.Group;
        targetGroup = group;

        string label = !string.IsNullOrEmpty(group)
            ? "그룹 버프: " + group
            : "그룹 버프";

        RefreshSlots(buffManager.GetVisibleTargetGroupBuffsAsList(group), label);
    }

    public void SetTargetBag(EquipmentBag bag)
    {
        targetBag = bag;

        if (displayMode == BuffUIDisplayMode.Bag)
            DisplayBagBuffs(targetBag);
    }

    public void SetTargetItem(ItemData itemData)
    {
        targetItemData = itemData;

        if (displayMode == BuffUIDisplayMode.Item)
            DisplayItemBuffs(targetItemData);
    }

    public void SetTargetItemSeries(ItemSeries series)
    {
        targetItemSeries = series;

        if (displayMode == BuffUIDisplayMode.ItemSeries)
            DisplayItemSeriesBuffs(targetItemSeries);
    }

    public void SetTargetBuffTargetObject(MonoBehaviour targetObject)
    {
        targetBuffTargetObject = targetObject;
        CacheTargetBuffTarget();

        if (displayMode == BuffUIDisplayMode.Target)
            DisplayTargetBuffs(targetBuffTarget);
    }

    public void SetTargetBuffTarget(IBuffTarget target)
    {
        targetBuffTarget = target;
        targetBuffTargetObject = null;

        if (target != null)
        {
            if (target.BuffTargetObject is MonoBehaviour mono)
                targetBuffTargetObject = mono;
            else if (target.BuffTargetObject is GameObject go)
                targetBuffTargetObject = go.GetComponent<MonoBehaviour>();
        }

        if (displayMode == BuffUIDisplayMode.Target)
            DisplayTargetBuffs(targetBuffTarget);
    }

    public void SetTargetGroup(string group)
    {
        targetGroup = group;

        if (displayMode == BuffUIDisplayMode.Group)
            DisplayGroupBuffs(targetGroup);
    }

    private void CacheTargetBuffTarget()
    {
        targetBuffTarget = null;

        if (targetBuffTargetObject == null)
            return;

        targetBuffTarget = targetBuffTargetObject as IBuffTarget;

        if (targetBuffTarget != null)
            return;

        targetBuffTarget = targetBuffTargetObject.GetComponent<IBuffTarget>();
    }

    private void RefreshSlots(IReadOnlyList<ActiveBuff> buffs, string displayLabel)
    {
        ClearSlots();

        if (buffs == null || contentParent == null || slotPrefab == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null || buff.IsExpired)
                continue;

            BuffUISlot slot = Instantiate(slotPrefab, contentParent);
            slot.Set(buff, displayLabel);
            spawnedSlots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();
    }
}