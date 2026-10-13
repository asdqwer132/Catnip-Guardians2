using System;
using UnityEngine;

[Serializable]
public sealed class PrefabVisualEntrance
{
    public bool enabled = true;
    [Min(0f)] public float duration = 0.15f;
    public Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
    [Range(0f, 1f)] public float startAlpha;
}

[Serializable]
public sealed class PrefabVisualLoop
{
    public bool rotate;
    public float rotationDegreesPerSecond = 90f;
    public bool pulse;
    [Range(0f, 0.95f)] public float pulseAmplitude = 0.05f;
    [Min(0f)] public float pulseFrequency = 1f;
    public bool sway;
    public Vector2 swayAmplitude = new Vector2(0.03f, 0.03f);
    [Min(0f)] public float swayFrequency = 1f;
}

[Serializable]
public sealed class PrefabVisualExit
{
    public bool enabled = true;
    [Min(0f)] public float duration = 0.15f;
    public Vector3 endScale = new Vector3(0.7f, 0.7f, 1f);
    [Range(0f, 1f)] public float endAlpha;
}

public struct PrefabVisualFrame
{
    public Vector3 offset, scaleMultiplier;
    public float rotationDegrees, alphaMultiplier;
}

// 항상 초기 상태에 곱할 결과를 계산합니다. 이전 프레임 결과를 다시 곱하지 않습니다.
public static class PrefabVisualAnimationMath
{
    public static PrefabVisualFrame Evaluate(PrefabVisualEntrance entrance, PrefabVisualLoop loop,
        PrefabVisualExit exit, float elapsed, float remaining, float explicitExitElapsed = -1f)
    {
        elapsed = EffectStatUtility.Safe(elapsed, 0f, 1000000f, 0f);
        var frame = new PrefabVisualFrame { scaleMultiplier = Vector3.one, alphaMultiplier = 1f };
        if (entrance != null && entrance.enabled)
        {
            float duration = Duration(entrance.duration);
            float progress = duration > 0f ? Smooth(elapsed / duration) : 1f;
            frame.scaleMultiplier = Vector3.Lerp(Scale(entrance.startScale), Vector3.one, progress);
            frame.alphaMultiplier = Mathf.Lerp(Alpha(entrance.startAlpha), 1f, progress);
        }
        if (loop != null)
        {
            if (loop.rotate) frame.rotationDegrees = Mathf.Repeat(elapsed *
                EffectStatUtility.Safe(loop.rotationDegreesPerSecond, -3600f, 3600f, 90f), 360f);
            if (loop.pulse)
            {
                float pulse = 1f + EffectStatUtility.Safe(loop.pulseAmplitude, 0f, 0.95f, 0f) *
                    Mathf.Sin(elapsed * EffectStatUtility.Safe(loop.pulseFrequency, 0f, 100f, 1f) * Mathf.PI * 2f);
                frame.scaleMultiplier = Vector3.Scale(frame.scaleMultiplier, new Vector3(pulse, pulse, 1f));
            }
            if (loop.sway)
            {
                float phase = elapsed * EffectStatUtility.Safe(loop.swayFrequency, 0f, 100f, 1f) * Mathf.PI * 2f;
                frame.offset = new Vector3(
                    EffectStatUtility.Safe(loop.swayAmplitude.x, -100f, 100f, 0f) * Mathf.Sin(phase),
                    EffectStatUtility.Safe(loop.swayAmplitude.y, -100f, 100f, 0f) * Mathf.Sin(phase * 0.73f), 0f);
            }
        }
        if (exit != null && exit.enabled)
        {
            float duration = Duration(exit.duration);
            float progress = explicitExitElapsed >= 0f
                ? (duration > 0f ? Smooth(explicitExitElapsed / duration) : 1f)
                : (duration > 0f && !float.IsNaN(remaining) ? Smooth(1f - remaining / duration) : 0f);
            frame.scaleMultiplier = Vector3.Scale(frame.scaleMultiplier,
                Vector3.Lerp(Vector3.one, Scale(exit.endScale), progress));
            frame.alphaMultiplier *= Mathf.Lerp(1f, Alpha(exit.endAlpha), progress);
        }
        return frame;
    }
    public static float Duration(float value) => EffectStatUtility.Safe(value, 0f, 600f, 0.15f);
    private static float Smooth(float value) { float t = Mathf.Clamp01(value); return t * t * (3f - 2f * t); }
    private static float Alpha(float value) => EffectStatUtility.Safe(value, 0f, 1f, 1f);
    private static Vector3 Scale(Vector3 value) => new Vector3(
        EffectStatUtility.Safe(value.x, 0f, 100f, 1f), EffectStatUtility.Safe(value.y, 0f, 100f, 1f),
        EffectStatUtility.Safe(value.z, 0f, 100f, 1f));
}
