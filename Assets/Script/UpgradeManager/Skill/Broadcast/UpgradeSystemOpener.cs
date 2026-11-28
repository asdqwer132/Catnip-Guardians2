using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class GimmickSkillGroup
{
    public MergeBoxEquipmentData data;
    public Toggle toggle;
    public GameObject selected;
    public Image representativeIcon;
    public InventorySlotUI[] skillViews = new InventorySlotUI[3];

    public void ApplyUnlocks(IReadOnlyList<string> keys)
    {
        if (data == null) return;
        int box = data.boxIndex;
        for (int index = 0; index < skillViews.Length; index++)
        {
            var slot = skillViews[index];
            if (slot != null)
                slot.gameObject.SetActive(UpgradeSystemOpener.IsSkillOpen(keys, box, index + 1));
        }
        if (toggle != null)
            toggle.gameObject.SetActive(UpgradeSystemOpener.IsBoxOpen(keys, box));
    }
}

[Serializable]
public class TreeByKey
{
    public PlantData plantData;
    public string key;
}

public sealed class UpgradeSystemOpener : RefreshListener
{
    [SerializeField] private UnlockManager unlockManager;
    [SerializeField] private GimmickSkillGroup[] gimmickSkills = Array.Empty<GimmickSkillGroup>();
    public IReadOnlyList<GimmickSkillGroup> GimmickSkills => gimmickSkills;
    [SerializeField] private TreeByKey[] plants = Array.Empty<TreeByKey>();

    public event Action Changed;

    public IReadOnlyList<string> ReadKeys()
    {
        var manager = unlockManager != null ? unlockManager : UnlockManager.Instance;
        return manager != null ? EquipmentUnlocks.Capture(manager) : null;
    }

    public static bool IsOpen(IReadOnlyList<string> keys, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (GameManager.DebugMode) return true;
        if (keys != null)
            foreach (string value in keys)
                if (value == key) return true;
        return false;
    }

    public static bool IsBoxOpen(IReadOnlyList<string> keys, int box) =>
        box >= 1 && box <= EquipmentUnlocks.BoxCount && IsOpen(keys, EquipmentUnlocks.BoxKey(box));

    public static bool IsSkillOpen(IReadOnlyList<string> keys, int box, int skill) =>
        skill >= 1 && skill <= 3 && IsBoxOpen(keys, box) && IsOpen(keys, EquipmentUnlocks.SkillKey(box, skill));

    public static bool IsTreeOpen(IReadOnlyList<string> keys, int index) =>
        index >= 1 && index <= EquipmentUnlocks.BoxCount && IsOpen(keys, EquipmentUnlocks.EggKey(index));

    public PlantData GetOpenTree(int index, IReadOnlyList<string> keys)
    {
        if (!IsTreeOpen(keys, index)) return null;
        string key = EquipmentUnlocks.EggKey(index);
        foreach (var tree in plants)
            if (tree?.key == key && tree.plantData != null) return tree.plantData;
        return null;
    }

    protected override void Start()
    {
        base.Start();
        Refresh(RefreshType.Unlock);
    }

    protected override void Refresh(RefreshType refreshType)
    {
        var keys = ReadKeys();
        foreach (var group in gimmickSkills) group?.ApplyUnlocks(keys);
        Changed?.Invoke();
    }
}
