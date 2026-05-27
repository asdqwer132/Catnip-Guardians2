using System.Collections.Generic;
using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    public BuffManager buffManager;
    public Transform contentParent;
    public BuffUISlot slotPrefab;
    public BuffTarget displayMode = BuffTarget.All;
    public EquipmentBag targetBag;
    public ItemData targetItemData;

    private readonly List<BuffUISlot> spawnedSlots = new List<BuffUISlot>();

    private void Start()
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
            case BuffTarget.Bag:
                DisplayBagBuffs(targetBag);
                break;
            case BuffTarget.Item:
                DisplayItemBuffs(targetItemData);
                break;
            default:
                DisplayAllBuffs();
                break;
        }
    }

    public void DisplayAllBuffs()
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.All;
        RefreshSlots(buffManager.GetAllVisibleBuffs(), "전체 버프");
    }

    public void DisplayBagBuffs(EquipmentBag bag)
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.Bag;
        targetBag = bag;
        RefreshSlots(buffManager.GetVisibleBagBuffsAsList(bag), bag != null ? "가방 버프: " + bag.name : "가방 버프");
    }

    public void DisplayItemBuffs(ItemData itemData)
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.Item;
        targetItemData = itemData;
        RefreshSlots(buffManager.GetVisibleItemBuffsAsList(itemData), itemData != null ? "아이템 버프: " + itemData.GetDataName() : "아이템 버프");
    }

    public void SetTargetBag(EquipmentBag bag)
    {
        targetBag = bag;

        if (displayMode == BuffTarget.Bag)
            DisplayBagBuffs(targetBag);
    }

    public void SetTargetItem(ItemData itemData)
    {
        targetItemData = itemData;

        if (displayMode == BuffTarget.Item)
            DisplayItemBuffs(targetItemData);
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