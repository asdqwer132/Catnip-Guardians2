using UnityEngine;

public class PlayerActorVisual : ActorVisual
{
    [Header("Player Move Animator Params")]
    public string moveXFloatName = "MoveX";
    public string moveYFloatName = "MoveY";
    [Header("Player Idle Animator Params")]
    public string idleXFloatName = "IdleX";
    public string idleYFloatName = "IdleY";
    [Header("Player Attack Animator Params")]
    public string attackXFloatName = "AttackX";
    public string attackYFloatName = "AttackY";
    [Header("Default Direction")]
    public Vector2 defaultIdleDirection = Vector2.down;
    private Vector2 lastDirection;

    protected override void Awake()
    {
        base.Awake();
        ResetDirection();
    }
    public override void ResetVisual()
    {
        base.ResetVisual();
        ResetDirection();
    }
    private void ResetDirection()
    {
        lastDirection = defaultIdleDirection.sqrMagnitude > 0.0001f
            ? defaultIdleDirection.normalized : Vector2.down;
        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }

    // 다방향 플레이어 애니메이션은 flipX 대신 방향 파라미터를 사용한다.
    public override void LookDirection(Vector2 direction)
    {
        if (IsHitPlaying || IsDeathPlaying || direction.sqrMagnitude <= 0.0001f) return;
        lastDirection = direction.normalized;
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }
    public override void PlayMove(Vector2 direction)
    {
        if (!CanPlayLocomotion) return;
        if (direction.sqrMagnitude <= 0.0001f) { StopMove(); return; }
        LookDirection(direction);
        base.PlayMove();
        ApplyMoveDirection(lastDirection);
    }
    public override void StopMove() { StopMove(lastDirection); }
    public override void StopMove(Vector2 direction)
    {
        if (!CanPlayLocomotion) return;
        if (direction.sqrMagnitude > 0.0001f) lastDirection = direction.normalized;
        base.StopMove();
        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }
    public override void PlayAttack()
    {
        if (!CanPlayAttack) return;
        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
        base.PlayAttack();
    }
    public override void PlayAttack(Vector2 direction)
    {
        if (!CanPlayAttack) return;
        LookDirection(direction);
        PlayAttack();
    }
    public override void PlayHit()
    {
        if (IsDeathPlaying) return;
        base.PlayHit();
        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }
    public override void PlayDie()
    {
        base.PlayDie();
        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }
    private void ApplyMoveDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat(moveXFloatName, direction.x);
        animator.SetFloat(moveYFloatName, direction.y);
    }
    private void ApplyIdleDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat(idleXFloatName, direction.x);
        animator.SetFloat(idleYFloatName, direction.y);
    }
    private void ApplyAttackDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat(attackXFloatName, direction.x);
        animator.SetFloat(attackYFloatName, direction.y);
    }
}
