using System;

[Serializable]
public sealed class ChainAttackStat : IGameStat<ChainAttackStat>, IEffectScalableStat
{
    public float chainFirstDamage = 8f;
    public float chainNextDamage = 6f;
    public float chainDamageChangePerJump;
    public float chainRange = 6f;
    public float chainMaxHits = 4f;
    public float chainJumpInterval = 0.1f;
    public ChainAttackStat Clone() => (ChainAttackStat)MemberwiseClone();
    public void Clamp()
    {
        chainFirstDamage = EffectStatUtility.Safe(chainFirstDamage, 0f, 1000000f, 8f);
        chainNextDamage = EffectStatUtility.Safe(chainNextDamage, 0f, 1000000f, 6f);
        chainDamageChangePerJump = EffectStatUtility.Safe(chainDamageChangePerJump, -1000000f, 1000000f, 0f);
        chainRange = EffectStatUtility.Safe(chainRange, 0f, 100000f, 6f);
        chainMaxHits = EffectStatUtility.Safe(chainMaxHits, 1f, 128f, 4f);
        chainJumpInterval = EffectStatUtility.Safe(chainJumpInterval, 0f, 3600f, 0.1f);
    }
    public float DamageAt(int hitIndex) => MathfSafeDamage(hitIndex == 0 ? chainFirstDamage :
        chainNextDamage + chainDamageChangePerJump * (hitIndex - 1));
    private static float MathfSafeDamage(float damage) => EffectStatUtility.Safe(damage, 0f, 1000000f, 0f);
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        chainFirstDamage = scale.ScaleDamage(chainFirstDamage);
        chainNextDamage = scale.ScaleDamage(chainNextDamage);
        chainDamageChangePerJump *= scale.Damage;
        chainRange *= scale.Range;
    }
}
