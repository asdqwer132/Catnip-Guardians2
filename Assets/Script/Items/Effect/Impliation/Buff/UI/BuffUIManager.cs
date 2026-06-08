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

    [Header("Targets")]
    public EquipmentBag targetBag;
    public ItemData targetItemData;
    public ItemSeries targetItemSeries = ItemSeries.None;
    public Enemy targetEnemy;
    public EnemySpawner targetEnemySpawner;

    private readonly List<BuffUISlot> spawnedSlots = new List<BuffUISlot>();



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
            case BuffUIDisplayMode.Enemy:
                DisplayEnemyBuffs(targetEnemy);
                break;
            case BuffUIDisplayMode.EnemySpawner:
                DisplayEnemySpawnerBuffs(targetEnemySpawner);
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

    public void DisplayEnemyBuffs(Enemy enemy)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.Enemy;
        targetEnemy = enemy;
        string label = enemy != null ? "적 버프: " + enemy.name : "적 버프";
        RefreshSlots(buffManager.GetVisibleEnemyBuffsAsList(enemy), label);
    }

    public void DisplayEnemySpawnerBuffs(EnemySpawner spawner)
    {
        if (buffManager == null)
        {
            ClearSlots();
            return;
        }

        displayMode = BuffUIDisplayMode.EnemySpawner;
        targetEnemySpawner = spawner;
        string label = spawner != null ? "스포너 버프: " + spawner.name : "스포너 버프";
        RefreshSlots(buffManager.GetVisibleEnemySpawnerBuffsAsList(spawner), label);
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

    public void SetTargetEnemy(Enemy enemy)
    {
        targetEnemy = enemy;
        if (displayMode == BuffUIDisplayMode.Enemy)
            DisplayEnemyBuffs(targetEnemy);
    }

    public void SetTargetEnemySpawner(EnemySpawner spawner)
    {
        targetEnemySpawner = spawner;
        if (displayMode == BuffUIDisplayMode.EnemySpawner)
            DisplayEnemySpawnerBuffs(targetEnemySpawner);
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
