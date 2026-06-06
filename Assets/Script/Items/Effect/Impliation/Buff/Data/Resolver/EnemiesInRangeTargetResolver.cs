using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Enemies In Range", menuName = "Game/Buff/Target/Enemies In Range")]
public class EnemiesInRangeTargetResolver : BuffTargetResolver
{
    [Min(0f)] public float radius = 1f;
    public LayerMask enemyLayer;
    public bool affectDeadEnemies;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(context.targetPosition, radius, enemyLayer);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            Enemy enemy = colliders[i].GetComponentInParent<Enemy>();
            if (enemy == null)
                continue;

            if (!affectDeadEnemies && enemy.IsDead)
                continue;

            results.Add(BuffTargetHandle.Enemy(enemy));
        }
    }
}
