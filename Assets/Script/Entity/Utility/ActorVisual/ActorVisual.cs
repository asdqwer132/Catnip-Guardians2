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

    [Header("Animator State")]
    public string idleStateName = "Idle";

    private bool defaultFlipX;
    private Color defaultColor;
    private Vector3 defaultLocalScale;

    private Coroutine customAnimationRoutine;
    private bool isCustomAnimationLocked;
    private int currentCustomStateHash;

    private bool currentFaceLeft;
    private bool hasFaceDirection;
    private bool isWalking;

    private int walkingBoolHash;
    private int attackTriggerHash;
    private int hitTriggerHash;
    private int dieTriggerHash;
    private int idleStateHash;


    private const float FaceThreshold = 0.01f;

    public bool IsCustomAnimationLocked => isCustomAnimationLocked;

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

        walkingBoolHash = Animator.StringToHash(walkingBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);
        hitTriggerHash = Animator.StringToHash(hitTriggerName);
        dieTriggerHash = Animator.StringToHash(dieTriggerName);
        idleStateHash = string.IsNullOrEmpty(idleStateName) ? 0 : Animator.StringToHash(idleStateName);

    }


    public virtual void ResetVisual()
    {
        CancelCustomAnimationLock();

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = defaultFlipX;
            spriteRenderer.color = defaultColor;
        }

        currentFaceLeft = defaultFaceLeft;
        hasFaceDirection = false;
        isWalking = false;

        transform.localScale = defaultLocalScale;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        ForceIdle(Vector2.zero, false, true, true);
    }

    public virtual void LookDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        if (Mathf.Abs(direction.x) <= FaceThreshold)
            return;

        bool faceLeft = direction.x < 0f;

        if (hasFaceDirection && currentFaceLeft == faceLeft)
            return;

        hasFaceDirection = true;
        currentFaceLeft = faceLeft;

        if (defaultFaceLeft)
            spriteRenderer.flipX = !faceLeft;
        else
            spriteRenderer.flipX = faceLeft;
    }

    public virtual void PlayMove()
    {
        if (isCustomAnimationLocked)
            return;

        if (animator == null)
            return;

        if (isWalking)
            return;

        isWalking = true;

        animator.speed = 1f;
        animator.ResetTrigger(attackTriggerHash);
        animator.SetBool(walkingBoolHash, true);
    }

    public virtual void PlayMove(Vector2 direction)
    {
        PlayMove();

        if (!isCustomAnimationLocked)
            LookDirection(direction);
    }

    public virtual void StopMove()
    {
        if (isCustomAnimationLocked)
            return;

        if (animator == null)
            return;

        if (!isWalking)
            return;

        isWalking = false;
        animator.SetBool(walkingBoolHash, false);
    }

    public virtual void StopMove(Vector2 lastMoveDirection)
    {
        StopMove();

        if (isCustomAnimationLocked)
            return;

        if (lastMoveDirection.sqrMagnitude > 0.0001f)
            LookDirection(lastMoveDirection);
    }

    public virtual void PlayAttack()
    {
        if (isCustomAnimationLocked)
            return;

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = 1f;
        animator.SetBool(walkingBoolHash, false);
        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
        animator.SetTrigger(attackTriggerHash);
    }

    public virtual void PlayAttack(Vector2 attackDirection)
    {
        PlayAttack();

        if (!isCustomAnimationLocked)
            LookDirection(attackDirection);
    }

    public virtual void PlayHit()
    {
        if (isCustomAnimationLocked)
            return;

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = 1f;
        animator.SetBool(walkingBoolHash, false);
        animator.ResetTrigger(attackTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
        animator.SetTrigger(hitTriggerHash);
    }

    public virtual void PlayDie()
    {
        CancelCustomAnimationLock();

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = 1f;
        animator.SetBool(walkingBoolHash, false);
        animator.ResetTrigger(attackTriggerHash);
        animator.ResetTrigger(hitTriggerHash);
        animator.SetTrigger(dieTriggerHash);
    }

    public virtual void ForceIdle(
        Vector2 lookDirection,
        bool keepDirection = true,
        bool restartIdleAnimation = false,
        bool ignoreCustomAnimationLock = false)
    {
        if (isCustomAnimationLocked && !ignoreCustomAnimationLock)
            return;

        if (keepDirection && lookDirection.sqrMagnitude > 0.0001f)
            LookDirection(lookDirection);

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = 1f;
        animator.ResetTrigger(attackTriggerHash);
        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
        animator.SetBool(walkingBoolHash, false);

        if (string.IsNullOrEmpty(idleStateName))
            return;

        int idleHash = idleStateHash;

        if (!animator.HasState(0, idleHash))
        {
            string fullIdleName = animator.GetLayerName(0) + "." + idleStateName;
            idleHash = Animator.StringToHash(fullIdleName);

            if (!animator.HasState(0, idleHash))
                return;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        bool isAlreadyIdle =
            currentState.shortNameHash == idleHash ||
            currentState.fullPathHash == idleHash;

        if (isAlreadyIdle && !restartIdleAnimation)
            return;

        animator.Play(idleHash, 0, restartIdleAnimation ? 0f : currentState.normalizedTime);
        animator.Update(0f);
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

    public virtual bool PlayAnimationByName(
        string stateName,
        bool restartAnimation = true,
        bool blockOtherVisuals = true,
        bool stopMove = true,
        bool resetTriggers = true,
        bool returnIdleWhenEnd = false,
        bool ignoreIfAlreadyPlaying = true,
        int layer = 0,
        float normalizedTime = 0f,
        float crossFadeTime = 0f)
    {
        if (animator == null)
            return false;

        if (string.IsNullOrEmpty(stateName))
            return false;

        if (!TryGetStateHash(stateName, layer, out int stateHash))
        {
            Debug.LogWarning($"[{name}] Animator State를 찾을 수 없음: {stateName}");
            return false;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layer);

        bool isAlreadyPlaying =
            currentState.shortNameHash == stateHash ||
            currentState.fullPathHash == stateHash ||
            currentCustomStateHash == stateHash;

        if (ignoreIfAlreadyPlaying && isCustomAnimationLocked && isAlreadyPlaying)
            return true;

        CancelCustomAnimationLock();

        animator.speed = 1f;

        if (stopMove)
        {
            isWalking = false;
            animator.SetBool(walkingBoolHash, false);
        }

        if (resetTriggers)
            ResetActionTriggers();

        float playTime = restartAnimation ? normalizedTime : 0f;

        if (crossFadeTime > 0f)
            animator.CrossFade(stateHash, crossFadeTime, layer, playTime);
        else
            animator.Play(stateHash, layer, playTime);

        animator.Update(0f);

        if (blockOtherVisuals)
        {
            isCustomAnimationLocked = true;
            currentCustomStateHash = stateHash;
            customAnimationRoutine = StartCoroutine(CustomAnimationLockRoutine(stateHash, layer, returnIdleWhenEnd));
        }

        return true;
    }

    public virtual bool PlayAnimationByName(
        string stateName,
        Vector2 lookDirection,
        bool restartAnimation = true,
        bool blockOtherVisuals = true,
        bool stopMove = true,
        bool resetTriggers = true,
        bool returnIdleWhenEnd = false,
        bool ignoreIfAlreadyPlaying = true,
        int layer = 0,
        float normalizedTime = 0f,
        float crossFadeTime = 0f)
    {
        if (lookDirection.sqrMagnitude > 0.0001f)
            LookDirection(lookDirection);

        return PlayAnimationByName(
            stateName,
            restartAnimation,
            blockOtherVisuals,
            stopMove,
            resetTriggers,
            returnIdleWhenEnd,
            ignoreIfAlreadyPlaying,
            layer,
            normalizedTime,
            crossFadeTime
        );
    }

    public virtual void CancelCustomAnimation()
    {
        CancelCustomAnimationLock();
    }

    private void CancelCustomAnimationLock()
    {
        if (customAnimationRoutine != null)
        {
            StopCoroutine(customAnimationRoutine);
            customAnimationRoutine = null;
        }

        isCustomAnimationLocked = false;
        currentCustomStateHash = 0;
    }

    private IEnumerator CustomAnimationLockRoutine(int stateHash, int layer, bool returnIdleWhenEnd)
    {
        yield return null;

        while (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);

            bool isCurrentCustomState =
                stateInfo.shortNameHash == stateHash ||
                stateInfo.fullPathHash == stateHash;

            if (!isCurrentCustomState && !animator.IsInTransition(layer))
                break;

            if (isCurrentCustomState && !animator.IsInTransition(layer) && stateInfo.normalizedTime >= 1f)
                break;

            yield return null;
        }

        isCustomAnimationLocked = false;
        currentCustomStateHash = 0;
        customAnimationRoutine = null;

        if (returnIdleWhenEnd)
            ForceIdle(Vector2.zero, false, false, true);
    }

    private bool TryGetStateHash(string stateName, int layer, out int stateHash)
    {
        stateHash = 0;

        if (animator == null)
            return false;

        if (layer < 0 || layer >= animator.layerCount)
            return false;

        stateHash = Animator.StringToHash(stateName);

        if (animator.HasState(layer, stateHash))
            return true;

        string layerStateName = animator.GetLayerName(layer) + "." + stateName;
        stateHash = Animator.StringToHash(layerStateName);

        if (animator.HasState(layer, stateHash))
            return true;

        stateHash = 0;
        return false;
    }

    private void ResetActionTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(attackTriggerHash);
        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
    }
}