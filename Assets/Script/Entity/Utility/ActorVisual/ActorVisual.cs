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

    [Header("Action Animation")]
    [Tooltip("실제 Animator State 이름입니다. 하위 상태는 전체 경로를 넣을 수 있습니다.")]
    public string attackStateName = "Attack";
    public string hitStateName = "Hit";
    public string dieStateName = "Die";
    [Min(0.01f)] public float attackFallbackDuration = 0.3f;
    [Min(0.01f)] public float hitFallbackDuration = 0.18f;
    [Min(0.01f)] public float animationEntryTimeout = 0.2f;
    [Min(0.1f)] public float maximumActionAnimationTime = 5f;

    private enum ActionAnimation { None, Attack, Hit, Death }
    private ActionAnimation actionAnimation;
    private ActorAnimationTracker actionTracker;
    private int actionAnimationVersion;
    private bool patternAnimationPaused;
    private bool defaultAttackAnimationPaused;
    private bool timeStopAnimationPaused;
    private Enemy enemyOwner;
    private bool IsAnimationPaused => patternAnimationPaused || defaultAttackAnimationPaused || timeStopAnimationPaused;
    private float speedBeforePatternPause = 1f;

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

    public bool IsAnimationPlaybackPaused => IsAnimationPaused;
    public bool IsCustomAnimationLocked => isCustomAnimationLocked;
    public bool IsHitPlaying => actionAnimation == ActionAnimation.Hit && actionTracker.IsPlaying;
    public bool IsAttackPlaying => actionAnimation == ActionAnimation.Attack && actionTracker.IsPlaying;
    public bool IsDeathPlaying => actionAnimation == ActionAnimation.Death;
    protected bool CanPlayLocomotion => !isCustomAnimationLocked && !IsAnimationPaused &&
        !IsHitPlaying && !IsAttackPlaying && !IsDeathPlaying;
    protected bool CanPlayAttack => !isCustomAnimationLocked && !IsAnimationPaused &&
        !IsHitPlaying && !IsDeathPlaying;

    protected virtual void Awake()
    {
        enemyOwner = GetComponent<Enemy>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

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


    protected virtual void Update()
    {
        SetTimeStopAnimationPaused(enemyOwner != null && enemyOwner.IsTimeStopped);
        if (!IsAnimationPaused) actionTracker.Tick(animator, Time.deltaTime);
        if (!actionTracker.IsPlaying && actionAnimation != ActionAnimation.None && actionAnimation != ActionAnimation.Death)
        {
            actionAnimation = ActionAnimation.None;
            ForceIdle(Vector2.zero, false, false);
        }
    }

    protected virtual void OnDisable()
    {
        CancelCustomAnimationLock();
        SetPatternAnimationPaused(false);
        SetDefaultAttackAnimationPaused(false);
        SetTimeStopAnimationPaused(false);
        ClearActionAnimation();
    }

    private void BeginActionAnimation(ActionAnimation action, string stateName, float fallback)
    {
        actionAnimation = action;
        unchecked { actionAnimationVersion++; }
        actionTracker.Begin(animator, stateName, fallback, animationEntryTimeout, maximumActionAnimationTime);
        // 같은 피격 State에 다시 맞아도 처음부터 재생한다.
        if (animator != null && !string.IsNullOrEmpty(stateName) &&
            TryGetStateHash(stateName, 0, out int stateHash))
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == stateHash || current.fullPathHash == stateHash)
                animator.Play(stateHash, 0, 0f);
        }
    }

    private void ClearActionAnimation()
    {
        actionTracker.Clear();
        actionAnimation = ActionAnimation.None;
        unchecked { actionAnimationVersion++; }
    }

    public void CancelAttackAnimation()
    {
        if (actionAnimation != ActionAnimation.Attack) return;
        ClearActionAnimation();
        ForceIdle(Vector2.zero, false, false);
    }

    public void CancelHitReaction()
    {
        if (actionAnimation != ActionAnimation.Hit) return;
        ClearActionAnimation();
        ForceIdle(Vector2.zero, false, false);
    }

    public void SetPatternAnimationPaused(bool paused)
    {
        bool wasPaused = IsAnimationPaused;
        patternAnimationPaused = paused;
        ApplyAnimationPause(wasPaused);
    }

    public void SetDefaultAttackAnimationPaused(bool paused)
    {
        bool wasPaused = IsAnimationPaused;
        defaultAttackAnimationPaused = paused;
        ApplyAnimationPause(wasPaused);
    }

    public void SetTimeStopAnimationPaused(bool paused)
    {
        bool wasPaused = IsAnimationPaused;
        timeStopAnimationPaused = paused;
        ApplyAnimationPause(wasPaused);
    }

    private void ApplyAnimationPause(bool wasPaused)
    {
        if (wasPaused == IsAnimationPaused || animator == null) return;
        if (IsAnimationPaused)
        {
            speedBeforePatternPause = animator.speed;
            animator.speed = 0f;
        }
        else animator.speed = Mathf.Max(0f, speedBeforePatternPause);
    }

    public virtual void ResetVisual()
    {
        CancelCustomAnimationLock();
        SetPatternAnimationPaused(false);
        SetDefaultAttackAnimationPaused(false);
        ClearActionAnimation();

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
        if (IsHitPlaying || IsDeathPlaying || IsAnimationPaused) return;
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
        if (!CanPlayLocomotion)
            return;

        if (animator == null)
            return;

        if (isWalking)
            return;

        isWalking = true;

        animator.speed = IsAnimationPaused ? 0f : 1f;
        animator.ResetTrigger(attackTriggerHash);
        animator.SetBool(walkingBoolHash, true);
    }

    public virtual void PlayMove(Vector2 direction)
    {
        PlayMove();

        if (CanPlayLocomotion)
            LookDirection(direction);
    }

    public virtual void StopMove()
    {
        if (isCustomAnimationLocked || IsHitPlaying || IsDeathPlaying || IsAnimationPaused)
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

        if (!CanPlayLocomotion)
            return;

        if (lastMoveDirection.sqrMagnitude > 0.0001f)
            LookDirection(lastMoveDirection);
    }

    public virtual void PlayAttack()
    {
        if (!CanPlayAttack)
            return;

        BeginActionAnimation(ActionAnimation.Attack, attackStateName, attackFallbackDuration);
        if (animator == null) return;

        isWalking = false;

        animator.speed = IsAnimationPaused ? 0f : 1f;
        animator.SetBool(walkingBoolHash, false);
        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
        animator.SetTrigger(attackTriggerHash);
    }

    public virtual void PlayAttack(Vector2 attackDirection)
    {
        if (!CanPlayAttack) return;
        LookDirection(attackDirection);
        PlayAttack();
    }

    public virtual void PlayHit()
    {
        if (IsDeathPlaying) return;
        CancelCustomAnimationLock();
        SetPatternAnimationPaused(false);
        SetDefaultAttackAnimationPaused(false);
        BeginActionAnimation(ActionAnimation.Hit, hitStateName, hitFallbackDuration);
        if (animator == null) return;

        isWalking = false;

        animator.speed = IsAnimationPaused ? 0f : 1f;
        animator.SetBool(walkingBoolHash, false);
        animator.ResetTrigger(attackTriggerHash);
        animator.ResetTrigger(dieTriggerHash);
        animator.SetTrigger(hitTriggerHash);
    }

    public virtual void PlayDie()
    {
        CancelCustomAnimationLock();
        SetPatternAnimationPaused(false);
        SetDefaultAttackAnimationPaused(false);
        BeginActionAnimation(ActionAnimation.Death, dieStateName, attackFallbackDuration);

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = IsAnimationPaused ? 0f : 1f;
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
        if (IsHitPlaying || IsDeathPlaying || IsAnimationPaused) return;
        if (isCustomAnimationLocked && !ignoreCustomAnimationLock)
            return;

        if (keepDirection && lookDirection.sqrMagnitude > 0.0001f)
            LookDirection(lookDirection);

        if (animator == null)
            return;

        isWalking = false;

        animator.speed = IsAnimationPaused ? 0f : 1f;
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

        animator.speed = IsAnimationPaused ? 0f : 1f;
    }

    public IEnumerator WaitCurrentAnimationEnd()
    {
        int version = actionAnimationVersion;
        if (actionTracker.IsPlaying)
        {
            while (version == actionAnimationVersion && actionTracker.IsPlaying) yield return null;
            yield break;
        }
        if (animator == null) yield break;
        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        int hash = state.fullPathHash;
        float elapsed = 0f;
        while (animator != null && elapsed < maximumActionAnimationTime)
        {
            state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != hash || state.normalizedTime >= 1f) break;
            if (!IsAnimationPaused) elapsed += Time.deltaTime;
            yield return null;
        }
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
        if (IsHitPlaying || IsDeathPlaying) return false;
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

        animator.speed = IsAnimationPaused ? 0f : 1f;
        ClearActionAnimation();

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
        float elapsed = 0f;
        while (animator != null)
        {
            if (IsAnimationPaused || animator.speed <= 0f) { yield return null; continue; }
            elapsed += Time.deltaTime;
            if (elapsed >= maximumActionAnimationTime) break;
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
