using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyZigzagMoveAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Zigzag Move")]
public class EnemyZigzagMoveAction : EnemyPatternAction
{
    [Min(0f)] public float speed = 3f;
    [Min(0f)] public float duration = 1f;
    [Min(0f)] public float amplitude = 0.8f;
    [Min(0f)] public float frequency = 8f;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        float timer = 0f;

        while (timer < duration)
        {
            Vector2 forward = context.DirectionToTarget;
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector2.right;

            Vector2 side = new Vector2(-forward.y, forward.x);
            float sidePower = Mathf.Sin(timer * frequency) * amplitude;
            Vector2 direction = (forward + side * sidePower).normalized;

            context.MoveBy(direction * speed * Time.deltaTime, true);

            timer += Time.deltaTime;
            yield return null;
        }

        context.StopMove();
    }
}
