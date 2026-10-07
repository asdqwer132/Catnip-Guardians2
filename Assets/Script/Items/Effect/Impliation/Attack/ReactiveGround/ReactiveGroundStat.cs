using System;

[Serializable]
public sealed class ReactiveGroundStat : IGameStat<ReactiveGroundStat>
{
    public float groundRadius = 2f;
    public float groundLifetime = 5f;
    public float groundTickInterval = 0.5f;
    public float groundReactionCooldown = 0.1f;
    public float groundSpecialDuration;
    public ReactiveGroundStat Clone() => (ReactiveGroundStat)MemberwiseClone();
    public void Clamp()
    {
        groundRadius = EffectStatUtility.Safe(groundRadius, 0.01f, 100f, 2f);
        groundLifetime = EffectStatUtility.Safe(groundLifetime, 0.01f, 600f, 5f);
        groundTickInterval = EffectStatUtility.Safe(groundTickInterval, 0.01f, 60f, 0.5f);
        groundReactionCooldown = EffectStatUtility.Safe(groundReactionCooldown, 0f, 60f, 0.1f);
        groundSpecialDuration = EffectStatUtility.Safe(groundSpecialDuration, 0f, 600f, 0f);
    }
}
