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
    [Tooltip("이 상태 키가 플레이어에게 활성화되어 있으면 특수 아이템을 실행합니다. StatusStat 수치는 검사하지 않습니다.")]
    public StatusDefinition requiredStatusKey;
    private void Awake()
    {
        Instance = this;
    }
    public void Call(ItemEffectContext itemEffectContext)
    {
        if (!CanTrigger(itemEffectContext)) return;
        ItemThrowExecutor.Throw(
            specialItems,
            itemEffectContext.targetPosition + new Vector3(0, 3, 0),
            itemEffectContext.targetPosition,
            gameObject,
            null,
            0,
            triggerSpecialItems: false
        );
    }

    public bool CanTrigger(ItemEffectContext context)
        => context != null && context.CanContinue && context.sourceItemData != null &&
            StatusManager.Instance != null && ItemThrowExecutor != null && specialItems != null &&
            context.sourceItemData != specialItems && context.sourceItemData.dataId != "WeaponExtra1" &&
            StatusManager.Instance.HasStatus(requiredStatusKey);

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
