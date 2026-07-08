using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapRadialTransition : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Tilemap targetTilemap;

    [Tooltip("원이 시작되는 위치입니다. 비어 있으면 이 타일맵의 Transform 위치를 사용합니다.")]
    [SerializeField] private Transform centerTransform;

    [Header("Transition")]
    [Min(0.01f)]
    [SerializeField] private float duration = 1.2f;

    [Tooltip("원의 경계에서 몇 월드 단위에 걸쳐 타일이 서서히 나타날지 설정합니다.")]
    [Min(0f)]
    [SerializeField] private float edgeWidth = 1.5f;

    [SerializeField]
    private AnimationCurve radiusCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Events")]
    public UnityEvent onRevealComplete;
    public UnityEvent onHideComplete;

    private readonly List<CellData> cellDatas = new();

    private Coroutine transitionCoroutine;

    private Vector3 currentCenter;
    private float currentRadius;
    private float maxRadius;
    private float hiddenRadius;

    private bool isCached;

    public bool IsPlaying => transitionCoroutine != null;
    public int CachedTileCount => cellDatas.Count;
    public Tilemap TargetTilemap => targetTilemap;

    private struct CellData
    {
        public Vector3Int cellPosition;
        public Color originalColor;
        public TileFlags originalFlags;
        public float distance;
        public float lastAlpha;
    }

    private void Awake()
    {
        if (targetTilemap == null)
            targetTilemap = GetComponent<Tilemap>();

        EnsureCached();
        RecalculateDistances(GetDefaultCenter());

        if (hideOnAwake)
            SetHiddenImmediately();
        else
            SetVisibleImmediately();
    }

    public void PlayReveal()
    {
        PlayRevealFrom(GetDefaultCenter());
    }

    public void PlayRevealFrom(Vector3 worldPosition)
    {
        EnsureCached();
        StopCurrentTransition();

        RecalculateDistances(worldPosition);
        SetRadius(hiddenRadius);

        transitionCoroutine = StartCoroutine(
            TransitionRoutine(
                hiddenRadius,
                maxRadius,
                true
            )
        );
    }

    public void PlayHide()
    {
        PlayHideFrom(GetDefaultCenter());
    }

    public void PlayHideFrom(Vector3 worldPosition)
    {
        EnsureCached();
        StopCurrentTransition();

        RecalculateDistances(worldPosition);
        SetRadius(maxRadius);

        transitionCoroutine = StartCoroutine(
            TransitionRoutine(
                maxRadius,
                hiddenRadius,
                false
            )
        );
    }

    public void SetVisibleImmediately()
    {
        StopCurrentTransition();
        EnsureCached();

        RecalculateDistances(GetDefaultCenter());
        SetRadius(maxRadius);
    }

    public void SetHiddenImmediately()
    {
        StopCurrentTransition();
        EnsureCached();

        RecalculateDistances(GetDefaultCenter());
        SetRadius(hiddenRadius);
    }

    public void StopTransition()
    {
        StopCurrentTransition();
    }

    private IEnumerator TransitionRoutine(
        float startRadius,
        float endRadius,
        bool isReveal
    )
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            elapsedTime += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / duration);

            float curvedTime =
                radiusCurve.Evaluate(normalizedTime);

            float radius = Mathf.Lerp(
                startRadius,
                endRadius,
                curvedTime
            );

            SetRadius(radius);

            yield return null;
        }

        SetRadius(endRadius);
        transitionCoroutine = null;

        if (isReveal)
            onRevealComplete?.Invoke();
        else
            onHideComplete?.Invoke();
    }

    private void CacheTiles()
    {
        if (isCached)
            return;

        if (targetTilemap == null)
            return;

        cellDatas.Clear();

        BoundsInt cellBounds = targetTilemap.cellBounds;

        foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
        {
            if (!targetTilemap.HasTile(cellPosition))
                continue;

            Color originalColor =
                targetTilemap.GetColor(cellPosition);

            TileFlags originalFlags =
                targetTilemap.GetTileFlags(cellPosition);

            targetTilemap.SetTileFlags(
                cellPosition,
                TileFlags.None
            );

            CellData cellData = new CellData
            {
                cellPosition = cellPosition,
                originalColor = originalColor,
                originalFlags = originalFlags,
                distance = 0f,
                lastAlpha = -1f
            };

            cellDatas.Add(cellData);
        }

        isCached = true;
    }

    private void RecalculateDistances(Vector3 worldCenter)
    {
        if (targetTilemap == null)
            return;

        currentCenter = worldCenter;
        maxRadius = 0f;

        for (int i = 0; i < cellDatas.Count; i++)
        {
            CellData cellData = cellDatas[i];

            Vector3 cellWorldPosition =
                targetTilemap.GetCellCenterWorld(
                    cellData.cellPosition
                );

            cellData.distance = Vector2.Distance(
                currentCenter,
                cellWorldPosition
            );

            if (cellData.distance > maxRadius)
                maxRadius = cellData.distance;

            cellDatas[i] = cellData;
        }

        hiddenRadius = -Mathf.Max(edgeWidth, 0.01f);

        maxRadius += Mathf.Max(
            edgeWidth,
            targetTilemap.cellSize.magnitude
        );
    }

    private void SetRadius(float radius)
    {
        if (targetTilemap == null)
            return;

        currentRadius = radius;

        for (int i = 0; i < cellDatas.Count; i++)
        {
            CellData cellData = cellDatas[i];

            float alpha;

            if (edgeWidth <= 0f)
            {
                alpha = radius >= cellData.distance ? 1f : 0f;
            }
            else
            {
                alpha = Mathf.InverseLerp(
                    cellData.distance - edgeWidth,
                    cellData.distance,
                    radius
                );

                alpha = Mathf.SmoothStep(0f, 1f, alpha);
            }

            if (Mathf.Abs(alpha - cellData.lastAlpha) < 0.01f)
                continue;

            Color tileColor = cellData.originalColor;
            tileColor.a = cellData.originalColor.a * alpha;

            targetTilemap.SetColor(
                cellData.cellPosition,
                tileColor
            );

            cellData.lastAlpha = alpha;
            cellDatas[i] = cellData;
        }
    }

    private Vector3 GetDefaultCenter()
    {
        if (centerTransform != null)
            return centerTransform.position;

        if (targetTilemap != null)
            return targetTilemap.transform.position;

        return transform.position;
    }

    private void StopCurrentTransition()
    {
        if (transitionCoroutine == null)
            return;

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }

    private void EnsureCached()
    {
        if (!isCached)
            CacheTiles();
    }

    private void OnDestroy()
    {
        if (targetTilemap == null)
            return;

        for (int i = 0; i < cellDatas.Count; i++)
        {
            CellData cellData = cellDatas[i];

            if (!targetTilemap.HasTile(cellData.cellPosition))
                continue;

            targetTilemap.SetTileFlags(
                cellData.cellPosition,
                cellData.originalFlags
            );
        }
    }
}