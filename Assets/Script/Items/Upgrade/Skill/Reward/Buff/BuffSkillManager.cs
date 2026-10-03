using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegisteredBuffSkillItem
{
    public ItemData itemData;
    public string bagId;

    public RegisteredBuffSkillItem(
        ItemData itemData,
        string bagId
    )
    {
        this.itemData = itemData;
        this.bagId = bagId;
    }
}

public class BuffSkillManager : MonoBehaviour
{
    [Header("Executor")]
    public ItemEffectExecutor itemEffectExecutor;

    [Header("Bag Manager")]
    public GameItemBagManager equipmentBagManager;

    [Header("Registered Buff Items")]
    [SerializeField]
    private List<RegisteredBuffSkillItem> registeredBuffItems =
        new List<RegisteredBuffSkillItem>();

    public IReadOnlyList<RegisteredBuffSkillItem> RegisteredBuffItems
    {
        get { return registeredBuffItems; }
    }

    public void RegisterBuffItem(
        ItemData itemData,
        string bagId
    )
    {
        if (itemData == null)
            return;

        if (!ItemEffectExecutor.CanExecuteItemEffect(itemData))
        {
            Debug.LogWarning(
                itemData.GetDataName() +
                " 은 실행 가능한 이펙트가 없습니다."
            );

            return;
        }

        // 같은 아이템 + 같은 가방 중복 등록 방지
        for (int i = 0; i < registeredBuffItems.Count; i++)
        {
            RegisteredBuffSkillItem registeredItem =
                registeredBuffItems[i];

            if (registeredItem == null)
                continue;

            if (registeredItem.itemData == itemData &&
                registeredItem.bagId == bagId)
            {
                return;
            }
        }

        registeredBuffItems.Add(
            new RegisteredBuffSkillItem(
                itemData,
                bagId
            )
        );

        Debug.Log(
            "버프 스킬 아이템 등록: " +
            itemData.GetDataName() +
            " / BagId: " +
            bagId
        );
    }

    public void ExecuteAllRegisteredBuffItems(
        GameObject owner,
        int currentCycleId
    )
    {
        if (itemEffectExecutor == null)
        {
            Debug.LogWarning(
                "BuffSkillManager에 ItemEffectExecutor가 없습니다."
            );

            return;
        }

        for (int i = 0; i < registeredBuffItems.Count; i++)
        {
            RegisteredBuffSkillItem registeredItem =
                registeredBuffItems[i];

            if (registeredItem == null)
                continue;

            ExecuteRegisteredBuffItem(
                registeredItem,
                owner,
                currentCycleId
            );
        }
    }

    private void ExecuteRegisteredBuffItem(
        RegisteredBuffSkillItem registeredItem,
        GameObject owner,
        int currentCycleId
    )
    {
        if (registeredItem.itemData == null)
            return;

        EquipmentBag targetBag = null;

        // bagId가 있으면
        // GameScene의 EquipmentBag을 다시 찾아준다.
        if (!string.IsNullOrEmpty(registeredItem.bagId))
        {
            if (equipmentBagManager == null)
            {
                Debug.LogWarning(
                    "GameItemBagManager가 없습니다."
                );

                return;
            }

            targetBag =
                equipmentBagManager.GetBagData(
                    registeredItem.bagId
                );

            if (targetBag == null)
            {
                Debug.LogWarning(
                    "BagId에 해당하는 가방을 찾지 못했습니다: " +
                    registeredItem.bagId
                );

                return;
            }
        }

        itemEffectExecutor.ExecuteItemEffect(
            registeredItem.itemData,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            owner,
            targetBag,
            currentCycleId
        );
    }

    public void ClearRegisteredBuffItems()
    {
        registeredBuffItems.Clear();
    }
}