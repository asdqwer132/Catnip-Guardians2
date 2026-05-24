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

    void Awake()
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

    public void ResetVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = defaultFlipX;
            spriteRenderer.color = defaultColor;
        }

        transform.localScale = defaultLocalScale;

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger(hitTriggerName);
            animator.ResetTrigger(dieTriggerName);

            animator.SetBool(walkingBoolName, false);

            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void LookDirection(Vector2 direction)
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

    #region PlayAnimation

    public void StopMove()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
    }

    public void PlayMove()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetBool(walkingBoolName, true);
    }

    public void PlayAttack()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

    public void PlayHit()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(hitTriggerName);
    }

    public void PlayDie()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(dieTriggerName);
    }

    public IEnumerator WaitCurrentAnimationEnd()
    {
        if (animator == null)
            yield break;

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(stateInfo.length);
    }

    #endregion
}