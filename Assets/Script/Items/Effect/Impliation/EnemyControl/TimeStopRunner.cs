using UnityEngine;

public sealed class TimeStopRunner : MonoBehaviour
{
    private ItemEffectContext context;
    private TimeStopHandle handle;
    private ItemEffectLease lease;
    private float remaining;
    private bool finished;
    public float RemainingTime => remaining;

    public void Init(ItemEffectContext source, TimeStopTargets targets, float duration)
    {
        context = source;
        remaining = duration;
        lease = context.RetainLifetime();
        handle = TimeStopRuntime.Acquire(targets, context);
    }

    private void Update() { Tick(Time.deltaTime); }

    // 정지 효과의 시계는 적 행동/상태 정지에 영향받지 않는다.
    public void Tick(float deltaTime)
    {
        if (finished) return;
        if (context == null || !context.CanContinue || handle == null || !handle.IsValid)
        { Finish(false); return; }
        remaining -= Mathf.Max(0f, deltaTime);
        if (remaining <= 0f) Finish(true);
    }

    private void Finish(bool completed)
    {
        if (finished) return;
        finished = true;
        if (handle != null) { handle.Dispose(); handle = null; }
        if (lease != null) { lease.Finish(completed); lease = null; }
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
    private void OnDisable() { Finish(false); }
}
