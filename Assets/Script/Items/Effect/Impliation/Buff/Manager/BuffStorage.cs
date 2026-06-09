using System.Collections.Generic;

public class BuffStorage
{
    public readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
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
            return;
        }

        activeBuffs.Add(newBuff);
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

            if (buff.IsSameBuff(sourceItemData, sourceBag, sourceEffectData, target))
                return buff;
        }

        return null;
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
        if (target == null)
            return;

        UnityEngine.Object targetObject = target.BuffTargetObject;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (buff == null || buff.target == null)
                continue;

            if (buff.target.kind != BuffTargetKind.Target)
                continue;

            if (buff.target.targetObject == targetObject)
                activeBuffs.RemoveAt(i);
        }
    }

    public void ClearAll()
    {
        activeBuffs.Clear();
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
