using UnityEngine;

public class TargetRangeIndicator : MonoBehaviour
{
    [Header("Visual")]
    public Transform rangeVisual;

    [Header("Scale")]
    public float baseSpriteDiameter = 1f;

    void Awake()
    {
        if (rangeVisual == null)
            rangeVisual = transform;
    }

    public void SetRadius(float radius)
    {
        if (rangeVisual == null)
            rangeVisual = transform;

        radius = Mathf.Max(0.01f, radius);

        float diameter = radius * 2f;
        float scale = diameter / Mathf.Max(0.01f, baseSpriteDiameter);

        rangeVisual.localScale = new Vector3(
            scale,
            scale,
            1f
        );
    }
}