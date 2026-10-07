using UnityEngine;

public static class EffectStatUtility
{
    public static float Safe(float value, float min, float max, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    public static int Count(float value) => Mathf.Clamp(Mathf.RoundToInt(value), 1, 128);
}
