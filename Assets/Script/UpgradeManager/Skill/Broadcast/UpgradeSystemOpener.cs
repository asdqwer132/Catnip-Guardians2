using System;
using UnityEngine;



[Serializable]
public class ObjectByKey
{
    public GameObject obj;
    public string key;
}
public class UpgradeSystemOpener : RefreshListener
{
    [SerializeField] private ObjectByKey[] gimickSKills;
    protected override void Refresh(RefreshType refreshType)
    {
        foreach (ObjectByKey gimickSKill in gimickSKills)
        {
            bool isUnlocked = UnlockManager.Instance.IsUnlocked(DataType.System, gimickSKill.key);
            gimickSKill.obj.SetActive(isUnlocked);
        }
    }
}
