using UnityEngine;
[System.Serializable]
public class SpecialItem
{
    public PlayerStatusList targetStat;
    public ItemData item;
}
public class SpecialItemManager : MonoBehaviour
{
    public static SpecialItemManager Instance;
    public ItemThrowExecutor ItemThrowExecutor;
    public ItemData specialItems;
    private void Awake()
    {
        Instance = this;
    }
    public void Call(ItemEffectContext itemEffectContext)
    {
        if (itemEffectContext == null || !itemEffectContext.CanContinue || itemEffectContext.sourceItemData == null ||
            StatusManager.Instance == null || ItemThrowExecutor == null || specialItems == null) return;
        if (StatusManager.Instance.HasStatus(PlayerStatusList.star))
        {
            if (itemEffectContext.sourceItemData != specialItems && itemEffectContext.sourceItemData.dataId != "WeaponExtra1")
            {
                Debug.Log("asd");
                ItemThrowExecutor.Throw(
                    specialItems,
                    itemEffectContext.targetPosition + new Vector3(0, 3, 0),
                    itemEffectContext.targetPosition,
                    gameObject,
                    null,
                    0
                    );
            }
        }
    }
}
