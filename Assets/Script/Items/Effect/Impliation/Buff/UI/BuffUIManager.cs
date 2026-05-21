using System.Collections.Generic;
using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    [Header("References")]
    public BuffManager buffManager;

    [Header("UI")]
    public Transform contentParent;
    public BuffUISlot slotPrefab;

    [Header("Default Display")]
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
            case BuffTarget.All:
                DisplayAllBuffs();
                break;

            case BuffTarget.Bag:
                DisplayBagBuffs(targetBag);
                break;

            case BuffTarget.Item:
                DisplayItemBuffs(targetItemData);
                break;
        }
    }

    public void DisplayAllBuffs()
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.All;

        List<ActiveBuff> buffs = buffManager.GetAllVisibleBuffs();

        RefreshSlots(
            buffs,
            "전체 버프"
        );
    }

    public void DisplayBagBuffs(EquipmentBag bag)
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.Bag;
        targetBag = bag;

        List<ActiveBuff> buffs = buffManager.GetVisibleBagBuffsAsList(bag);

        string label = "가방 버프";

        if (bag != null)
            label = "가방 버프: " + bag.name;

        RefreshSlots(
            buffs,
            label
        );
    }

    public void DisplayItemBuffs(ItemData itemData)
    {
        if (buffManager == null)
            return;

        displayMode = BuffTarget.Item;
        targetItemData = itemData;

        List<ActiveBuff> buffs = buffManager.GetVisibleItemBuffsAsList(itemData);

        string label = "아이템 버프";

        if (itemData != null)
            label = "아이템 버프: " + itemData.dataName;

        RefreshSlots(
            buffs,
            label
        );
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

    private void RefreshSlots(
        IReadOnlyList<ActiveBuff> buffs,
        string displayLabel
    )
    {
        ClearSlots();

        if (buffs == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            CreateSlot(buff, displayLabel);
        }
    }

    private void CreateSlot(
        ActiveBuff activeBuff,
        string displayLabel
    )
    {
        if (contentParent == null)
            return;

        if (slotPrefab == null)
            return;

        BuffUISlot slot = Instantiate(slotPrefab, contentParent);

        slot.Set(
            activeBuff,
            displayLabel
        );

        spawnedSlots.Add(slot);
    }

    private void ClearSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            BuffUISlot slot = spawnedSlots[i];

            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }
}