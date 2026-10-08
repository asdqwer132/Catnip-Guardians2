using UnityEngine;

public enum ProjectileAttackPath { Straight, Return, Orbit, Homing }
public enum ProjectileRehitPolicy { OncePerLife, OncePerPathCycle, Interval }
public enum ProjectileOrbitCenter { EffectPosition, OwnerPosition }

[CreateAssetMenu(fileName = "ProjectileAttackEffect", menuName = "GameData/Items/Effects/Attack/Projectile Attack")]
public sealed class ProjectileAttackEffect : ItemEffectData
{
    public ProjectileAttackStat attackStat = new ProjectileAttackStat();
    public ProjectileAttackPath path;
    public ProjectileRehitPolicy rehitPolicy = ProjectileRehitPolicy.OncePerPathCycle;
    public LayerMask enemyLayerMask = ~0;
    public GameObject projectileVisualPrefab;
    public Sprite projectileSprite;
    public TargetSelection targetSelection = new TargetSelection();
    public bool retargetWhenTargetLost = true;
    [Min(0f)] public float homingTurnSpeed = 360f;
    public bool returnToOwner;
    public ProjectileOrbitCenter orbitCenter;
    public bool clockwise;
    public float orbitStartAngle;
    public HitEffectData[] onHitEffects;
    public HitEffectApplyMode hitEffectApplyMode = HitEffectApplyMode.OncePerTarget;
    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, attackStat);
        HitEffectPreparation.Prepare(onHitEffects, context);
    }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || attackStat == null) return;
        GameObject host = new GameObject("Projectile Attack");
        host.transform.position = context.targetPosition;
        if (projectileVisualPrefab != null)
            Instantiate(projectileVisualPrefab, host.transform);
        host.AddComponent<ProjectileAttackRuntime>().Init(this, context);
    }
    protected override float GetImpactRadius(ItemEffectContext context)
        => context.GetCurrentStat(this, attackStat)?.projectileHitRadius ?? 1f;
}
