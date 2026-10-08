using UnityEngine;

public enum EnemyTargetOverrideMode
{
    Owner,
    NearestOtherEnemy,
    RandomOtherEnemy
}

[CreateAssetMenu(fileName = "TargetOverrideHitEffect", menuName = "GameData/Items/Hit Effects/Target Override")]
public sealed class TargetOverrideHitEffectData : HitEffectData
{
    [Tooltip("미끼는 Owner에 공격 가능한 미끼 소환물을 전달합니다. 혼란의 적끼리 공격 여부는 데이터에서 선택합니다.")]
    public EnemyTargetOverrideMode mode;
    [Min(0.01f)] public float duration = 2f;
    public int priority = 10;
    [Min(0.01f)] public float selectionRadius = 10f;
    public LayerMask enemyLayerMask = ~0;
    protected override bool OwnsEndVisual => false;

    protected override bool ApplyEffect(HitEffectContext context)
    {
        IDamageable selected = null;
        if (mode == EnemyTargetOverrideMode.Owner)
            selected = context.owner != null ? context.owner.GetComponentInParent<IDamageable>() : null;
        else
        {
            // 사용 때 한 번 조회한다. 에셋에는 대상/상태를 보관하지 않는다.
            EnemyQueryBuffer query = new EnemyQueryBuffer();
            query.Scan(context.hitPosition, selectionRadius, enemyLayerMask);
            query.Enemies.Remove(context.target);
            selected = EnemyQueryBuffer.Select(query.Enemies, context.hitPosition, mode == EnemyTargetOverrideMode.RandomOtherEnemy);
        }
        if (selected == null || ReferenceEquals(selected, context.target)) return false;
        return context.target.GetOrCreateStatusController().ApplyTargetOverride(selected, duration * context.CreateItemContext().durationMultiplier, priority, context, endVisualData);
    }
}
