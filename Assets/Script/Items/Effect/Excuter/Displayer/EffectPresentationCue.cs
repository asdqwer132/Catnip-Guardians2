using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 아이템/상자 등 호출자의 연출 연결 정보. 실제 VFX와 사운드는 EffectVisualData가 재생합니다.
[Serializable]
public sealed class EffectPresentationCue
{
    public Transform anchor;
    public bool followTarget;
    public EffectVisualData[] effects = Array.Empty<EffectVisualData>();
    public UnityEvent onPlay = new UnityEvent();
    private sealed class Playback { public EffectVisualAnchor anchor; public EffectVisualContext context; public bool stopOnReset; }
    private readonly List<Playback> playing = new List<Playback>();
    public void Play(GameObject owner)
    {
        if (owner == null) return;
        onPlay?.Invoke();
        playing.RemoveAll(p => p.anchor == null || !p.anchor.IsCurrentPlayback(p.context));
        Transform target = anchor != null ? anchor : owner.transform;
        foreach (var effect in effects)
        {
            if (effect == null) continue;
            var context = new EffectVisualContext(target.position, target.rotation,
                followTarget: followTarget ? target : null, audioOwner: owner);
            var visual = effect.Play(context);
            if (visual == null || (!followTarget && !effect.stopOnReset)) continue;
            var root = visual.GetComponentInParent<EffectVisualAnchor>();
            if (root != null) playing.Add(new Playback { anchor = root, context = context, stopOnReset = effect.stopOnReset });
        }
    }
    public void Stop()
    {
        foreach (var playback in playing)
            if (playback.anchor != null && playback.anchor.IsCurrentPlayback(playback.context))
            {
                if (playback.stopOnReset) playback.anchor.ReturnToPool();
                else playback.context.DetachFollow();
            }
        playing.Clear();
    }
}
