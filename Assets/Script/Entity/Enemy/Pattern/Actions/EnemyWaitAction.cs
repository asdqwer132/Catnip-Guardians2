using System.Collections;
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

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null) yield break;
        ActorMover mover = context.Mover;
        ActorAttack attack = context.Attack;
        bool previousMoveStopped = mover != null && mover.IsMoveStopped;
        bool previousAttackStopped = attack != null && attack.IsAttackStopped;
        try
        {
            if (stopMove && mover != null) mover.SetMoveStopped(true);
            if (stopAttack && attack != null) attack.SetAttackStopped(true);
            if (forceIdle && context.Visual != null)
                context.Visual.ForceIdle(mover != null ? mover.LastMoveDirection : Vector2.zero, true, false);
            yield return context.WaitSeconds(duration);
        }
        finally
        {
            // 코루틴을 중단해도 패턴이 건 정지 플래그를 복구한다.
            if (context.Enemy != null && (!context.Enemy.IsFullyStopped ||
                (context.Runner != null && context.Runner.IsHandlingLethalDamage)))
            {
                if (mover != null) mover.SetMoveStopped(previousMoveStopped);
                if (attack != null) attack.SetAttackStopped(previousAttackStopped);
            }
        }
    }
}
