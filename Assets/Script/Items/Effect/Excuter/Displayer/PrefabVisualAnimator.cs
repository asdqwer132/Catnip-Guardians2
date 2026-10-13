using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Catnip/Visuals/Prefab Visual Animator")]
public sealed class PrefabVisualAnimator : MonoBehaviour, IPoolable
{
    [Header("연출 대상")]
    [Tooltip("이 프리팹의 자식 Visual을 지정하세요. 충돌/범위 크기를 제어하는 Transform과 분리합니다.")]
    public Transform visualTarget;
    [Tooltip("비우면 부모에서 DamageArea/Area/Summon의 수명을 자동으로 찾습니다.")]
    public MonoBehaviour lifetimeSource;
    public bool playOnEnable = true;
    [Tooltip("런타임 수명이 없는 프리팹에 사용합니다. 0이면 유지 연출을 계속 재생합니다.")]
    [Min(0f)] public float manualLifetime;
    [Header("등장 · 크기와 투명도")]
    public PrefabVisualEntrance entrance = new PrefabVisualEntrance();
    [Header("유지 · 회전/맥동/흔들림")]
    public PrefabVisualLoop loop = new PrefabVisualLoop();
    [Header("종료 · 크기와 투명도")]
    public PrefabVisualExit exit = new PrefabVisualExit();
    [Header("명시적 종료 완료")]
    public UnityEvent onExitCompleted = new UnityEvent();

    private Transform boundTarget;
    private Vector3 originalPosition, originalScale;
    private Quaternion originalRotation;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private IPrefabVisualLifetime source;
    private MonoBehaviour sourceComponent;
    private float elapsed, exitElapsed;
    private bool captured, playing, pendingPlay, exiting, exitCompleted;
    public bool IsPlaying => playing;

    private void OnEnable() { ResetPlayback(); }
    private void LateUpdate() => Advance(Time.deltaTime);
    public void OnSpawnedFromPool() { Restore(); ResetPlayback(); }
    public void OnReturnedToPool() => Stop();
    private void OnDisable() => Stop();
    private void ResetPlayback()
    {
        pendingPlay = playOnEnable; playing = exiting = exitCompleted = false;
        elapsed = exitElapsed = 0f;
    }

    // OnEnable 후 첫 LateUpdate에서 시작하므로 생성 코드의 범위/색 초기화가 먼저 끝납니다.
    public bool Play()
    {
        Restore();
        pendingPlay = playing = exiting = exitCompleted = false;
        ResolveSource();
        if (visualTarget == null) visualTarget = transform.Find("Visual");
        if (ConfigurationIssue != null) { pendingPlay = false; return false; }
        boundTarget = visualTarget;
        originalPosition = boundTarget.localPosition; originalRotation = boundTarget.localRotation;
        originalScale = boundTarget.localScale;
        renderers = boundTarget.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
        captured = playing = true; pendingPlay = exiting = exitCompleted = false;
        elapsed = exitElapsed = 0f;
        ApplyFrame(GetRemaining());
        return true;
    }

    public string ConfigurationIssue
    {
        get
        {
            Transform target = visualTarget != null ? visualTarget : transform.Find("Visual");
            if (target == null) return "자식 Visual을 만들고 Visual Target에 연결하세요.";
            if (target == transform || !target.IsChildOf(transform)) return "이 프리팹의 자식 Transform만 연출 대상으로 사용할 수 있습니다.";
            if (target.GetComponentsInChildren<Collider2D>(true).Length > 0)
                return "연출 대상 안에 Collider2D가 있습니다. 판정용 오브젝트와 Visual을 분리하세요.";
            foreach (MonoBehaviour parent in GetComponentsInParent<MonoBehaviour>(true))
                if (parent is IPrefabVisualLifetime lifetime && ControlsAnimatedRange(lifetime, target))
                    return "범위 크기를 제어하는 Transform 아래에 Visual을 만들고 그 자식을 지정하세요.";
            if (lifetimeSource != null && !(lifetimeSource is IPrefabVisualLifetime))
                return "Lifetime Source는 IPrefabVisualLifetime을 구현해야 합니다.";
            if (lifetimeSource is IPrefabVisualLifetime configured && ControlsAnimatedRange(configured, target))
                return "연출 대상 안에 범위 크기를 제어하는 Transform이 있습니다. Visual을 별도 자식으로 분리하세요.";
            return null;
        }
    }
    private static bool ControlsAnimatedRange(IPrefabVisualLifetime lifetime, Transform target)
    {
        Transform controlled = lifetime.ControlledRangeVisual;
        return controlled != null && (controlled == target || controlled.IsChildOf(target));
    }
    private void ResolveSource()
    {
        source = null; sourceComponent = null;
        if (lifetimeSource != null)
        { source = lifetimeSource as IPrefabVisualLifetime; sourceComponent = lifetimeSource; return; }
        foreach (MonoBehaviour parent in GetComponentsInParent<MonoBehaviour>(true))
            if (parent is IPrefabVisualLifetime lifetime)
            { source = lifetime; sourceComponent = parent; break; }
    }
    private float GetRemaining()
    {
        float remaining;
        if (sourceComponent != null && source != null && source.TryGetVisualLifetime(out remaining))
            return Mathf.Max(0f, remaining);
        float lifetime = EffectStatUtility.Safe(manualLifetime, 0f, 600f, 0f);
        return lifetime > 0f ? Mathf.Max(0f, lifetime - elapsed) : float.PositiveInfinity;
    }
    public void Advance(float deltaTime)
    {
        if (!isActiveAndEnabled) return;
        if (pendingPlay && !Play()) return;
        if (!playing || boundTarget == null) return;
        float step = EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
        elapsed += step;
        if (exiting) exitElapsed += step;
        float remaining = GetRemaining();
        ApplyFrame(remaining);
        if (exiting && !exitCompleted && (exit == null || !exit.enabled || exitElapsed >= PrefabVisualAnimationMath.Duration(exit.duration)))
        {
            exitCompleted = true; playing = false;
            onExitCompleted?.Invoke(); // 사용자가 필요할 때만 후속 처리를 연결합니다.
        }
        else if (!exiting && source == null && manualLifetime > 0f && remaining <= 0f) playing = false;
    }
    private void ApplyFrame(float remaining)
    {
        if (!captured || boundTarget == null) return;
        PrefabVisualFrame frame = PrefabVisualAnimationMath.Evaluate(entrance, loop, exit, elapsed, remaining,
            exiting ? exitElapsed : -1f);
        boundTarget.localPosition = originalPosition + frame.offset;
        boundTarget.localRotation = originalRotation * Quaternion.Euler(0f, 0f, frame.rotationDegrees);
        boundTarget.localScale = Vector3.Scale(originalScale, frame.scaleMultiplier);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
            { Color color = originalColors[i]; color.a *= frame.alphaMultiplier; renderers[i].color = color; }
    }
    public void PlayExit()
    {
        if (exiting) return;
        if (!playing && !Play()) return;
        pendingPlay = false; exiting = true; exitCompleted = false; exitElapsed = 0f;
        ApplyFrame(GetRemaining());
    }
    public void Stop()
    {
        pendingPlay = playing = exiting = exitCompleted = false;
        Restore(); source = null; sourceComponent = null;
    }
    private void Restore()
    {
        if (!captured) return;
        if (boundTarget != null)
        {
            boundTarget.localPosition = originalPosition; boundTarget.localRotation = originalRotation;
            boundTarget.localScale = originalScale;
        }
        if (renderers != null)
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].color = originalColors[i];
        captured = false; boundTarget = null;
    }
}
