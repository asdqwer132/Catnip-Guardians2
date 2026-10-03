using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentBagSnapshot
{
    public BagData bagData;
    public List<InventoryItem> equippedItems = new List<InventoryItem>();

    public EquipmentBagSnapshot()
    {
    }

    public EquipmentBagSnapshot(EquipmentBag bag)
    {
        if (bag == null)
            return;

        bagData = bag.bagData;

        if (bag.equippedItems == null)
            return;

        foreach (InventoryItem item in bag.equippedItems)
        {
            if (item == null)
            {
                equippedItems.Add(new InventoryItem(null, 0));
                continue;
            }

            equippedItems.Add(
                new InventoryItem(item.itemData, item.amount)
            );
        }
    }
}

[System.Serializable]
public class SkillBuffSnapshot
{
    public ItemData itemData;
    public string bagId;

    public SkillBuffSnapshot()
    {
    }

    public SkillBuffSnapshot(RegisteredBuffSkillItem item)
    {
        if (item == null)
            return;

        itemData = item.itemData;
        bagId = item.bagId;
    }
}

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    [Header("Equipment Bag Data")]
    public List<EquipmentBagSnapshot> equipmentBags =
        new List<EquipmentBagSnapshot>();

    [Header("Skill Buff Data")]
    public List<SkillBuffSnapshot> skillBuffs =
        new List<SkillBuffSnapshot>();

    public bool HasEquipmentBagData =>
        equipmentBags != null &&
        equipmentBags.Count > 0;

    public bool HasSkillBuffData =>
        skillBuffs != null &&
        skillBuffs.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // Equipment Bag
    // =========================================================

    public void SaveEquipmentBags(
        IReadOnlyList<EquipmentBag> bags
    )
    {
        if (equipmentBags == null)
            equipmentBags = new List<EquipmentBagSnapshot>();

        equipmentBags.Clear();

        if (bags == null)
            return;

        for (int i = 0; i < bags.Count; i++)
        {
            EquipmentBag bag = bags[i];

            if (bag == null)
            {
                equipmentBags.Add(null);
                continue;
            }

            equipmentBags.Add(
                bag.CreateSnapshot()
            );
        }
    }

    public void LoadEquipmentBags(
        IReadOnlyList<EquipmentBag> bags
    )
    {
        if (!HasEquipmentBagData)
            return;

        if (bags == null)
            return;

        int count = Mathf.Min(
            equipmentBags.Count,
            bags.Count
        );

        for (int i = 0; i < count; i++)
        {
            EquipmentBag targetBag = bags[i];
            EquipmentBagSnapshot snapshot = equipmentBags[i];

            if (targetBag == null)
                continue;

            if (snapshot == null)
                continue;

            targetBag.ApplySnapshot(snapshot);
        }
    }

    // =========================================================
    // Skill Buff
    // =========================================================

    public void SaveSkillBuff(
        IReadOnlyList<RegisteredBuffSkillItem> registeredItems
    )
    {
        if (skillBuffs == null)
            skillBuffs = new List<SkillBuffSnapshot>();

        skillBuffs.Clear();

        if (registeredItems == null)
            return;

        for (int i = 0; i < registeredItems.Count; i++)
        {
            RegisteredBuffSkillItem item = registeredItems[i];

            if (item == null)
                continue;

            if (item.itemData == null)
                continue;

            skillBuffs.Add(
                new SkillBuffSnapshot(item)
            );
        }
    }

    public void LoadSkillBuff(
        BuffSkillManager buffSkillManager
    )
    {
        if (buffSkillManager == null)
            return;

        // 기존 등록 제거
        buffSkillManager.ClearRegisteredBuffItems();

        if (!HasSkillBuffData)
            return;

        for (int i = 0; i < skillBuffs.Count; i++)
        {
            SkillBuffSnapshot snapshot = skillBuffs[i];

            if (snapshot == null)
                continue;

            if (snapshot.itemData == null)
                continue;

            buffSkillManager.RegisterBuffItem(
                snapshot.itemData,
                snapshot.bagId
            );
        }
    }

    // =========================================================
    // Clear
    // =========================================================

    public void ClearEquipmentBagData()
    {
        if (equipmentBags != null)
            equipmentBags.Clear();
    }

    public void ClearSkillBuffData()
    {
        if (skillBuffs != null)
            skillBuffs.Clear();
    }

    public void ClearAll()
    {
        ClearEquipmentBagData();
        ClearSkillBuffData();
    }
}