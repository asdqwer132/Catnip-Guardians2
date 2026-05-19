using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("UI")]
    public BuffUIManager buffUIManager;

    [Header("Runtime Global Buffs")]
    [SerializeField]
    private List<ActiveBuff> globalBuffs = new List<ActiveBuff>();

    private Dictionary<EquipmentBag, List<ActiveBuff>> bagBuffs =
        new Dictionary<EquipmentBag, List<ActiveBuff>>();

    private Dictionary<ItemData, List<ActiveBuff>> itemBuffs =
        new Dictionary<ItemData, List<ActiveBuff>>();

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        bool changed = false;

        if (TickBuffList(globalBuffs, deltaTime))
            changed = true;

        if (TickBagBuffs(deltaTime))
            changed = true;

        if (TickItemBuffs(deltaTime))
            changed = true;

        if (changed)
            RefreshUI();
    }

    public void RegisterBuff(
        BuffEffect effect,
        ItemEffectContext context
    )
    {
        if (effect == null)
            return;

        if (context == null)
            return;

        if (effect.bonus == null)
            return;

        if (effect.buffInfo == null)
            return;

        BuffInfo finalBuffInfo = GetBuffedBuffInfo(
            effect.buffInfo,
            context.sourceItemData,
            context.sourceBag
        );

        if (finalBuffInfo == null)
            return;

        finalBuffInfo.Clamp();

        if (finalBuffInfo.duration <= 0f)
            return;

        switch (effect.targetScope)
        {
            case BuffTarget.Self:
                RegisterOrUpdateItemBuff(
                    context.sourceItemData,
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.Bag:
                RegisterOrUpdateBagBuff(
                    context.sourceBag,
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.All:
                RegisterOrUpdateGlobalBuff(
                    effect,
                    context,
                    finalBuffInfo
                );
                break;
        }

        RefreshUI();
    }

    private void RegisterOrUpdateGlobalBuff(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        ActiveBuff existing = FindSameBuff(
            globalBuffs,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData
        );

        if (existing != null)
        {
            existing.ApplyRegisterAgain(finalBuffInfo);
            return;
        }

        ActiveBuff activeBuff = CreateActiveBuff(
            effect,
            context,
            finalBuffInfo
        );

        globalBuffs.Add(activeBuff);
    }

    private void RegisterOrUpdateBagBuff(
        EquipmentBag bag,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        if (bag == null)
            return;

        if (!bagBuffs.ContainsKey(bag))
            bagBuffs.Add(bag, new List<ActiveBuff>());

        List<ActiveBuff> list = bagBuffs[bag];

        ActiveBuff existing = FindSameBuff(
            list,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData
        );

        if (existing != null)
        {
            existing.ApplyRegisterAgain(finalBuffInfo);
            return;
        }

        ActiveBuff activeBuff = CreateActiveBuff(
            effect,
            context,
            finalBuffInfo
        );

        list.Add(activeBuff);
    }

    private void RegisterOrUpdateItemBuff(
        ItemData itemData,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        if (itemData == null)
            return;

        if (!itemBuffs.ContainsKey(itemData))
            itemBuffs.Add(itemData, new List<ActiveBuff>());

        List<ActiveBuff> list = itemBuffs[itemData];

        ActiveBuff existing = FindSameBuff(
            list,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData
        );

        if (existing != null)
        {
            existing.ApplyRegisterAgain(finalBuffInfo);
            return;
        }

        ActiveBuff activeBuff = CreateActiveBuff(
            effect,
            context,
            finalBuffInfo
        );

        list.Add(activeBuff);
    }

    private ActiveBuff CreateActiveBuff(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        return new ActiveBuff(
            effect.bonus,
            finalBuffInfo,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData,
            effect.includeSelf,
            effect.showInUI
        );
    }

    private ActiveBuff FindSameBuff(
        List<ActiveBuff> buffs,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData
    )
    {
        if (buffs == null)
            return null;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            if (buff.IsSameBuff(
                    sourceItemData,
                    sourceBag,
                    sourceEffectData
                ))
            {
                return buff;
            }
        }

        return null;
    }

    private bool TickBuffList(
        List<ActiveBuff> buffs,
        float deltaTime
    )
    {
        bool changed = false;

        if (buffs == null)
            return false;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
            {
                buffs.RemoveAt(i);
                changed = true;
                continue;
            }

            buff.Tick(deltaTime);

            if (buff.IsExpired)
            {
                buffs.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    private bool TickBagBuffs(float deltaTime)
    {
        bool changed = false;

        List<EquipmentBag> emptyKeys = null;

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in bagBuffs)
        {
            List<ActiveBuff> buffs = pair.Value;

            if (TickBuffList(buffs, deltaTime))
                changed = true;

            if (buffs == null || buffs.Count == 0)
            {
                if (emptyKeys == null)
                    emptyKeys = new List<EquipmentBag>();

                emptyKeys.Add(pair.Key);
            }
        }

        if (emptyKeys != null)
        {
            for (int i = 0; i < emptyKeys.Count; i++)
            {
                bagBuffs.Remove(emptyKeys[i]);
                changed = true;
            }
        }

        return changed;
    }

    private bool TickItemBuffs(float deltaTime)
    {
        bool changed = false;

        List<ItemData> emptyKeys = null;

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in itemBuffs)
        {
            List<ActiveBuff> buffs = pair.Value;

            if (TickBuffList(buffs, deltaTime))
                changed = true;

            if (buffs == null || buffs.Count == 0)
            {
                if (emptyKeys == null)
                    emptyKeys = new List<ItemData>();

                emptyKeys.Add(pair.Key);
            }
        }

        if (emptyKeys != null)
        {
            for (int i = 0; i < emptyKeys.Count; i++)
            {
                itemBuffs.Remove(emptyKeys[i]);
                changed = true;
            }
        }

        return changed;
    }

    public AttackStat GetBuffedAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedStat(
            baseStat,
            targetItemData,
            targetBag,
            ApplyBuffToAttackStat
        );
    }

    public BuffInfo GetBuffedBuffInfo(
        BuffInfo baseInfo,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedStat(
            baseInfo,
            targetItemData,
            targetBag,
            ApplyBuffToBuffInfo
        );
    }

    private T GetBuffedStat<T>(
        T baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag,
        System.Action<BuffStat, T> applyAction
    )
        where T : class, IGameStat<T>
    {
        if (baseStat == null)
            return null;

        T result = baseStat.Clone();

        ApplyBuffsToStat(
            result,
            globalBuffs,
            targetItemData,
            targetBag,
            applyAction
        );

        if (targetBag != null && bagBuffs.ContainsKey(targetBag))
        {
            ApplyBuffsToStat(
                result,
                bagBuffs[targetBag],
                targetItemData,
                targetBag,
                applyAction
            );
        }

        if (targetItemData != null && itemBuffs.ContainsKey(targetItemData))
        {
            ApplyBuffsToStat(
                result,
                itemBuffs[targetItemData],
                targetItemData,
                targetBag,
                applyAction
            );
        }

        result.Clamp();

        return result;
    }

    private void ApplyBuffsToStat<T>(
        T target,
        List<ActiveBuff> buffs,
        ItemData targetItemData,
        EquipmentBag targetBag,
        System.Action<BuffStat, T> applyAction
    )
    {
        if (target == null)
            return;

        if (buffs == null)
            return;

        if (applyAction == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (activeBuff == null)
                continue;

            if (activeBuff.IsExpired)
                continue;

            if (activeBuff.buffStat == null)
                continue;

            if (!CanApplyBuffToTarget(
                    activeBuff,
                    targetItemData,
                    targetBag
                ))
            {
                continue;
            }

            int stackCount = Mathf.Max(1, activeBuff.stack);

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
            {
                applyAction(activeBuff.buffStat, target);
            }
        }
    }

    private bool CanApplyBuffToTarget(
        ActiveBuff activeBuff,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        if (activeBuff == null)
            return false;

        bool isSelf =
            activeBuff.sourceItemData != null &&
            targetItemData != null &&
            activeBuff.sourceItemData == targetItemData;

        if (isSelf && !activeBuff.includeSelf)
            return false;

        return true;
    }

    private void ApplyBuffToAttackStat(
        BuffStat buffStat,
        AttackStat target
    )
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToAttackStat(target);
    }

    private void ApplyBuffToBuffInfo(
        BuffStat buffStat,
        BuffInfo target
    )
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToBuffInfo(target);
    }

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        AddBuffsToList(result, globalBuffs);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in bagBuffs)
        {
            AddBuffsToList(result, pair.Value);
        }

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in itemBuffs)
        {
            AddBuffsToList(result, pair.Value);
        }

        return result;
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        AddBuffsToList(result, globalBuffs, true);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in bagBuffs)
        {
            AddBuffsToList(result, pair.Value, true);
        }

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in itemBuffs)
        {
            AddBuffsToList(result, pair.Value, true);
        }

        return result;
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (bag == null)
            return result;

        if (!bagBuffs.ContainsKey(bag))
            return result;

        AddBuffsToList(result, bagBuffs[bag]);

        return result;
    }

    public List<ActiveBuff> GetVisibleBagBuffsAsList(EquipmentBag bag)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (bag == null)
            return result;

        if (!bagBuffs.ContainsKey(bag))
            return result;

        AddBuffsToList(result, bagBuffs[bag], true);

        return result;
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (itemData == null)
            return result;

        if (!itemBuffs.ContainsKey(itemData))
            return result;

        AddBuffsToList(result, itemBuffs[itemData]);

        return result;
    }

    public List<ActiveBuff> GetVisibleItemBuffsAsList(ItemData itemData)
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (itemData == null)
            return result;

        if (!itemBuffs.ContainsKey(itemData))
            return result;

        AddBuffsToList(result, itemBuffs[itemData], true);

        return result;
    }

    private void AddBuffsToList(
        List<ActiveBuff> target,
        List<ActiveBuff> source,
        bool visibleOnly = false
    )
    {
        if (target == null)
            return;

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            if (visibleOnly && !buff.showInUI)
                continue;

            target.Add(buff);
        }
    }

    public List<ActiveBuff> GetGlobalBuffs()
    {
        return globalBuffs;
    }

    public List<ActiveBuff> GetBagBuffs(EquipmentBag bag)
    {
        if (bag == null)
            return null;

        if (!bagBuffs.ContainsKey(bag))
            return null;

        return bagBuffs[bag];
    }

    public List<ActiveBuff> GetItemBuffs(ItemData itemData)
    {
        if (itemData == null)
            return null;

        if (!itemBuffs.ContainsKey(itemData))
            return null;

        return itemBuffs[itemData];
    }

    public IReadOnlyDictionary<EquipmentBag, List<ActiveBuff>> GetAllBagBuffs()
    {
        return bagBuffs;
    }

    public IReadOnlyDictionary<ItemData, List<ActiveBuff>> GetAllItemBuffs()
    {
        return itemBuffs;
    }

    public void ClearGlobalBuffs()
    {
        globalBuffs.Clear();
        RefreshUI();
    }

    public void ClearBagBuffs(EquipmentBag bag)
    {
        if (bag == null)
            return;

        if (bagBuffs.ContainsKey(bag))
            bagBuffs.Remove(bag);

        RefreshUI();
    }

    public void ClearItemBuffs(ItemData itemData)
    {
        if (itemData == null)
            return;

        if (itemBuffs.ContainsKey(itemData))
            itemBuffs.Remove(itemData);

        RefreshUI();
    }

    public void ClearAllBuffs()
    {
        globalBuffs.Clear();
        bagBuffs.Clear();
        itemBuffs.Clear();

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (buffUIManager == null)
            return;

        buffUIManager.RefreshCurrentMode();
    }
}