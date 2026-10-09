using UnityEngine;

public sealed class TimeStopRunner : MonoBehaviour
{
    private ItemEffectContext context;
    private TimeStopHandle handle;
    private ItemEffectLease lease;
    private EffectBuffUIHandle buffUI;
    private float remaining;
    private bool finished;
    public float RemainingTime => remaining;

    public void Init(ItemEffectContext source, TimeStopTargets targets, float duration, TimeStopEffect effect = null)
    {
        context = source;
        remaining = duration;
        lease = context.RetainLifetime();
        handle = TimeStopRuntime.Acquire(targets, context);
        IBuffTarget target = context.owner != null ? context.owner.GetComponentInParent<IBuffTarget>() : null;
        buffUI = effect != null && effect.buffUI != null ? effect.buffUI.Register(effect, context, target, duration) : null;
    }

    private void Update() { Tick(Time.deltaTime); }

    // 정지 효과의 시계는 적 행동/상태 정지에 영향받지 않는다.
    public void Tick(float deltaTime)
    {
        if (finished) return;
        if (context == null || !context.CanContinue || handle == null || !handle.IsValid)
        { Finish(false); return; }
        remaining -= Mathf.Max(0f, deltaTime);
        if (buffUI != null) buffUI.SetRemaining(remaining);
        if (remaining <= 0f) Finish(true);
    }

    private void Finish(bool completed)
    {
        if (finished) return;
        finished = true;
        if (handle != null) { handle.Dispose(); handle = null; }
        if (buffUI != null) { buffUI.Dispose(); buffUI = null; }
        if (lease != null) { lease.Finish(completed); lease = null; }
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
    private void OnDisable() { Finish(false); }
}
