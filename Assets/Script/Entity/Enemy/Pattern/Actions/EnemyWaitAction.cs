using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaitAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Wait")]
public class EnemyWaitAction : EnemyPatternAction
{
    [Header("Wait")]
    [Min(0f)] public float duration = 0.3f;


    [Header("Stop Option")]
    public bool stopMove = true;
    public bool stopAttack = false;
    public bool forceIdle = true;

    [Header("During Wait")]
    public bool tickDefaultAttack = false;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null)
            yield break;

        ActorMover mover = context.Mover;
        ActorAttack attack = context.Attack;
        ActorVisual visual = context.Visual;

        bool previousMoveStopped = mover != null && mover.IsMoveStopped;
        bool previousAttackStopped = attack != null && attack.IsAttackStopped;

        if (stopMove && mover != null)
            mover.SetMoveStopped(true);

        if (stopAttack && attack != null)
            attack.SetAttackStopped(true);

        if (forceIdle && visual != null)
        {
            Vector2 lookDirection = mover != null ? mover.LastMoveDirection : Vector2.zero;
            visual.ForceIdle(lookDirection, true, false);
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (context.Enemy == null)
                break;

            if (context.Enemy.IsDead)
                break;

            if (context.Enemy.IsFullyStopped)
                break;

            if (tickDefaultAttack && !stopAttack)
                context.TickDefaultAttack();

            timer += Time.deltaTime;
            yield return null;
        }

        if (context.Enemy != null && !context.Enemy.IsFullyStopped)
        {
            if (mover != null)
                mover.SetMoveStopped(previousMoveStopped);

            if (attack != null)
                attack.SetAttackStopped(previousAttackStopped);
        }
    }
}