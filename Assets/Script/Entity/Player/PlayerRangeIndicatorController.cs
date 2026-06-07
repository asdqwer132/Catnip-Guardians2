using UnityEngine;

public class PlayerRangeIndicatorController : MonoBehaviour
{
    [Header("Reference")]
    public Player player;

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

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();

        SetVisible(showOnStart);
        RefreshRange();
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
            RefreshRange();
    }

    public void Show()
    {
        SetVisible(true);
        RefreshRange();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(!isVisible);

        if (isVisible)
            RefreshRange();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (indicatorRoot != null)
            indicatorRoot.SetActive(visible);
        return;
    }

    public void RefreshRange()
    {
        if (player == null)
            return;

        ApplyRangeScale(minRangeIndicator, player.MinRange);
        ApplyRangeScale(maxRangeIndicator, player.MaxRange);
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