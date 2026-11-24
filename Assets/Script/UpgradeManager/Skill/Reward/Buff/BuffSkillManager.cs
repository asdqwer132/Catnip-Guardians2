using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegisteredBuffSkillItem
{
    public ItemData itemData;
    public RegisteredBuffSkillItem(ItemData itemData) => this.itemData = itemData;
}

public class BuffSkillManager : MonoBehaviour
{
    public ItemEffectExecutor itemEffectExecutor;
    [SerializeField] private List<RegisteredBuffSkillItem> registeredBuffItems = new List<RegisteredBuffSkillItem>();
    public IReadOnlyList<RegisteredBuffSkillItem> RegisteredBuffItems => registeredBuffItems;

    public void RegisterBuffItem(ItemData itemData)
    {
        if (!ItemEffectExecutor.CanExecuteItemEffect(itemData)) return;
        if (registeredBuffItems.Exists(entry => entry != null && entry.itemData == itemData)) return;
        registeredBuffItems.Add(new RegisteredBuffSkillItem(itemData));
    }
    public void ExecuteAllRegisteredBuffItems(GameObject owner)
    {
        if (itemEffectExecutor == null) return;
        foreach (var entry in registeredBuffItems)
            if (entry?.itemData != null)
                itemEffectExecutor.ExecuteItemEffect(entry.itemData, Vector3.zero, Vector3.zero, Vector3.zero, owner, ItemEffectOrigin.Skill);
    }
    public void ClearRegisteredBuffItems() => registeredBuffItems.Clear();
}
