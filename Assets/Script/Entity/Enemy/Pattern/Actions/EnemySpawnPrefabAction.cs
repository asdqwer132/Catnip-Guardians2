using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnPrefabAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Spawn Prefab")]
public class EnemySpawnPrefabAction : EnemyPatternAction
{
    public GameObject spawnPrefab;
    public EnemyPatternPointType pointType = EnemyPatternPointType.Self;

    [Header("Spawn")]
    [Min(1)] public int count = 1;
    [Min(0f)] public float interval = 0.05f;
    [Min(0f)] public float distance = 0f;
    [Min(0f)] public float spreadRadius = 0.5f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (spawnPrefab == null)
            yield break;

        int spawnCount = Mathf.Max(1, count);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 position = context.ResolvePoint(pointType, distance, spreadRadius);
            position += (Vector3)(Random.insideUnitCircle * spreadRadius);
            Instantiate(spawnPrefab, position, Quaternion.identity);

            if (interval > 0f && i < spawnCount - 1)
                yield return new WaitForSeconds(interval);
        }
    }
}
