using System.Collections;
using UnityEngine;

public class ActorVisual : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;

    [Header("Animator Params")]
    public string walkingBoolName = "IsWalking";
    public string attackTriggerName = "Attack";
    public string hitTriggerName = "Hit";
    public string dieTriggerName = "Die";

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void LookAt(Transform target)
    {
        if (target == null || spriteRenderer == null)
            return;

        spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    public void SetWalking(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, value);
    }

    public void PlayAttack()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(hitTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

    public void PlayHit()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(hitTriggerName);
    }

    public void PlayDie()
    {
        if (animator == null)
            return;

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
}