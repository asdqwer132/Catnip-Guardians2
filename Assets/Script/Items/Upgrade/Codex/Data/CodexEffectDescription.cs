using System.Collections.Generic;
using UnityEngine;

// Display metadata shared by item and enemy codices. Reading it never executes effects.
[CreateAssetMenu(fileName = "Codex Effect Description", menuName = "03.Upgrade/Codex/Effect Description")]
public sealed class CodexEffectDescription : DefaultData { }

public static class CodexEffectUtility
{
    public static IReadOnlyList<CodexEffectDescription> GetDescriptions(DefaultData entry)
    {
        var result = new List<CodexEffectDescription>();
        if (entry is ItemData item)
        {
            Add(result, item.codexEffects);
            if (item.effectDatas != null)
                foreach (var effect in item.effectDatas)
                    if (effect != null && effect.codexDescription != null && !result.Contains(effect.codexDescription))
                        result.Add(effect.codexDescription);
        }
        else if (entry is EnemyDataSet enemy) Add(result, enemy.codexEffects);
        return result;
    }

    private static void Add(List<CodexEffectDescription> result, CodexEffectDescription[] values)
    {
        if (values == null) return;
        foreach (var value in values)
            if (value != null && !result.Contains(value)) result.Add(value);
    }
}
