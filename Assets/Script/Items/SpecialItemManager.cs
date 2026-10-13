using System.Collections.Generic;
using UnityEngine;

// 상태별 기능은 상태 키의 모듈 에셋에 있습니다. 이 컴포넌트는 사용 사건을 전달합니다.
public class SpecialItemManager : MonoBehaviour
{
    public static SpecialItemManager Instance;
    [Header("모듈 실행에 필요한 참조")]
    public ItemThrowExecutor ItemThrowExecutor;
    [Tooltip("비우면 아이템 소유자의 StatusManager, 이후 전역 PlayerStatus를 찾습니다.")]
    public StatusManager statusManager;

    private void Awake() { Instance = this; }

    private StatusManager ResolveStatusManager(ItemEffectContext context)
    {
        StatusManager receiver = statusManager;
        if (receiver == null && context?.owner != null) receiver = context.owner.GetComponentInParent<StatusManager>();
        if (receiver == null) receiver = StatusManager.Instance;
        if (receiver == null || !receiver.isActiveAndEnabled ||
            (context?.buffManager != null && receiver.buffManager != context.buffManager)) return null;
        return receiver;
    }

    public void Call(ItemEffectContext context)
    {
        if (!isActiveAndEnabled || context == null || !context.CanContinue || !context.allowItemUseReactions) return;
        StatusManager receiver = ResolveStatusManager(context);
        if (receiver == null) return;
        var statuses = new List<ActiveBuff>();
        receiver.GetActiveSpecialStatuses(statuses);
        SpecialStatusItemUseDispatcher.Execute(context, statuses, ItemThrowExecutor);
    }

    // 미리보기는 조건만 조회합니다. 난수·추가 효과·사용 횟수를 소비하지 않습니다.
    public bool CanTrigger(ItemEffectContext context)
    {
        if (!isActiveAndEnabled || context == null || !context.CanContinue || !context.allowItemUseReactions) return false;
        StatusManager receiver = ResolveStatusManager(context);
        if (receiver == null) return false;
        var statuses = new List<ActiveBuff>(); receiver.GetActiveSpecialStatuses(statuses);
        foreach (ActiveBuff status in statuses)
            if (status.specialStatusKey.onItemUsedModules != null)
                foreach (SpecialStatusItemUseModule module in status.specialStatusKey.onItemUsedModules)
                    if (module != null && module.CanReact(context, ItemThrowExecutor)) return true;
        return false;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
