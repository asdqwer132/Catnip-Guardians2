using UnityEngine;

public sealed class CooldownControlRunner : MonoBehaviour
{
    private CooldownControlEffect settings;
    private ItemEffectContext context;
    private ItemEffectLease lease;
    private float remaining;
    public void Init(CooldownControlEffect value, ItemEffectContext execution, float delay)
    { settings = value; context = execution; remaining = delay; lease = execution.RetainLifetime(); }
    private void Update()
    {
        if (context == null || !context.CanContinue) { Destroy(gameObject); return; }
        remaining -= Time.deltaTime;
        if (remaining > 0f) return;
        settings.Apply(context);
        lease?.Finish(); lease = null;
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        lease?.Cancel(); lease = null;
        if (settings != null) Destroy(settings);
        settings = null;
    }
}
