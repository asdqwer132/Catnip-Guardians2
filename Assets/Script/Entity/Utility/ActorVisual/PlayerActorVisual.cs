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

        lastDirection = defaultIdleDirection.sqrMagnitude > 0.0001f
            ? defaultIdleDirection.normalized
            : Vector2.down;

        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }

    public override void ResetVisual()
    {
        base.ResetVisual();

        lastDirection = defaultIdleDirection.sqrMagnitude > 0.0001f
            ? defaultIdleDirection.normalized
            : Vector2.down;

        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }

    public override void PlayMove(Vector2 direction)
    {
        if (animator == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            StopMove(lastDirection);
            return;
        }

        direction.Normalize();
        lastDirection = direction;

        animator.ResetTrigger(attackTriggerName);
        animator.SetBool(walkingBoolName, true);

        ApplyMoveDirection(direction);
        ApplyIdleDirection(direction);
        ApplyAttackDirection(direction);
    }

    public override void StopMove()
    {
        StopMove(lastDirection);
    }

    public override void StopMove(Vector2 lastMoveDirection)
    {
        if (animator == null)
            return;

        if (lastMoveDirection.sqrMagnitude > 0.0001f)
            lastDirection = lastMoveDirection.normalized;

        animator.SetBool(walkingBoolName, false);

        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);
    }

    public override void PlayAttack()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(dieTriggerName);

        ApplyMoveDirection(Vector2.zero);
        ApplyIdleDirection(lastDirection);
        ApplyAttackDirection(lastDirection);

        animator.SetTrigger(attackTriggerName);
    }

    public override void PlayAttack(Vector2 attackDirection)
    {
        if (animator == null)
            return;

        if (attackDirection.sqrMagnitude > 0.0001f)
            lastDirection = attackDirection.normalized;

        PlayAttack();
    }

    public override void PlayHit()
    {
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
        if (animator == null)
            return;

        animator.SetFloat(moveXFloatName, direction.x);
        animator.SetFloat(moveYFloatName, direction.y);
    }

    private void ApplyIdleDirection(Vector2 direction)
    {
        if (animator == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.down;

        direction.Normalize();

        animator.SetFloat(idleXFloatName, direction.x);
        animator.SetFloat(idleYFloatName, direction.y);
    }

    private void ApplyAttackDirection(Vector2 direction)
    {
        if (animator == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.down;

        direction.Normalize();

        animator.SetFloat(attackXFloatName, direction.x);
        animator.SetFloat(attackYFloatName, direction.y);
    }
}