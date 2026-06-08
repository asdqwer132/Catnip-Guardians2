using UnityEngine;

public class PlayerRangeIndicatorController : MonoBehaviour
{
    [Header("Root")]
    public GameObject indicatorRoot;

    [Header("Indicators")]
    public Transform minRangeIndicator;
    public Transform maxRangeIndicator;

    [Header("Sprite Setting")]
    public float baseSpriteDiameter = 1f;

    [Header("Option")]
    public bool showOnStart = false;
    public bool refreshOnEnable = true;

    private bool isVisible;

    private void Start()
    {
        if(!showOnStart) Hide();
    }

    public void Show(float max, float min)
    {
        SetVisible(true);
        RefreshRange(max, min);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Toggle(float max, float min)
    {
        SetVisible(!isVisible);

        if (isVisible)
            RefreshRange(max, min);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (indicatorRoot != null)
            indicatorRoot.SetActive(visible);
        return;
    }

    public void RefreshRange(float max, float min)
    {
        ApplyRangeScale(minRangeIndicator, min);
        ApplyRangeScale(maxRangeIndicator, max);
    }

    private void ApplyRangeScale(Transform indicator, float radius)
    {
        if (indicator == null)
            return;

        radius = Mathf.Max(0f, radius);

        float diameter = radius * 2f;
        float scale = baseSpriteDiameter <= 0f ? diameter : diameter / baseSpriteDiameter;

        indicator.localScale = new Vector3(scale, scale, 1f);
    }
}