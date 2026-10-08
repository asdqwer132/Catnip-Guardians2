using System;

[Serializable]
public sealed class BagCooldownStat : IGameStat<BagCooldownStat>
{
    public float cooldown;
    public float cooldownRecoveryRate = 1f;

    public BagCooldownStat Clone() => (BagCooldownStat)MemberwiseClone();

    public void Clamp()
    {
        cooldown = EffectStatUtility.Safe(cooldown, 0f, 1000000f, 0f);
        cooldownRecoveryRate = EffectStatUtility.Safe(cooldownRecoveryRate, 0f, 100f, 1f);
    }

    public static BagCooldownStat Resolve(float baseCooldown, EquipmentBag bag, BuffManager manager)
    {
        var stat = new BagCooldownStat { cooldown = baseCooldown };
        stat.Clamp();
        if (manager != null && bag != null)
            stat = manager.GetBuffedStat(stat, BuffQueryContext.ForBag(bag), BuffCalculationMode.All, false) ?? stat;
        stat.Clamp();
        return stat;
    }
}
