using UnityEngine;

// 비행 중 취소도 부모 효과에 전달한다. 착지는 적 명중 여부와 관계없는 사건이다.
public sealed class ItemThrowCompletion : MonoBehaviour
{
    private ItemEffectLease lease;
    public void Init(ItemEffectContext context) { lease = context.RetainLifetime(); }
    public void Complete()
    {
        ItemEffectLease finished = lease;
        lease = null;
        if (finished != null) finished.Finish();
    }
    private void OnDisable()
    {
        if (lease != null) lease.Cancel();
        lease = null;
    }
}
