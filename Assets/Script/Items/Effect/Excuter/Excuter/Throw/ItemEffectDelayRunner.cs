using System.Collections.Generic;
using UnityEngine;

// 이동제어처럼 별도 런타임 객체가 없는 효과의 수명만 함께 갱신한다.
public sealed class ItemEffectDelayRunner : MonoBehaviour
{
    private sealed class WaitState
    {
        public ItemEffectContext context;
        public ItemEffectLease lease;
        public float remaining;
    }
    private static ItemEffectDelayRunner instance;
    private readonly List<WaitState> states = new List<WaitState>();
    public static void Wait(ItemEffectContext context, float duration)
    {
        if (context == null || context.lifetime == null || duration <= 0f) return;
        if (instance == null)
        {
            GameObject host = new GameObject("ItemEffectDelays");
            instance = host.AddComponent<ItemEffectDelayRunner>();
        }
        instance.states.Add(new WaitState { context = context, lease = context.RetainLifetime(), remaining = duration });
    }
    private void Update()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            WaitState state = states[i];
            state.remaining -= Time.deltaTime;
            if (state.context.CanContinue && state.remaining > 0f) continue;
            states.RemoveAt(i);
            if (state.lease != null) state.lease.Finish(state.context.CanContinue);
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < states.Count; i++) if (states[i].lease != null) states[i].lease.Cancel();
        states.Clear();
    }
    private void OnDestroy() { OnDisable(); if (instance == this) instance = null; }
}
