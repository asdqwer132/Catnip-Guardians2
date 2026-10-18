using UnityEngine;
using UnityEngine.UI;

// 배치 비율은 씬이 정하고, UI 한 단위의 화면 픽셀 크기는 이 공통 데이터가 정합니다.
[CreateAssetMenu(menuName = "00.Game/Scene UI Scale Profile", fileName = "SceneUIScaleProfile")]
public sealed class SceneUIScaleProfile : ScriptableObject
{
    public Vector2 referenceResolution = new Vector2(2400, 1440);
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    [Range(0f, 1f)] public float matchWidthOrHeight = 1f;
    [Min(1f)] public float referencePixelsPerUnit = 100f;

    public float CalculateScale(Vector2 outputPixels)
        => CalculateScale(outputPixels, referenceResolution, screenMatchMode, matchWidthOrHeight);

    public static float CalculateScale(Vector2 outputPixels, Vector2 referenceResolution,
        CanvasScaler.ScreenMatchMode screenMatchMode, float matchWidthOrHeight)
    {
        Vector2 reference = new Vector2(SafePositive(referenceResolution.x, 2400), SafePositive(referenceResolution.y, 1440));
        float x = SafePositive(outputPixels.x, reference.x) / reference.x;
        float y = SafePositive(outputPixels.y, reference.y) / reference.y;
        float match = float.IsNaN(matchWidthOrHeight) ? 0f : Mathf.Clamp01(matchWidthOrHeight);
        return screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(x, y) :
            screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(x, y) :
            SceneUILayout.CanvasScale(new Vector2(x, y), Vector2.one, match);
    }
    public float ReferencePixelsPerUnit => SafePositive(referencePixelsPerUnit, 100);
    private static float SafePositive(float value, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? fallback : value;
}
