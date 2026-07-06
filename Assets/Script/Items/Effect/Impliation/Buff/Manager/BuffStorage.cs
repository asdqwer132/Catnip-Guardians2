using System.Collections.Generic;

public class BuffStorage
{
    // 전체 버프 = 일반 버프 + 무한 버프
    // 기존 코드 호환성을 위해 activeBuffs는 전체 목록으로 유지합니다.
    public readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    // 시간제 또는 사용 횟수 제한 버프
    public readonly List<ActiveBuff> normalBuffs = new List<ActiveBuff>();

    // BuffUseLimitType.Infinite 버프
    public readonly List<ActiveBuff> infiniteBuffs = new List<ActiveBuff>();

    public readonly List<IBuffTarget> registeredTargets = new List<IBuffTarget>();

    public void AddOrRefresh(ActiveBuff newBuff, BuffInfo info)
    {
        if (newBuff == null)
            return;

        ActiveBuff same = FindSameBuff(
            newBuff.sourceItemData,
            newBuff.sourceBag,
            newBuff.sourceEffectData,
            newBuff.target
        );

        if (same != null)
        {
            same.modifiers = newBuff.modifiers;
            same.includeSelf = newBuff.includeSelf;
            same.showInUI = newBuff.showInUI;
            same.RegisterAgain(info);

            SyncBuffCategory(same);
            return;
        }

        AddNewBuff(newBuff);
    }

    private void AddNewBuff(ActiveBuff buff)
    {
        if (buff == null)
            return;

        if (!activeBuffs.Contains(buff))
            activeBuffs.Add(buff);

        SyncBuffCategory(buff);
    }

    private void SyncBuffCategory(ActiveBuff buff)
    {
        if (buff == null)
            return;

        normalBuffs.Remove(buff);
        infiniteBuffs.Remove(buff);

        if (buff.IsInfinite)
            infiniteBuffs.Add(buff);
        else
            normalBuffs.Add(buff);
    }

    public ActiveBuff FindSameBuff(
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData,
        BuffTargetHandle target
    )
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuff buff = activeBuffs[i];

            if (buff == null || buff.IsExpired)
                continue;

            if (buff.IsSameBuff(
                sourceItemData,
                sourceBag,
                sourceEffectData,
                target))
            {
                return buff;
            }
        }

        return null;
    }

    public void RemoveBuff(ActiveBuff buff)
    {
        if (buff == null)
            return;

        activeBuffs.Remove(buff);
        normalBuffs.Remove(buff);
        infiniteBuffs.Remove(buff);
    }

    public void RegisterTarget(IBuffTarget target)
    {
        if (target == null)
            return;

        if (!registeredTargets.Contains(target))
            registeredTargets.Add(target);
    }

    public void UnregisterTarget(IBuffTarget target)
    {
        if (target == null)
            return;

        registeredTargets.Remove(target);
        RemoveBuffsForTarget(target);
    }

    public void RemoveBuffsForTarget(IBuffTarget target)
    {
        RemoveBuffsForTargetInternal(
            target,
            removeNormalBuffs: true,
            removeInfiniteBuffs: true
        );
    }

    public void RemoveNormalBuffsForTarget(IBuffTarget target)
    {
        RemoveBuffsForTargetInternal(
            target,
            removeNormalBuffs: true,
            removeInfiniteBuffs: false
        );
    }

    public void RemoveInfiniteBuffsForTarget(IBuffTarget target)
    {
        RemoveBuffsForTargetInternal(
            target,
            removeNormalBuffs: false,
            removeInfiniteBuffs: true
        );
    }

    private void RemoveBuffsForTargetInternal(
        IBuffTarget target,
        bool removeNormalBuffs,
        bool removeInfiniteBuffs
    )
    {
        if (target == null)
            return;

        UnityEngine.Object targetObject = target.BuffTargetObject;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (buff == null)
            {
                activeBuffs.RemoveAt(i);
                normalBuffs.Remove(null);
                infiniteBuffs.Remove(null);
                continue;
            }

            if (buff.target == null)
                continue;

            if (buff.target.kind != BuffTargetKind.Target)
                continue;

            if (buff.target.targetObject != targetObject)
                continue;

            if (buff.IsInfinite)
            {
                if (!removeInfiniteBuffs)
                    continue;
            }
            else
            {
                if (!removeNormalBuffs)
                    continue;
            }

            RemoveBuff(buff);
        }
    }

    public void ClearNormalBuffs()
    {
        for (int i = normalBuffs.Count - 1; i >= 0; i--)
            activeBuffs.Remove(normalBuffs[i]);

        normalBuffs.Clear();
    }

    public void ClearInfiniteBuffs()
    {
        for (int i = infiniteBuffs.Count - 1; i >= 0; i--)
            activeBuffs.Remove(infiniteBuffs[i]);

        infiniteBuffs.Clear();
    }

    public void ClearAll()
    {
        activeBuffs.Clear();
        normalBuffs.Clear();
        infiniteBuffs.Clear();
    }

    public void RemoveNullRegisters()
    {
        for (int i = registeredTargets.Count - 1; i >= 0; i--)
        {
            IBuffTarget target = registeredTargets[i];

            if (target == null || target.BuffTargetObject == null)
                registeredTargets.RemoveAt(i);
        }
    }
}
