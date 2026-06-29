using System.Collections.Generic;
using UnityEngine;

public enum OffscreenIndicatorSpriteMode
{
    PrefabDefault,
    ByRegisterOrder,
    Random
}

public class OffscreenTargetIndicatorManager : MonoBehaviour
{
    public static OffscreenTargetIndicatorManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private OffscreenTargetIndicatorUI indicatorPrefab;

    [Header("Sprite")]
    [SerializeField] private OffscreenIndicatorSpriteMode spriteMode = OffscreenIndicatorSpriteMode.PrefabDefault;
    [SerializeField] private Sprite[] indicatorSprites;

    [Header("Option")]
    [SerializeField] private float edgePadding = 60f;

    [Tooltip("타겟이 화면 안에 들어오면 인디케이터를 숨길지")]
    [SerializeField] private bool hideWhenTargetVisible = true;

    [Tooltip("인디케이터가 타겟 방향으로 회전할지")]
    [SerializeField] private bool useRotation = true;

    [Tooltip("화살표 이미지가 위쪽을 보고 있으면 -90, 오른쪽을 보고 있으면 0")]
    [SerializeField] private float indicatorAngleOffset = -90f;

    [Tooltip("타겟 위치보다 살짝 위를 가리키고 싶을 때 사용")]
    [SerializeField] private Vector3 targetWorldOffset = Vector3.zero;

    private readonly Dictionary<Transform, OffscreenTargetIndicatorUI> indicators =
        new Dictionary<Transform, OffscreenTargetIndicatorUI>();

    private readonly List<Transform> removeBuffer = new List<Transform>();

    private int registerCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || canvasRoot == null || indicatorPrefab == null)
            return;

        removeBuffer.Clear();

        foreach (var pair in indicators)
        {
            Transform target = pair.Key;
            OffscreenTargetIndicatorUI indicator = pair.Value;

            if (target == null || indicator == null)
            {
                removeBuffer.Add(target);
                continue;
            }

            UpdateIndicator(target, indicator);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            RemoveIndicator(removeBuffer[i]);
        }
    }

    public OffscreenTargetIndicatorUI ShowIndicator(GameObject targetObject)
    {
        if (targetObject == null)
            return null;

        return ShowIndicator(targetObject.transform);
    }

    public OffscreenTargetIndicatorUI ShowIndicator(Transform target)
    {
        return CreateOrGetIndicator(target, null);
    }

    public OffscreenTargetIndicatorUI ShowIndicator(GameObject targetObject, int spriteIndex)
    {
        if (targetObject == null)
            return null;

        return ShowIndicator(targetObject.transform, spriteIndex);
    }

    public OffscreenTargetIndicatorUI ShowIndicator(Transform target, int spriteIndex)
    {
        Sprite sprite = GetSpriteByIndex(spriteIndex);
        return CreateOrGetIndicator(target, sprite);
    }

    public OffscreenTargetIndicatorUI ShowIndicator(GameObject targetObject, Sprite sprite)
    {
        if (targetObject == null)
            return null;

        return ShowIndicator(targetObject.transform, sprite);
    }

    public OffscreenTargetIndicatorUI ShowIndicator(Transform target, Sprite sprite)
    {
        return CreateOrGetIndicator(target, sprite);
    }

    public void ShowIndicators(IEnumerable<GameObject> targetObjects)
    {
        if (targetObjects == null)
            return;

        foreach (GameObject targetObject in targetObjects)
        {
            ShowIndicator(targetObject);
        }
    }

    public void ShowIndicators(IEnumerable<Transform> targets)
    {
        if (targets == null)
            return;

        foreach (Transform target in targets)
        {
            ShowIndicator(target);
        }
    }

    public void HideIndicator(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        RemoveIndicator(targetObject.transform);
    }

    public void HideIndicator(Transform target)
    {
        RemoveIndicator(target);
    }

    public void ClearAll()
    {
        foreach (var pair in indicators)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        indicators.Clear();
        registerCount = 0;
    }

    public void SetUseRotation(bool value)
    {
        useRotation = value;
    }

    private OffscreenTargetIndicatorUI CreateOrGetIndicator(Transform target, Sprite overrideSprite)
    {
        if (target == null)
            return null;

        if (indicators.TryGetValue(target, out OffscreenTargetIndicatorUI existing))
        {
            if (overrideSprite != null)
                existing.SetSprite(overrideSprite);

            return existing;
        }

        OffscreenTargetIndicatorUI indicator = Instantiate(indicatorPrefab, canvasRoot);
        indicator.Init(target);

        RectTransform rect = indicator.RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Sprite selectedSprite = overrideSprite;

        if (selectedSprite == null)
            selectedSprite = GetAutoSprite();

        if (selectedSprite != null)
            indicator.SetSprite(selectedSprite);

        indicators.Add(target, indicator);
        registerCount++;

        return indicator;
    }

    private Sprite GetAutoSprite()
    {
        if (indicatorSprites == null || indicatorSprites.Length == 0)
            return null;

        switch (spriteMode)
        {
            case OffscreenIndicatorSpriteMode.ByRegisterOrder:
                return indicatorSprites[registerCount % indicatorSprites.Length];

            case OffscreenIndicatorSpriteMode.Random:
                return indicatorSprites[Random.Range(0, indicatorSprites.Length)];

            case OffscreenIndicatorSpriteMode.PrefabDefault:
            default:
                return null;
        }
    }

    private Sprite GetSpriteByIndex(int index)
    {
        if (indicatorSprites == null || indicatorSprites.Length == 0)
            return null;

        if (index < 0 || index >= indicatorSprites.Length)
            return null;

        return indicatorSprites[index];
    }

    private void RemoveIndicator(Transform target)
    {
        if (target == null)
        {
            indicators.Remove(target);
            return;
        }

        if (indicators.TryGetValue(target, out OffscreenTargetIndicatorUI indicator))
        {
            if (indicator != null)
                Destroy(indicator.gameObject);

            indicators.Remove(target);
        }
    }

    private void UpdateIndicator(Transform target, OffscreenTargetIndicatorUI indicator)
    {
        Vector3 worldPosition = target.position + targetWorldOffset;
        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(worldPosition);

        bool isVisible =
            viewportPosition.z > 0f &&
            viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f;

        if (isVisible && hideWhenTargetVisible)
        {
            indicator.SetVisible(false);
            return;
        }

        indicator.SetVisible(true);

        Vector2 direction = new Vector2(
            viewportPosition.x - 0.5f,
            viewportPosition.y - 0.5f
        );

        if (viewportPosition.z < 0f)
            direction = -direction;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;

        direction.Normalize();

        Vector2 anchoredPosition = GetEdgePosition(direction);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += indicatorAngleOffset;

        indicator.SetPositionAndRotation(anchoredPosition, angle, useRotation);
    }

    private Vector2 GetEdgePosition(Vector2 direction)
    {
        Rect rect = canvasRoot.rect;

        float halfWidth = rect.width * 0.5f - edgePadding;
        float halfHeight = rect.height * 0.5f - edgePadding;

        halfWidth = Mathf.Max(0f, halfWidth);
        halfHeight = Mathf.Max(0f, halfHeight);

        float scaleX = Mathf.Abs(direction.x) > 0.0001f
            ? halfWidth / Mathf.Abs(direction.x)
            : float.PositiveInfinity;

        float scaleY = Mathf.Abs(direction.y) > 0.0001f
            ? halfHeight / Mathf.Abs(direction.y)
            : float.PositiveInfinity;

        float scale = Mathf.Min(scaleX, scaleY);

        return direction * scale;
    }
}