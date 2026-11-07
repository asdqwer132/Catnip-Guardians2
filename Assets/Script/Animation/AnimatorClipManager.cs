using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorClipManager : MonoBehaviour
{
    [Serializable]
    public class AnimationEntry
    {
        public string stateName;
        public AnimationClip[] clips;

        [SerializeField, HideInInspector]
        public AnimationClip originalClip;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private AnimationEntry[] animations;

    private AnimatorOverrideController overrideController;
    private Dictionary<string, AnimationEntry> animationMap;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator가 설정되지 않았습니다.", this);
            return;
        }

        overrideController = new AnimatorOverrideController(
            animator.runtimeAnimatorController
        );

        animator.runtimeAnimatorController = overrideController;

        animationMap = new Dictionary<string, AnimationEntry>();

        foreach (var entry in animations)
        {
            if (entry == null || string.IsNullOrEmpty(entry.stateName))
                continue;

            animationMap[entry.stateName] = entry;

            if (entry.originalClip == null)
            {
                Debug.LogWarning(
                    $"State '{entry.stateName}'의 원본 클립을 찾지 못했습니다.",
                    this
                );
            }
        }
    }

    // 특정 State의 클립 교체
    public void SetClip(string stateName, AnimationClip clip)
    {
        if (overrideController == null || clip == null)
            return;

        if (!animationMap.TryGetValue(stateName, out var entry))
            return;

        if (entry.originalClip == null)
            return;

        overrideController[entry.originalClip] = clip;
    }

    // 특정 State의 클립을 인덱스로 교체
    public void SetClip(string stateName, int index)
    {
        if (animationMap == null ||
            !animationMap.TryGetValue(stateName, out var entry))
            return;

        if (entry.clips == null ||
            index < 0 ||
            index >= entry.clips.Length)
            return;

        SetClip(stateName, entry.clips[index]);
    }

    // 특정 State의 클립을 랜덤 교체
    public void SetRandomClip(string stateName)
    {
        if (animationMap == null ||
            !animationMap.TryGetValue(stateName, out var entry))
            return;

        if (entry.clips == null || entry.clips.Length == 0)
            return;

        int index = UnityEngine.Random.Range(0, entry.clips.Length);

        SetClip(stateName, index);
    }

    // 특정 State 실행
    public void Play(string stateName)
    {
        if (animator == null) return;

        animator.Play(stateName, 0, 0f);
    }

    // 클립 교체 후 실행
    public void Play(string stateName, int index)
    {
        SetClip(stateName, index);
        Play(stateName);
    }

    // 랜덤 클립 교체 후 실행
    public void PlayRandom(string stateName)
    {
        SetRandomClip(stateName);
        Play(stateName);
    }

    // 전체 State 랜덤 교체
    public void RandomizeAll()
    {
        if (animations == null) return;

        foreach (var entry in animations)
        {
            if (entry == null) continue;

            SetRandomClip(entry.stateName);
        }
    }

    // 전체 State를 지정한 인덱스로 교체
    public void SetAllClips(int index)
    {
        if (animations == null) return;

        foreach (var entry in animations)
        {
            if (entry == null) continue;

            SetClip(entry.stateName, index);
        }
    }

    // 전체 State의 클립 원본 복원
    public void ResetAllClips()
    {
        if (overrideController == null || animations == null)
            return;

        foreach (var entry in animations)
        {
            if (entry == null || entry.originalClip == null)
                continue;

            overrideController[entry.originalClip] = entry.originalClip;
        }
    }

#if UNITY_EDITOR

    // Inspector 값 변경 시 원본 클립 자동 검색
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        RefreshOriginalClips();
    }

    [ContextMenu("Refresh Original Clips")]
    private void RefreshOriginalClips()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null || animations == null)
            return;

        RuntimeAnimatorController runtimeController =
            animator.runtimeAnimatorController;

        // OverrideController인 경우 원본 Controller 가져오기
        while (runtimeController is AnimatorOverrideController overrideCtrl)
        {
            runtimeController = overrideCtrl.runtimeAnimatorController;
        }

        var controller =
            runtimeController as UnityEditor.Animations.AnimatorController;

        if (controller == null)
            return;

        foreach (var entry in animations)
        {
            if (entry == null) continue;

            entry.originalClip = null;

            if (string.IsNullOrEmpty(entry.stateName))
                continue;

            foreach (var layer in controller.layers)
            {
                AnimationClip clip = FindOriginalClip(
                    layer.stateMachine,
                    entry.stateName
                );

                if (clip == null) continue;

                entry.originalClip = clip;
                break;
            }
        }
    }

    // State 이름으로 원본 AnimationClip 검색
    private AnimationClip FindOriginalClip(
        UnityEditor.Animations.AnimatorStateMachine stateMachine,
        string stateName)
    {
        foreach (var child in stateMachine.states)
        {
            if (child.state.name == stateName)
                return child.state.motion as AnimationClip;
        }

        // 하위 StateMachine까지 검색
        foreach (var child in stateMachine.stateMachines)
        {
            AnimationClip clip = FindOriginalClip(
                child.stateMachine,
                stateName
            );

            if (clip != null)
                return clip;
        }

        return null;
    }

#endif
}