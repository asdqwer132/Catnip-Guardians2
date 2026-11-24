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
    [SerializeField] private ObjectByKey[] mapToggles;
    [SerializeField] private ObjectByKey[] pannelToggles;
    protected override void Refresh(RefreshType refreshType)
    {
        foreach (ObjectByKey mapToggle in mapToggles)
        {
            bool isUnlocked = UnlockManager.Instance.IsUnlocked(DataType.System, mapToggle.key);
            mapToggle.obj.SetActive(isUnlocked);
        }
        foreach (ObjectByKey pannelToggle in pannelToggles)
        {
            bool isUnlocked = UnlockManager.Instance.IsUnlocked(DataType.System, pannelToggle.key);
            pannelToggle.obj.SetActive(isUnlocked);
        }
    }
}
