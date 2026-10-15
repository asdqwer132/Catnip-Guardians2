using System;
using System.Collections.Generic;

[Serializable]
public sealed class MergeContainerUpgradeLedger
{
    public List<MergeContainerUpgradeReward> purchases = new List<MergeContainerUpgradeReward>();
    public bool Has(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return false;
        foreach (var reward in purchases) if (reward != null && reward.skillId == skillId) return true;
        return false;
    }
    public bool Add(MergeContainerUpgradeReward reward)
    {
        if (reward == null || string.IsNullOrEmpty(reward.skillId) || Has(reward.skillId)) return false;
        purchases.Add(reward); return true;
    }
    public void Remove(string skillId) => purchases.RemoveAll(r => r == null || r.skillId == skillId);
    public HashSet<string> InMap(string mapId)
    {
        var result = new HashSet<string>();
        foreach (var r in purchases) if (r != null && r.skillMapId == mapId) result.Add(r.skillId);
        return result;
    }
    public void ClearMap(string mapId) => purchases.RemoveAll(r => r == null || r.skillMapId == mapId);
    public MergeContainerStats Resolve(MergeContainerDefinition definition)
    {
        var result = definition != null && definition.baseStats != null ? definition.baseStats.Clone() : new MergeContainerStats();
        foreach (var reward in purchases)
        {
            if (reward == null || (reward.container != null && reward.container != definition)) continue;
            float amount = EffectStatUtility.Safe(reward.amount, 0f, 100f, 0f);
            switch (reward.upgrade)
            {
                case MergeContainerUpgradeKind.ContainerSize: result.width *= 1f + amount; result.height *= 1f + amount; break;
                case MergeContainerUpgradeKind.InjectionSpeed: result.injectionInterval /= 1f + amount; break;
                case MergeContainerUpgradeKind.InletCount: result.inletCount += (int)amount; break;
                case MergeContainerUpgradeKind.DragRadius: result.dragRadius += amount; break;
                case MergeContainerUpgradeKind.ClickCooldown: result.clickCooldown *= System.Math.Max(0f, 1f - amount); break;
                case MergeContainerUpgradeKind.DragCooldown: result.dragCooldown *= System.Math.Max(0f, 1f - amount); break;
            }
        }
        result.Clamp(); return result;
    }
}
