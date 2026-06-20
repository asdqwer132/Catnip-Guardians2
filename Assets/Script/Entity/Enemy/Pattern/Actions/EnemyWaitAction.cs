using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaitAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Wait")]
public class EnemyWaitAction : EnemyPatternAction
{
    [Min(0f)] public float duration = 0.3f;
    public bool stopMove = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (stopMove)
            context.StopMove();

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
    }
}
