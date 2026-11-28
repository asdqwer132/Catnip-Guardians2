using System;
using UnityEngine;



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
public class UpgradeSystemOpener : RefreshListener
{
    [SerializeField] private ObjectByKey[] gimickSkills;
    [SerializeField] private TreeByKey[] plants;
    protected override void Refresh(RefreshType refreshType)
    {
        foreach (ObjectByKey gimickSKill in gimickSkills)
        {
            bool isUnlocked = UnlockManager.Instance.IsUnlocked(DataType.System, gimickSKill.key);
            gimickSKill.obj.SetActive(isUnlocked);
        }
    }
}
