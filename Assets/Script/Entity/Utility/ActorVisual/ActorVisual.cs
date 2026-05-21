using System.Collections;
using UnityEngine;

public class ActorVisual : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;

    [Header("Flip")]
    [Tooltip("원본 스프라이트가 왼쪽을 보고 있으면 true, 오른쪽을 보고 있으면 false")]
    public bool defaultFaceLeft = false;

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
        if (target == null)
            return;

        Vector2 direction = target.position - transform.position;
        LookDirection(direction);
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

    public void PlayMove()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetBool(walkingBoolName, true);
    }

    public void StopMove()
    {
        if (animator == null)
            return;

        animator.SetBool(walkingBoolName, false);
    }

    public void SetWalking(bool value)
    {
        if (value)
            PlayMove();
        else
            StopMove();
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
}