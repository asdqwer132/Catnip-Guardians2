using System;
using UnityEngine;
using UnityEngine.Events;

public enum MergeItemPresentationEvent { Spawned, IdleBegin, IdleEnd, MergeConsumed, MergeCreated, Gimmick, Returned }

// 연출 구현은 별도 컴포넌트/모듈에 두고 게임 로직은 이 진입점만 호출합니다.
[DisallowMultipleComponent, RequireComponent(typeof(MergeItemObject))]
public sealed class MergeItemPresentation : MonoBehaviour
{
    [Serializable] public sealed class Binding
    {
        public MergeItemPresentationEvent trigger;
        [Tooltip("Gimmick에서만 사용합니다. 비우면 모든 기믹에 반응합니다.")]
        public string gimmickId;
        public EffectPresentationCue presentation = new EffectPresentationCue();
    }
    public Binding[] bindings = Array.Empty<Binding>();
    public MergeItemPresentationModule[] modules = Array.Empty<MergeItemPresentationModule>();
    public MergeItemObject Item { get; private set; }
    public string LastGimmickId { get; private set; }
    private bool live, idle;
    public void BeginLifetime()
    {
        Item = GetComponent<MergeItemObject>(); live = true;
        Play(MergeItemPresentationEvent.Spawned);
        RefreshIdle();
    }
    private void LateUpdate() { if (live) RefreshIdle(); }
    private void RefreshIdle()
    {
        bool next = Item != null && Item.IsLive && !Item.IsHeld;
        if (next == idle) return;
        if (!next) foreach (var binding in bindings)
            if (binding != null && binding.trigger == MergeItemPresentationEvent.IdleBegin) binding.presentation?.Stop();
        idle = next; Play(idle ? MergeItemPresentationEvent.IdleBegin : MergeItemPresentationEvent.IdleEnd);
    }
    public void Play(MergeItemPresentationEvent trigger, string gimmickId = null)
    {
        if (!live) return;
        LastGimmickId = gimmickId;
        foreach (var module in modules)
            if (module != null && module.isActiveAndEnabled) module.Play(this, trigger, gimmickId);
        foreach (var binding in bindings)
        {
            if (binding == null || binding.trigger != trigger || (trigger == MergeItemPresentationEvent.Gimmick &&
                !string.IsNullOrEmpty(binding.gimmickId) && binding.gimmickId != gimmickId)) continue;
            binding.presentation?.Play(gameObject);
        }
    }
    public void EndLifetime()
    {
        if (!live) return;
        if (idle) { idle = false; Play(MergeItemPresentationEvent.IdleEnd); }
        Play(MergeItemPresentationEvent.Returned);
        live = false;
        foreach (var binding in bindings) binding?.presentation?.Stop();
        foreach (var module in modules) if (module != null) module.ResetPresentation();
        LastGimmickId = null;
    }
    private void OnDisable() => EndLifetime();
}
