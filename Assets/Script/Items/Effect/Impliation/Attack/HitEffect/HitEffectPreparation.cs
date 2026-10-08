public static class HitEffectPreparation
{
    public static void Prepare(HitEffectData[] effects, ItemEffectContext context)
    {
        if (effects == null || context == null) return;
        foreach (HitEffectData effect in effects)
        {
            BuffHitEffectData buff = effect as BuffHitEffectData;
            if (buff != null && buff.buffEffect != null) context.plan.Prepare(buff.buffEffect, context);
            ExecuteEffectOnHitData followup = effect as ExecuteEffectOnHitData;
            if (followup != null && followup.effects != null)
                foreach (ItemEffectData child in followup.effects) context.plan.Prepare(child, context);
        }
    }
}
