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
        if (context == null || !context.HasEnemy) yield break;
        if (stopMove) context.StopMove();
        if (faceTarget) context.FaceDirection(context.DirectionToTarget);
        Vector3 position = context.ResolvePoint(pointType, distance, randomRadius);
        Transform parent = parentToEnemy ? context.Enemy.transform : null;
        GameObject instance = null;
        try
        {
            if (telegraphPrefab != null)
                instance = Instantiate(telegraphPrefab, position, Quaternion.identity, parent);
            yield return context.WaitSeconds(duration);
        }
        finally
        {
            // 중단되거나 풀로 반환되어도 경고 영역이 남지 않는다.
            if (instance != null) Destroy(instance);
        }
    }
}
