using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("Runtime Global Buffs")]
    [SerializeField]
    private List<ActiveBuff> globalBuffs = new List<ActiveBuff>();

    [Header("UI")]
    public BuffUIManager buffUIManager;

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

        if (effect.buffInfo.duration <= 0f)
            return;

        ActiveBuff activeBuff = new ActiveBuff(
            effect.bonus,
            effect.buffInfo.duration,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData
        );

        switch (effect.targetScope)
        {
            case BuffTarget.Self:
                RegisterItemBuff(context.sourceItemData, activeBuff);
                break;

            case BuffTarget.SameBag:
                RegisterBagBuff(context.sourceBag, activeBuff);
                break;

            case BuffTarget.All:
                RegisterGlobalBuff(activeBuff);
                break;
        }

        RefreshUI();
    }

    private void RegisterGlobalBuff(ActiveBuff buff)
    {
        if (buff == null)
            return;

        globalBuffs.Add(buff);
    }

    private void RegisterBagBuff(
        EquipmentBag bag,
        ActiveBuff buff
    )
    {
        if (bag == null || buff == null)
            return;

        if (!bagBuffs.ContainsKey(bag))
        {
            bagBuffs.Add(bag, new List<ActiveBuff>());
        }

        bagBuffs[bag].Add(buff);
    }

    private void RegisterItemBuff(
        ItemData itemData,
        ActiveBuff buff
    )
    {
        if (itemData == null || buff == null)
            return;

        if (!itemBuffs.ContainsKey(itemData))
        {
            itemBuffs.Add(itemData, new List<ActiveBuff>());
        }

        itemBuffs[itemData].Add(buff);
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
        if (baseStat == null)
            return null;

        AttackStat result = baseStat.Clone();

        ApplyAttackBuffList(result, globalBuffs);

        if (targetBag != null && bagBuffs.ContainsKey(targetBag))
        {
            ApplyAttackBuffList(result, bagBuffs[targetBag]);
        }

        if (targetItemData != null && itemBuffs.ContainsKey(targetItemData))
        {
            ApplyAttackBuffList(result, itemBuffs[targetItemData]);
        }

        ClampAttackStat(result);

        return result;
    }

    private void ApplyAttackBuffList(
        AttackStat target,
        List<ActiveBuff> buffs
    )
    {
        if (target == null || buffs == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (activeBuff == null)
                continue;

            if (activeBuff.buffStat == null)
                continue;

            if (activeBuff.buffStat.attackBuffStat == null)
                continue;

            ApplyAttackBuff(
                target,
                activeBuff.buffStat.attackBuffStat
            );
        }
    }

    private void ApplyAttackBuff(
        AttackStat target,
        AttackBuffStat buff
    )
    {
        if (target == null || buff == null)
            return;

        target.attackPower += buff.attackPower;
        target.damageInterval += buff.damageInterval;
        target.attackRange += buff.attackRange;
        target.attackLifeTime += buff.attackLifeTime;

        target.attackPower *= 1f + buff.attackPowerM;
        target.damageInterval *= 1f + buff.damageIntervalM;
        target.attackRange *= 1f + buff.attackRangeM;
        target.attackLifeTime *= 1f + buff.attackLifeTimeM;
    }

    private void ClampAttackStat(AttackStat stat)
    {
        if (stat == null)
            return;

        if (stat.damageInterval < 0.01f)
            stat.damageInterval = 0.01f;

        if (stat.attackRange < 0f)
            stat.attackRange = 0f;

        if (stat.attackLifeTime < 0.01f)
            stat.attackLifeTime = 0.01f;
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

    private void AddBuffsToList(
        List<ActiveBuff> target,
        List<ActiveBuff> source
    )
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
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