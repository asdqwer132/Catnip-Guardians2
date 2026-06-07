using System.Collections;
using UnityEngine;

public class ActorVisual : MonoBehaviour
{
    [Header("Visual")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public bool defaultFaceLeft = false;

    [Header("Animator Params")]
    public string walkingBoolName = "IsWalking";
    public string attackTriggerName = "Attack";
    public string hitTriggerName = "Hit";
    public string dieTriggerName = "Die";

    private bool defaultFlipX;
    private Color defaultColor;
    private Vector3 defaultLocalScale;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            defaultFlipX = spriteRenderer.flipX;
            defaultColor = spriteRenderer.color;
        }

        defaultLocalScale = transform.localScale;
    }

    public virtual void ResetVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = defaultFlipX;
            spriteRenderer.color = defaultColor;
        }

        transform.localScale = defaultLocalScale;

        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetBool(walkingBoolName, false);

        animator.Rebind();
        animator.Update(0f);
    }

    public virtual void LookDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        if (Mathf.Abs(direction.x) < 0.01f)
            return;

        bool faceLeft = direction.x < 0f;

        if (defaultFaceLeft)
            spriteRenderer.flipX = !faceLeft;
        else
            spriteRenderer.flipX = faceLeft;
    }

    public virtual void PlayMove()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetBool(walkingBoolName, true);
    }

    public virtual void PlayMove(Vector2 direction)
    {
        PlayMove();
        LookDirection(direction);
    }

    public virtual void StopMove()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
    }

    public virtual void StopMove(Vector2 lastMoveDirection)
    {
        StopMove();
    }

    public virtual void PlayAttack()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

    public virtual void PlayAttack(Vector2 attackDirection)
    {
        PlayAttack();
        LookDirection(attackDirection);
    }

    public virtual void PlayHit()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(attackTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetTrigger(hitTriggerName);
    }

    public virtual void PlayDie()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(attackTriggerName);
        animator.ResetTrigger(hitTriggerName);
        animator.SetTrigger(dieTriggerName);
    }

    public virtual void PauseAnimation()
    {
        if (animator == null)
            return;

        animator.speed = 0f;
    }

    public virtual void ResumeAnimation()
    {
        if (animator == null)
            return;

        animator.speed = 1f;
    }

    public IEnumerator WaitCurrentAnimationEnd()
    {
        if (animator == null)
            yield break;

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
    }
}