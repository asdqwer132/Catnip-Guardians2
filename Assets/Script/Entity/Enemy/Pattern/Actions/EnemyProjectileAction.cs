using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyProjectileAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Projectile")]
public class EnemyProjectileAction : EnemyPatternAction
{
    public EnemySimpleProjectile projectilePrefab;
    public EnemyPatternPointType spawnPointType = EnemyPatternPointType.Self;

    [Header("Projectile")]
    [Min(0.01f)] public float speed = 6f;
    [Min(0.05f)] public float lifeTime = 3f;
    public float damage = 0f;
    public float damageMultiplier = 1f;
    public LayerMask targetLayerMask = ~0;

    [Header("Burst")]
    [Min(1)] public int count = 1;
    [Min(0f)] public float interval = 0.15f;
    [Min(0f)] public float spreadAngle = 0f;

    [Header("Spawn Offset")]
    [Min(0f)] public float spawnDistance = 0.2f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (projectilePrefab == null)
            yield break;

        Vector2 baseDirection = context.DirectionToTarget;
        if (baseDirection.sqrMagnitude <= 0.0001f)
            baseDirection = Vector2.right;

        int projectileCount = Mathf.Max(1, count);

        for (int i = 0; i < projectileCount; i++)
        {
            Vector2 direction = GetSpreadDirection(baseDirection, i, projectileCount);
            Vector3 spawnPosition = context.ResolvePoint(spawnPointType, spawnDistance, 0f);

            EnemySimpleProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            float finalDamage = damage > 0f ? damage : context.GetAttackDamage() * damageMultiplier;

            projectile.Init(
                context.Enemy != null ? context.Enemy.gameObject : null,
                direction,
                finalDamage,
                speed,
                lifeTime,
                targetLayerMask
            );

            if (interval > 0f && i < projectileCount - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    private Vector2 GetSpreadDirection(Vector2 baseDirection, int index, int totalCount)
    {
        if (spreadAngle <= 0f || totalCount <= 1)
            return baseDirection.normalized;

        float startAngle = -spreadAngle * 0.5f;
        float t = totalCount <= 1 ? 0.5f : (float)index / (totalCount - 1);
        float angle = startAngle + spreadAngle * t;

        return Quaternion.Euler(0f, 0f, angle) * baseDirection.normalized;
    }
}
