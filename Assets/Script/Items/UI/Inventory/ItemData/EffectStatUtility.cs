public static class EffectStatUtility
{
    public static EffectStat CloneStat(EffectStat source)
    {
        EffectStat result = new EffectStat();

        if (source == null)
            return result;

        result.attackPower = source.attackPower;
        result.healPower = source.healPower;
        result.debuffPower = source.debuffPower;

        result.effectRadius = source.effectRadius;
        result.effectCount = source.effectCount;

        result.defensePower = source.defensePower;
        result.speedPower = source.speedPower;

        result.attackMultiplier = source.attackMultiplier;
        result.healMultiplier = source.healMultiplier;
        result.debuffMultiplier = source.debuffMultiplier;
        result.defenseMultiplier = source.defenseMultiplier;
        result.speedMultiplier = source.speedMultiplier;

        return result;
    }
}