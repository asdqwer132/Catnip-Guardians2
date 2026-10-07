using UnityEngine;

[CreateAssetMenu(fileName = "StunHitEffect", menuName = "GameData/Items/Hit Effects/Stun")]
public class StunHitEffectData : HitEffectData
{
    [Header("Stun")]
    [Min(0.01f)] public float duration = 1f;

    protected override bool ApplyEffect(HitEffectContext context)
    {
        return context.target.GetOrCreateStatusController().ApplyStun(duration, context);
    }
}
