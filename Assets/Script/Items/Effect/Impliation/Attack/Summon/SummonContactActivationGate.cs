using System;
using System.Collections.Generic;

public enum SummonContactTriggerMode { OnEnter, WhileTouching }
public enum SummonContactLimitScope { PerSummon, PerTarget }
public enum SummonContactFirstHitTiming
{
    [UnityEngine.InspectorName("즉시")] Immediate = 0,
    [UnityEngine.InspectorName("인터벌 후")] AfterInterval = 1
}

// 물리 조회와 분리한 접촉/횟수 상태입니다. 공유 모듈 에셋에는 실행 상태를 저장하지 않습니다.
public sealed class SummonContactActivationGate
{
    private sealed class Contact
    {
        public int life, activations;
        public long lastSeen = -2, lastAttempt = -2;
        public double attemptTime;
        public bool inFlight;
    }
    private readonly Dictionary<object, Contact> contacts = new Dictionary<object, Contact>();
    private readonly List<object> removed = new List<object>();
    private readonly SummonContactTriggerMode mode;
    private readonly bool limited;
    private readonly SummonContactLimitScope scope;
    private readonly SummonContactFirstHitTiming firstHitTiming;
    private long step;
    private double elapsed;
    private int total, pending, version;
    public int ActivationCount => total;

    public SummonContactActivationGate(SummonContactTriggerMode mode, bool limited = false,
        SummonContactLimitScope scope = SummonContactLimitScope.PerSummon,
        SummonContactFirstHitTiming firstHitTiming = SummonContactFirstHitTiming.Immediate)
    { this.mode = mode; this.limited = limited; this.scope = scope; this.firstHitTiming = firstHitTiming; }

    public void BeginStep(float deltaTime)
    {
        step++;
        elapsed += EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
    }
    public bool TryActivate(object target, int lifeId, float interval, int maximum, Func<bool> action)
    {
        if (target == null || action == null) return false;
        Contact state;
        if (!contacts.TryGetValue(target, out state) || state.life != lifeId)
            contacts[target] = state = new Contact { life = lifeId };
        bool alreadyInside = state.lastSeen == step || state.lastSeen == step - 1;
        state.lastSeen = step;
        // 인터벌 후 모드는 매 진입부터 한 주기를 기다립니다. 같은 적의 생명이 바뀌어도 새 접촉입니다.
        bool waitOnEntry = mode == SummonContactTriggerMode.WhileTouching &&
            firstHitTiming == SummonContactFirstHitTiming.AfterInterval;
        if (waitOnEntry && !alreadyInside) state.attemptTime = elapsed;
        maximum = Math.Max(1, maximum);
        if (state.inFlight || state.lastAttempt == step ||
            (mode == SummonContactTriggerMode.OnEnter && alreadyInside) ||
            (mode == SummonContactTriggerMode.WhileTouching && (state.lastAttempt >= 0 || waitOnEntry) &&
                elapsed - state.attemptTime + 0.000001d < EffectStatUtility.Safe(interval, 0.01f, 600f, 0.5f)) ||
            (limited && (scope == SummonContactLimitScope.PerTarget ? state.activations : total + pending) >= maximum))
            return false;
        state.lastAttempt = step;
        state.attemptTime = elapsed;
        state.inFlight = true;
        pending++;
        int currentVersion = version;
        bool success = false;
        try { success = action(); return success; }
        finally
        {
            // 콜백이 소환물/모듈을 제거해도 새 실행 상태를 건드리지 않습니다.
            if (version == currentVersion)
            {
                pending--;
                state.inFlight = false;
                if (success) { state.activations++; total++; }
            }
        }
    }
    public void EndStep(float interval, Func<object, int, bool> isValid = null)
    {
        removed.Clear();
        foreach (KeyValuePair<object, Contact> pair in contacts)
        {
            Contact state = pair.Value;
            bool noLongerNeeded = state.lastSeen != step && !(limited && scope == SummonContactLimitScope.PerTarget) &&
                (mode == SummonContactTriggerMode.OnEnter ||
                    elapsed - state.attemptTime >= EffectStatUtility.Safe(interval, 0.01f, 600f, 0.5f));
            if ((isValid != null && !isValid(pair.Key, state.life)) || noLongerNeeded) removed.Add(pair.Key);
        }
        foreach (object target in removed) contacts.Remove(target);
    }
    public void Clear() { version++; contacts.Clear(); removed.Clear(); total = pending = 0; }
}
