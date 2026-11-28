using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class ObjectByKey
{
    public GameObject obj;
    public string key;
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
    [FormerlySerializedAs("gimickSKills")]
    [SerializeField] private ObjectByKey[] gimickSkills = Array.Empty<ObjectByKey>();
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
        foreach (var target in gimickSkills)
            if (target?.obj != null) target.obj.SetActive(IsOpen(keys, target.key));
        Changed?.Invoke();
    }
}
