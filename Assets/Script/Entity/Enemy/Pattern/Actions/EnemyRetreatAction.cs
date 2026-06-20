using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRetreatAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Retreat")]
public class EnemyRetreatAction : EnemyPatternAction
{
    [Min(0f)] public float speed = 3f;
    [Min(0f)] public float duration = 0.4f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        float timer = 0f;

        while (timer < duration)
        {
            Vector2 direction = -context.DirectionToTarget;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.left;

            context.MoveBy(direction.normalized * speed * Time.deltaTime, true);
            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }
}
