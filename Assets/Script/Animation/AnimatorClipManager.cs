using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorClipManager : MonoBehaviour
{
    [Serializable]
    public class AnimationEntry
    {
        public string stateName;
        public AnimationClip originalClip;
        public AnimationClip[] clips;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private AnimationEntry[] animations;

    private AnimatorOverrideController overrideController;
    private Dictionary<string, AnimationEntry> animationMap;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        overrideController = new AnimatorOverrideController(
            animator.runtimeAnimatorController
        );

        animator.runtimeAnimatorController = overrideController;

        animationMap = new Dictionary<string, AnimationEntry>();

        foreach (var entry in animations)
        {
            if (string.IsNullOrEmpty(entry.stateName))
                continue;

            animationMap[entry.stateName] = entry;
        }
    }

    // 특정 State의 클립 교체
    public void SetClip(string stateName, AnimationClip clip)
    {
        if (clip == null) return;
        if (!animationMap.TryGetValue(stateName, out var entry))
            return;

        if (entry.originalClip == null) return;

        overrideController[entry.originalClip.name] = clip;
    }

    // 특정 State의 클립을 인덱스로 교체
    public void SetClip(string stateName, int index)
    {
        if (!animationMap.TryGetValue(stateName, out var entry))
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
        if (!animationMap.TryGetValue(stateName, out var entry))
            return;

        if (entry.clips == null || entry.clips.Length == 0)
            return;

        int index = UnityEngine.Random.Range(0, entry.clips.Length);

        SetClip(stateName, index);
    }

    // 특정 State 실행
    public void Play(string stateName)
    {
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

    // 전체 State의 클립을 랜덤으로 교체
    public void RandomizeAll()
    {
        foreach (var entry in animations)
        {
            SetRandomClip(entry.stateName);
        }
    }

    // 전체 State를 지정된 인덱스의 클립으로 교체
    public void SetAllClips(int index)
    {
        foreach (var entry in animations)
        {
            SetClip(entry.stateName, index);
        }
    }

    // 전체 State를 원본 클립으로 복원
    public void ResetAllClips()
    {
        foreach (var entry in animations)
        {
            if (entry.originalClip == null) continue;

            overrideController[entry.originalClip.name] =
                entry.originalClip;
        }
    }
}