using System;
using UnityEngine;

public enum IndicatorSpriteSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

[Serializable]
public class IndicatorSpriteSet
{
    public IndicatorSpriteSize size;

    [Header("Sprite")]
    public Sprite sprite;

    [Header("Visual Size")]
    [Tooltip("이 값이 클수록 같은 공격 범위여도 인디케이터 이미지가 더 크게 보입니다.")]
    public float sizeMultiplier = 1f;

    [Tooltip("스프라이트 원본 기준 지름입니다. 보통 원형 스프라이트가 1유닛 크기면 1로 둡니다.")]
    public float baseSpriteDiameter = 1f;
}

public class TargetRangeIndicator : MonoBehaviour
{
    [Header("Visual")]
    public Transform rangeVisual;
    public SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    public IndicatorSpriteSet[] indicatorSprites;

    private float currentRadius = 1f;
    private IndicatorSpriteSet currentSpriteSet;

    private void Awake()
    {
        if (rangeVisual == null)
            rangeVisual = transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ApplySettingSprite();
        ApplyRadiusScale();
    }

    public void SetRadius(float radius)
    {
        currentRadius = Mathf.Max(0.01f, radius);

        ApplySettingSprite();
        ApplyRadiusScale();
    }

    public void ApplySettingSprite()
    {
        IndicatorSpriteSize size = IndicatorSpriteSize.Medium;

        if (SettingManager.instance != null)
            size = SettingManager.instance.GetIndicatorSpriteSize();

        SetIndicatorSprite(size);
    }

    public void SetIndicatorSprite(IndicatorSpriteSize size)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        IndicatorSpriteSet set = GetSpriteSet(size);

        if (set == null)
            return;

        currentSpriteSet = set;

        if (set.sprite != null)
            spriteRenderer.sprite = set.sprite;

        ApplyRadiusScale();
    }

    private IndicatorSpriteSet GetSpriteSet(IndicatorSpriteSize size)
    {
        if (indicatorSprites == null)
            return null;

        for (int i = 0; i < indicatorSprites.Length; i++)
        {
            if (indicatorSprites[i] == null)
                continue;

            if (indicatorSprites[i].size == size)
                return indicatorSprites[i];
        }

        return null;
    }

    private void ApplyRadiusScale()
    {
        if (rangeVisual == null)
            rangeVisual = transform;

        float radius = Mathf.Max(0.01f, currentRadius);
        float diameter = radius * 2f;

        float baseDiameter = 1f;
        float sizeMultiplier = 1f;

        if (currentSpriteSet != null)
        {
            baseDiameter = Mathf.Max(0.01f, currentSpriteSet.baseSpriteDiameter);
            sizeMultiplier = Mathf.Max(0.01f, currentSpriteSet.sizeMultiplier);
        }

        float scale = diameter / baseDiameter;
        scale *= sizeMultiplier;

        rangeVisual.localScale = new Vector3(
            scale,
            scale,
            1f
        );
    }
}