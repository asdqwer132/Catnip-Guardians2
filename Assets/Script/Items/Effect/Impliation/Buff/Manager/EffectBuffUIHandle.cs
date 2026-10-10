using System;
using UnityEngine;

// This record is never added to BuffStorage: it cannot modify stats or consume uses.
public sealed class EffectBuffUIHandle : IDisposable
{
    private BuffManager manager;
    private readonly ItemEffectContext context;
    public ActiveBuff DisplayBuff { get; private set; }
    internal bool CanDisplay => manager != null && context != null && context.CanContinue &&
        DisplayBuff != null && !DisplayBuff.IsExpired &&
        (DisplayBuff.target == null || DisplayBuff.target.kind != BuffTargetKind.Target ||
         DisplayBuff.target.targetObject != null);

    internal EffectBuffUIHandle(BuffManager manager, ActiveBuff displayBuff, ItemEffectContext context)
    {
        this.manager = manager;
        DisplayBuff = displayBuff;
        this.context = context;
    }

    // Only the owning effect advances this clock, including pauses and refreshes.
    public void SetRemaining(float remaining)
    {
        if (manager == null) return;
        DisplayBuff.remainTime = EffectStatUtility.Safe(remaining, 0f, DisplayBuff.maxTime, 0f);
    }

    public void Dispose()
    {
        BuffManager owner = manager;
        Invalidate();
        if (owner != null) owner.RemoveEffectUI(this);
    }

    internal void Invalidate()
    {
        manager = null;
        if (DisplayBuff != null)
        {
            DisplayBuff.remainTime = 0f;
            DisplayBuff.uiEnded = true;
        }
    }
}
