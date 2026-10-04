using UnityEngine;

[CreateAssetMenu(fileName = "DamageOverTimeHitEffect", menuName = "GameData/Item/Hit Effect/Damage Over Time")]
public class DamageOverTimeHitEffectData : HitEffectData
{
    [Header("Damage Over Time")]
    [Min(0f)] public float damagePerTick = 5f;
    [Min(0.01f)] public float duration = 3f;
    [Min(0.01f)] public float tickInterval = 1f;

    private HitEffectContext context;
    protected override bool ApplyEffect(HitEffectContext context)
    {
        this.context = context;
        return context.target.GetOrCreateStatusController().ApplyDamageOverTime(
            this, damagePerTick, duration, tickInterval
        );
    }
    public void PlayHit()
    {
        PlayHitVisual( context );
    }
}
