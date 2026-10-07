using UnityEngine;

public static class ItemEffectUtility
{
    public static ItemEffectData[] Copy(ItemEffectData[] effects)
        => effects != null ? (ItemEffectData[])effects.Clone() : null;

    public static void Prepare(ItemEffectContext context, ItemEffectData[] effects)
    {
        if (context == null || effects == null) return;
        foreach (ItemEffectData effect in effects)
            if (effect != null && effect.CanExecute(context)) context.plan.Prepare(effect, context);
    }

    public static void Execute(ItemEffectData[] effects, ItemEffectContext context)
    {
        if (effects == null || context == null) return;
        foreach (ItemEffectData effect in effects)
        {
            if (!context.CanContinue) break;
            if (effect != null) effect.Execute(context);
        }
    }
}
