using UnityEngine;

// 트리거 전달과 실제 State 진입 사이의 지연을 처리하는 값 타입 상태다.
// Animator/State가 없으면 대체 시간으로 종료하며, 무한 대기도 방지한다.
internal struct ActorAnimationTracker
{
    private int stateHash;
    private float elapsed;
    private float fallbackDuration;
    private float entryTimeout;
    private float maximumDuration;
    private bool entered;

    internal bool IsPlaying { get; private set; }

    internal void Begin(Animator animator, string stateName, float fallback, float timeout, float maximum)
    {
        this = default(ActorAnimationTracker);
        fallbackDuration = Mathf.Max(0.01f, fallback);
        entryTimeout = Mathf.Max(0.01f, timeout);
        maximumDuration = Mathf.Max(fallbackDuration, maximum);
        IsPlaying = true;
        if (animator == null || animator.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(stateName) || animator.layerCount == 0)
            return;

        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash)) stateHash = hash;
        else
        {
            hash = Animator.StringToHash(animator.GetLayerName(0) + "." + stateName);
            if (animator.HasState(0, hash)) stateHash = hash;
        }
    }

    internal void Tick(Animator animator, float deltaTime)
    {
        if (!IsPlaying || deltaTime <= 0f) return;
        if (animator != null && animator.speed <= 0f) return;
        elapsed += deltaTime;
        if (animator == null || !animator.isActiveAndEnabled ||
            animator.runtimeAnimatorController == null || stateHash == 0)
        {
            IsPlaying = elapsed < fallbackDuration;
            return;
        }
        if (elapsed >= maximumDuration) { IsPlaying = false; return; }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        bool isCurrent = Matches(current);
        bool isNext = animator.IsInTransition(0) && Matches(animator.GetNextAnimatorStateInfo(0));
        if (isCurrent)
        {
            entered = true;
            if (current.normalizedTime >= 1f && !isNext) IsPlaying = false;
        }
        else if (entered && !isNext) IsPlaying = false;
        else if (!entered && !isNext && elapsed >= Mathf.Max(entryTimeout, fallbackDuration))
            IsPlaying = false;
    }

    private bool Matches(AnimatorStateInfo state)
    {
        return state.shortNameHash == stateHash || state.fullPathHash == stateHash;
    }
    internal void Clear() { this = default(ActorAnimationTracker); }
}
