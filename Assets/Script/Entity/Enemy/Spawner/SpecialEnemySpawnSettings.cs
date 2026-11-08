using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialEnemySpawnSelection { IndependentIntervals, AllTogether, WeightedOne }
public enum SpecialEnemySpawnConditionType { AliveEnemyCountAtLeast, AliveEnemyCountAtMost, KillsSinceLastSpawnAtLeast }
public enum SpecialEnemySpawnConditionMatch { All, Any }

[Serializable]
public sealed class SpecialEnemySpawnCondition
{
    public SpecialEnemySpawnConditionType type;
    [Min(0)] public int amount = 1;

    public bool IsSatisfied(int alive, int kills, bool hasEnemySource, bool hasKillSource)
    {
        int threshold = Mathf.Max(0, amount);
        switch (type)
        {
            case SpecialEnemySpawnConditionType.AliveEnemyCountAtLeast: return hasEnemySource && alive >= threshold;
            case SpecialEnemySpawnConditionType.AliveEnemyCountAtMost: return hasEnemySource && alive <= threshold;
            case SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast: return hasKillSource && kills >= threshold;
            default: return false;
        }
    }
}

[Serializable]
public sealed class SpecialEnemySpawnSettings
{
    [Tooltip("Independent Intervals preserves each entry's Spawn Rate. All Together and Weighted One use the shared interval below.")]
    public SpecialEnemySpawnSelection selection;
    [Min(.05f)] public float spawnInterval = 1.5f;
    public SpecialEnemySpawnConditionMatch conditionMatch;
    [Tooltip("Cooldown and these conditions must both pass. Kill progress resets only after this spawner successfully summons.")]
    public List<SpecialEnemySpawnCondition> conditions = new List<SpecialEnemySpawnCondition>();

    public bool ConditionsMet(int alive, int kills, bool hasEnemySource, bool hasKillSource)
    {
        bool found = false;
        if (conditions != null)
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                found = true;
                bool pass = condition.IsSatisfied(alive, kills, hasEnemySource, hasKillSource);
                if (conditionMatch == SpecialEnemySpawnConditionMatch.All && !pass) return false;
                if (conditionMatch == SpecialEnemySpawnConditionMatch.Any && pass) return true;
            }
        return !found || conditionMatch == SpecialEnemySpawnConditionMatch.All;
    }
}
