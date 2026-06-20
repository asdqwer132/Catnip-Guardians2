using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAreaDamageAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Area Damage")]
public class EnemyAreaDamageAction : EnemyPatternAction
{
    public EnemyPatternPointType pointType = EnemyPatternPointType.Target;
    public GameObject effectPrefab;

    [Header("Area")]
    [Min(0f)] public float delay = 0f;
    [Min(0.01f)] public float radius = 1f;
    public float damage = 0f;
    public float damageMultiplier = 1f;
    public LayerMask targetLayerMask = ~0;

    [Header("Multi")]
    [Min(1)] public int count = 1;
    [Min(0f)] public float interval = 0.1f;
    [Min(0f)] public float distance = 1f;
    [Min(0f)] public float randomRadius = 1.5f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        int areaCount = Mathf.Max(1, count);

        for (int i = 0; i < areaCount; i++)
        {
            Vector3 position = context.ResolvePoint(pointType, distance, randomRadius);
            SpawnEffect(position);
            ApplyDamage(context, position);

            if (interval > 0f && i < areaCount - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnEffect(Vector3 position)
    {
        if (effectPrefab == null)
            return;

        Instantiate(effectPrefab, position, Quaternion.identity);
    }

    private void ApplyDamage(EnemyPatternContext context, Vector3 position)
    {
        float finalDamage = damage > 0f ? damage : context.GetAttackDamage() * damageMultiplier;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, targetLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead)
                continue;

            damageable.TakeDamage(finalDamage);
        }
    }
}
