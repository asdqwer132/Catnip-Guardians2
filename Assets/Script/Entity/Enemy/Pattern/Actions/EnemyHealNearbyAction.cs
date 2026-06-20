using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyHealNearbyAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Heal Nearby")]
public class EnemyHealNearbyAction : EnemyPatternAction
{
    [Min(0f)] public float radius = 2f;
    [Min(0f)] public float healAmount = 5f;
    public LayerMask targetLayerMask = ~0;
    public bool includeSelf = true;
    public GameObject effectPrefab;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (effectPrefab != null)
            Instantiate(effectPrefab, context.Position, Quaternion.identity);

        if (includeSelf && context.Enemy != null)
            context.Enemy.Heal(healAmount);

        Collider2D[] hits = Physics2D.OverlapCircleAll(context.Position, radius, targetLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsDead)
                continue;

            if (!includeSelf && context.Enemy != null && enemy == context.Enemy)
                continue;

            enemy.Heal(healAmount);
        }

        yield return null;
    }
}
