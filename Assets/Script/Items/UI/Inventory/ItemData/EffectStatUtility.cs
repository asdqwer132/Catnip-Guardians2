public static class EffectStatUtility
{
    public static EffectStat CloneStat(EffectStat source)
    {
        if (source == null)
            return new EffectStat();

        if (source is AttackStat attackStat)
            return CloneAttackStat(attackStat);

        if (source is BuffStat buffStat)
            return CloneBuffStat(buffStat);

        return CloneEffectStat(source);
    }

    public static EffectStat CloneEffectStat(EffectStat source)
    {
        EffectStat result = new EffectStat();

        if (source == null)
            return result;

        result.effectRadius = source.effectRadius;
        result.effectCount = source.effectCount;
        result.effectLifeTime = source.effectLifeTime;
        result.effectDuration = source.effectDuration;

        return result;
    }

    public static AttackStat CloneAttackStat(AttackStat source)
    {
        AttackStat result = new AttackStat();

        if (source == null)
            return result;

        result.effectRadius = source.effectRadius;
        result.effectCount = source.effectCount;
        result.effectLifeTime = source.effectLifeTime;
        result.effectDuration = source.effectDuration;

        result.attackPower = source.attackPower;
        result.attackMultiplier = source.attackMultiplier;
        result.damageApplyMode = source.damageApplyMode;
        result.damageInterval = source.damageInterval;

        return result;
    }

    public static BuffStat CloneBuffStat(BuffStat source)
    {
        BuffStat result = new BuffStat();

        if (source == null)
            return result;

        result.effectRadius = source.effectRadius;
        result.effectCount = source.effectCount;
        result.effectLifeTime = source.effectLifeTime;
        result.effectDuration = source.effectDuration;

        if (source.attackBuff != null)
            result.attackBuff = source.attackBuff.Clone();

        if (source.cooldownBuff != null)
            result.cooldownBuff = source.cooldownBuff.Clone();

        if (source.buffInfoBuff != null)
            result.buffInfoBuff = source.buffInfoBuff.Clone();


        return result;
    }
}