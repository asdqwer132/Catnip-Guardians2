using System.Collections.Generic;
using UnityEngine;

public class RuntimeBuffStatusUI : MonoBehaviour
{
    [Header("UI")]
    public RuntimeBuffStatusSlotUI slotPrefab;
    public Transform slotParent;

    private readonly List<RuntimeBuffStatusSlotUI> activeSlots =
        new List<RuntimeBuffStatusSlotUI>();

    private void OnEnable()
    {
        BuffBagItemsEffect.OnBuffActivated += HandleBuffActivated;
    }

    private void OnDisable()
    {
        BuffBagItemsEffect.OnBuffActivated -= HandleBuffActivated;
    }

    private void HandleBuffActivated(RuntimeBuffUIInfo info)
    {
        if (info == null)
            return;

        if (slotPrefab == null)
        {
            Debug.LogWarning("RuntimeBuffStatusUI: slotPrefab이 없습니다.");
            return;
        }

        if (slotParent == null)
        {
            Debug.LogWarning("RuntimeBuffStatusUI: slotParent가 없습니다.");
            return;
        }

        RuntimeBuffStatusSlotUI slot = FindSlot(info);

        if (slot == null)
        {
            slot = Instantiate(slotPrefab, slotParent);
            activeSlots.Add(slot);

            Debug.Log("새 버프 UI 생성 / 범위: " + info.scope);
        }

        slot.Bind(info);
    }

    private RuntimeBuffStatusSlotUI FindSlot(RuntimeBuffUIInfo info)
    {
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            RuntimeBuffStatusSlotUI slot = activeSlots[i];

            if (slot == null)
            {
                activeSlots.RemoveAt(i);
                continue;
            }

            if (slot.IsSameBuff(info))
                return slot;
        }

        return null;
    }

    public void RemoveSlot(RuntimeBuffStatusSlotUI slot)
    {
        if (slot == null)
            return;

        activeSlots.Remove(slot);
        Destroy(slot.gameObject);
    }
}