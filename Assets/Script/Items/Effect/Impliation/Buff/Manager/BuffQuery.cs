using System;
using System.Collections.Generic;

public class BuffQuery
{
    private readonly BuffStorage storage;

    public BuffQuery(BuffStorage storage)
    {
        this.storage = storage;
    }

    #region All Buffs

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        return CreateList(storage?.activeBuffs, false);
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        return CreateList(storage?.activeBuffs, true);
    }

    #endregion

    #region Normal Buffs

    public List<ActiveBuff> GetNormalActiveBuffs()
    {
        return CreateList(storage?.normalBuffs, false);
    }

    public List<ActiveBuff> GetNormalVisibleBuffs()
    {
        return CreateList(storage?.normalBuffs, true);
    }

    #endregion

    #region Infinite Buffs

    public List<ActiveBuff> GetInfiniteActiveBuffs()
    {
        return CreateList(storage?.infiniteBuffs, false);
    }

    public List<ActiveBuff> GetInfiniteVisibleBuffs()
    {
        return CreateList(storage?.infiniteBuffs, true);
    }

    #endregion

    #region Target Queries

    public List<ActiveBuff> GetBagBuffsAsList(
        EquipmentBag bag,
        bool visibleOnly = false
    )
    {
        return GetBuffsByPredicate(
            buff =>
                buff.target != null &&
                buff.target.kind == BuffTargetKind.Bag &&
                buff.target.bag == bag,
            visibleOnly
        );
    }

    public List<ActiveBuff> GetItemBuffsAsList(
        ItemData itemData,
        bool visibleOnly = false
    )
    {
        return GetBuffsByPredicate(
            buff =>
                buff.target != null &&
                buff.target.kind == BuffTargetKind.Item &&
                buff.target.itemData == itemData,
            visibleOnly
        );
    }

    public List<ActiveBuff> GetItemSeriesBuffsAsList(
        ItemSeries series,
        bool visibleOnly = false
    )
    {
        return GetBuffsByPredicate(
            buff =>
                buff.target != null &&
                buff.target.kind == BuffTargetKind.ItemSeries &&
                buff.target.itemSeries == series,
            visibleOnly
        );
    }

    public List<ActiveBuff> GetTargetBuffsAsList(
        IBuffTarget target,
        bool visibleOnly = false
    )
    {
        return GetBuffsByPredicate(
            buff =>
                buff.target != null &&
                buff.target.MatchesTarget(target),
            visibleOnly
        );
    }

    public List<ActiveBuff> GetTargetGroupBuffsAsList(
        string targetGroup,
        bool visibleOnly = false
    )
    {
        return GetBuffsByPredicate(
            buff =>
                buff.target != null &&
                buff.target.kind == BuffTargetKind.Group &&
                buff.target.targetGroup == targetGroup,
            visibleOnly
        );
    }

    #endregion

    private List<ActiveBuff> GetBuffsByPredicate(
        Predicate<ActiveBuff> predicate,
        bool visibleOnly
    )
    {
        List<ActiveBuff> result = new List<ActiveBuff>();

        if (storage == null || predicate == null)
            return result;

        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];

            if (!CanAdd(buff, visibleOnly))
                continue;

            if (predicate(buff))
                result.Add(buff);
        }

        return result;
    }

    private List<ActiveBuff> CreateList(
        List<ActiveBuff> source,
        bool visibleOnly
    )
    {
        List<ActiveBuff> result = new List<ActiveBuff>();
        AddBuffsToList(result, source, visibleOnly);
        return result;
    }

    private void AddBuffsToList(
        List<ActiveBuff> result,
        List<ActiveBuff> source,
        bool visibleOnly
    )
    {
        if (result == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuff buff = source[i];

            if (CanAdd(buff, visibleOnly))
                result.Add(buff);
        }
    }

    private bool CanAdd(
        ActiveBuff buff,
        bool visibleOnly
    )
    {
        if (buff == null || buff.IsExpired)
            return false;

        if (visibleOnly && !buff.showInUI)
            return false;

        return true;
    }
}
