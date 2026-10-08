using UnityEngine;

[CreateAssetMenu(fileName = "ChainAttackEffect", menuName = "GameData/Items/Effects/Attack/Chain Attack")]
public sealed class ChainAttackEffect : ItemEffectData
{
    public ChainAttackStat attackStat = new ChainAttackStat();
    public TargetSelection firstTarget = new TargetSelection();
    public TargetSelection nextTarget = new TargetSelection();
    public LayerMask enemyLayerMask = ~0;
    public bool allowTargetRehit;
    public bool allowImmediateSameTarget;
    public bool retargetWhenTargetLost = true;
    public HitEffectData[] onHitEffects;
    public HitEffectApplyMode hitEffectApplyMode = HitEffectApplyMode.OncePerTarget;
    public EffectVisualData jumpVisualData;
    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, attackStat);
        HitEffectPreparation.Prepare(onHitEffects, context);
    }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || attackStat == null) return;
        GameObject host = new GameObject("Chain Attack");
        host.transform.position = context.targetPosition;
        host.AddComponent<ChainAttackRuntime>().Init(this, context);
    }
    protected override float GetImpactRadius(ItemEffectContext context) => firstTarget != null ? firstTarget.range : 1f;
}
