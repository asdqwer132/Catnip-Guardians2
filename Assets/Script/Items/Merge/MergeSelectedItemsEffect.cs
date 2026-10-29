using System.Collections.Generic;
using UnityEngine;

// 선택 확정 후 호출되는 연출 창구입니다. 실제 애니메이션은 이 UI 영역에서 추가합니다.
public class MergeSelectedItemsEffect : MonoBehaviour
{
    public virtual void Play(IReadOnlyList<InventoryItem> items)
    {
        Debug.Log($"[MergeSelectedItemsEffect] 선택 확정 연출 호출: {items?.Count ?? 0}개", this);
    }
}
