using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTelegraphAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Telegraph")]
public class EnemyTelegraphAction : EnemyPatternAction
{
    public GameObject telegraphPrefab;
    public EnemyPatternPointType pointType = EnemyPatternPointType.Self;
    [Min(0f)] public float duration = 0.5f;
    [Min(0f)] public float distance = 1f;
    [Min(0f)] public float randomRadius = 1f;
    public bool parentToEnemy = false;
    public bool faceTarget = true;
    public bool stopMove = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (stopMove)
            context.StopMove();

        if (faceTarget)
            context.LookDirection(context.DirectionToTarget);

        Vector3 position = context.ResolvePoint(pointType, distance, randomRadius);
        Transform parent = parentToEnemy && context.Enemy != null ? context.Enemy.transform : null;

        GameObject instance = null;
        if (telegraphPrefab != null)
            instance = Instantiate(telegraphPrefab, position, Quaternion.identity, parent);

        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        if (instance != null)
            Destroy(instance);
    }
}
