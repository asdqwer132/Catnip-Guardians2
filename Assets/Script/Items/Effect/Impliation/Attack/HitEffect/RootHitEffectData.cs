using UnityEngine;

[CreateAssetMenu(fileName = "RootHitEffect", menuName = "GameData/Items/Hit Effects/Root (Movement Only)")]
public sealed class RootHitEffectData : HitEffectData
{
    [Min(0.01f)] public float duration = 1f;
    protected override bool OwnsEndVisual => false;
    protected override bool ApplyEffect(HitEffectContext context) =>
        context.target.GetOrCreateStatusController().ApplyRoot(duration * context.CreateItemContext().durationMultiplier, context, endVisualData);
}
