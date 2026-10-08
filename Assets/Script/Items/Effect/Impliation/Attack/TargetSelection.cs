using System;
using System.Collections.Generic;
using UnityEngine;

public enum TargetSelectionMode { Nearest, Farthest, HighestCurrentHp, Random, StrongFirst }

[Serializable]
public sealed class TargetSelection
{
    public TargetSelectionMode mode;
    [Min(0f)] public float range = 8f;
    [Min(1)] public int count = 1;
    public bool allowDuplicates;
    [Tooltip("Strong First에서 우선하는 EnemyStatData.enemyClass 값. 비우면 최대 HP를 비교합니다.")]
    public string[] preferredEnemyClasses;

    public Enemy Select(List<Enemy> candidates, Vector3 origin,
        HashSet<EnemyLifeKey> excluded = null, EnemyLifeKey? previous = null, float? rangeOverride = null)
    {
        Enemy selected = null;
        float best = float.NegativeInfinity;
        int eligible = 0;
        float radius = EffectStatUtility.Safe(rangeOverride ?? range, 0f, 100000f, 8f);
        foreach (Enemy candidate in candidates)
        {
            if (candidate == null || !candidate.CanReceiveHitEffects) continue;
            EnemyLifeKey key = new EnemyLifeKey(candidate);
            if ((excluded != null && excluded.Contains(key)) || (previous.HasValue && previous.Value.Equals(key))) continue;
            float squared = (candidate.transform.position - origin).sqrMagnitude;
            if (squared > radius * radius) continue;
            if (mode == TargetSelectionMode.Random)
            {
                if (UnityEngine.Random.Range(0, ++eligible) == 0) selected = candidate;
                continue;
            }
            if (mode == TargetSelectionMode.StrongFirst)
            {
                if (selected == null || IsStronger(candidate, selected, origin)) selected = candidate;
                continue;
            }
            float score = Score(candidate, squared);
            if (selected == null || score > best) { selected = candidate; best = score; }
        }
        return selected;
    }

    public void SelectMany(List<Enemy> candidates, Vector3 origin, List<Enemy> results)
    {
        results.Clear();
        HashSet<EnemyLifeKey> excluded = allowDuplicates ? null : new HashSet<EnemyLifeKey>();
        int limit = Mathf.Clamp(count, 1, 128);
        for (int i = 0; i < limit; i++)
        {
            Enemy selected = Select(candidates, origin, excluded);
            if (selected == null) break;
            results.Add(selected);
            if (excluded != null) excluded.Add(new EnemyLifeKey(selected));
        }
    }

    private float Score(Enemy enemy, float squaredDistance)
    {
        if (mode == TargetSelectionMode.Farthest) return squaredDistance;
        if (mode == TargetSelectionMode.HighestCurrentHp)
            return enemy.health != null ? enemy.health.Hp : 0f;
        return -squaredDistance;
    }

    private bool IsPreferred(Enemy enemy)
    {
        if (enemy.statData == null || preferredEnemyClasses == null) return false;
        foreach (string enemyClass in preferredEnemyClasses)
            if (!string.IsNullOrEmpty(enemyClass) && enemy.statData.enemyClass == enemyClass) return true;
        return false;
    }
    private bool IsStronger(Enemy candidate, Enemy selected, Vector3 origin)
    {
        bool preferred = IsPreferred(candidate), selectedPreferred = IsPreferred(selected);
        if (preferred != selectedPreferred) return preferred;
        float maxHp = candidate.health != null ? candidate.health.MaxHp : 0f;
        float selectedMaxHp = selected.health != null ? selected.health.MaxHp : 0f;
        if (maxHp != selectedMaxHp) return maxHp > selectedMaxHp;
        return (candidate.transform.position - origin).sqrMagnitude < (selected.transform.position - origin).sqrMagnitude;
    }
}
