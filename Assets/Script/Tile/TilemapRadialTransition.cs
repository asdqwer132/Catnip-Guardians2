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

    [Header("Decoration Tilemaps")]
    [Tooltip("기본 타일맵 아래의 자식 타일맵도 같은 원형 전환으로 처리합니다.")]
    [SerializeField] private bool includeChildTilemaps = true;

    [Tooltip("자식이 아닌 데코 타일맵은 여기에 연결합니다. 자식 자동 수집과 중복되어도 한 번만 처리합니다.")]
    [SerializeField] private Tilemap[] decorationTilemaps;

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
    private readonly List<Tilemap> cachedTilemaps = new();

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
        public Tilemap tilemap;
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

    // 기본 타일맵과 연결된 데코 타일맵 전체에 적용합니다.
    public void SetRenderersEnabled(bool enabled)
    {
        EnsureCached();

        for (int i = 0; i < cachedTilemaps.Count; i++)
        {
            Tilemap tilemap = cachedTilemaps[i];

            if (tilemap == null)
                continue;

            TilemapRenderer tilemapRenderer =
                tilemap.GetComponent<TilemapRenderer>();

            if (tilemapRenderer != null)
                tilemapRenderer.enabled = enabled;
        }
    }

    // 이전 바닥을 남겨 두더라도 이전 데코가 위에 남지 않게 합니다.
    public void SetDecorationRenderersEnabled(bool enabled)
    {
        EnsureCached();

        for (int i = 0; i < cachedTilemaps.Count; i++)
        {
            Tilemap tilemap = cachedTilemaps[i];

            if (tilemap == null || tilemap == targetTilemap)
                continue;

            TilemapRenderer tilemapRenderer =
                tilemap.GetComponent<TilemapRenderer>();

            if (tilemapRenderer != null)
                tilemapRenderer.enabled = enabled;
        }
    }

    public void SetCollidersEnabled(bool enabled)
    {
        EnsureCached();

        for (int i = 0; i < cachedTilemaps.Count; i++)
        {
            Tilemap tilemap = cachedTilemaps[i];

            if (tilemap == null)
                continue;

            TilemapCollider2D tilemapCollider =
                tilemap.GetComponent<TilemapCollider2D>();

            if (tilemapCollider != null)
                tilemapCollider.enabled = enabled;
        }
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
            targetTilemap = GetComponent<Tilemap>();

        if (targetTilemap == null)
            return;

        cellDatas.Clear();
        cachedTilemaps.Clear();

        AddTilemap(targetTilemap);

        if (includeChildTilemaps)
        {
            Tilemap[] childTilemaps =
                targetTilemap.GetComponentsInChildren<Tilemap>(true);

            for (int i = 0; i < childTilemaps.Length; i++)
                AddTilemap(childTilemaps[i]);
        }

        if (decorationTilemaps != null)
        {
            for (int i = 0; i < decorationTilemaps.Length; i++)
                AddTilemap(decorationTilemaps[i]);
        }

        for (int i = 0; i < cachedTilemaps.Count; i++)
            CacheTilemapCells(cachedTilemaps[i]);

        isCached = true;
    }

    private void AddTilemap(Tilemap tilemap)
    {
        if (tilemap == null || cachedTilemaps.Contains(tilemap))
            return;

        cachedTilemaps.Add(tilemap);
    }

    private void CacheTilemapCells(Tilemap tilemap)
    {
        BoundsInt cellBounds = tilemap.cellBounds;

        foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cellPosition))
                continue;

            Color originalColor = tilemap.GetColor(cellPosition);
            TileFlags originalFlags = tilemap.GetTileFlags(cellPosition);

            // 색상 잠금만 해제하고 타일의 회전/반전 등 다른 플래그는 유지합니다.
            tilemap.SetTileFlags(
                cellPosition,
                originalFlags & ~TileFlags.LockColor
            );

            cellDatas.Add(new CellData
            {
                tilemap = tilemap,
                cellPosition = cellPosition,
                originalColor = originalColor,
                originalFlags = originalFlags,
                distance = 0f,
                lastAlpha = -1f
            });
        }
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

            if (cellData.tilemap == null)
                continue;

            Vector3 cellWorldPosition =
                cellData.tilemap.GetCellCenterWorld(
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

            if (cellData.tilemap == null)
                continue;

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

            // 완전히 숨김/표시되는 끝 상태는 작은 차이여도 반드시 반영합니다.
            if (alpha == cellData.lastAlpha ||
                (alpha > 0f && alpha < 1f &&
                 Mathf.Abs(alpha - cellData.lastAlpha) < 0.01f))
                continue;

            Color tileColor = cellData.originalColor;
            tileColor.a = cellData.originalColor.a * alpha;

            cellData.tilemap.SetColor(
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
        for (int i = 0; i < cellDatas.Count; i++)
        {
            CellData cellData = cellDatas[i];

            if (cellData.tilemap == null ||
                !cellData.tilemap.HasTile(cellData.cellPosition))
                continue;

            cellData.tilemap.SetColor(
                cellData.cellPosition,
                cellData.originalColor
            );

            cellData.tilemap.SetTileFlags(
                cellData.cellPosition,
                cellData.originalFlags
            );
        }
    }
}
