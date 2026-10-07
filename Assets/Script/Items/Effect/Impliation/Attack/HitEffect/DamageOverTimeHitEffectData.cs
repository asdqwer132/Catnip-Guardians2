using UnityEngine;

[CreateAssetMenu(fileName = "DamageOverTimeHitEffect", menuName = "GameData/Items/Hit Effects/Damage Over Time")]
public class DamageOverTimeHitEffectData : HitEffectData
{
    [Header("Damage Over Time")]
    [Min(0f)] public float damagePerTick = 5f;
    [Min(0.01f)] public float duration = 3f;
    [Min(0.01f)] public float tickInterval = 1f;

    protected override bool OwnsEndVisual => false;
    protected override bool ApplyEffect(HitEffectContext context)
    {
        
        return context.target.GetOrCreateStatusController().ApplyDamageOverTime(
            this, damagePerTick, duration, tickInterval, context
        );
    }
    public void PlayHit(HitEffectContext context)
    {
        PlayHitVisual( context );
    }
}
