using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class TilemapRadialSequenceController : MonoBehaviour
{
    [Header("Tilemaps")]
    [Tooltip("0번은 처음 보이는 기본 맵입니다. 이후 배열 순서대로 나타납니다.")]
    [SerializeField]
    private TilemapRadialTransition[] tilemapTransitions;

    [Header("Center")]
    [Tooltip("모든 맵이 같은 위치에서 퍼지게 할 경우 지정합니다.")]
    [SerializeField]
    private Transform sharedCenter;

    [Header("Sequence")]
    [FormerlySerializedAs("initializeOnAwake")]
    [SerializeField]
    private bool initializeOnStart = true;

    [SerializeField]
    private bool autoPlayOnStart = false;

    [Min(0f)]
    [SerializeField]
    private float delayBetweenTransitions = 0.2f;

    [SerializeField]
    private bool useUnscaledTime = false;

    [Header("Previous Map")]
    [Tooltip("전환 완료 후 이전 맵의 TilemapRenderer를 끕니다.")]
    [SerializeField]
    private bool disablePreviousRenderer = false;

    [Tooltip("전환 완료 후 이전 맵의 TilemapCollider2D를 끕니다.")]
    [SerializeField]
    private bool disablePreviousCollider = true;

    [Header("Events")]
    public UnityEvent<int> onMapChanged;
    public UnityEvent onSequenceComplete;

    private int currentMapIndex = -1;
    private Coroutine sequenceCoroutine;

    public int CurrentMapIndex => currentMapIndex;
    public bool IsPlaying => sequenceCoroutine != null;

    public int MapCount
    {
        get
        {
            if (tilemapTransitions == null)
                return 0;

            return tilemapTransitions.Length;
        }
    }

    private void Start()
    {
        if (initializeOnStart)
            InitializeSequence();

        if (autoPlayOnStart)
            PlayAll();
    }

    public void InitializeSequence()
    {
        StopSequence();

        if (tilemapTransitions == null ||
            tilemapTransitions.Length == 0)
        {
            currentMapIndex = -1;
            return;
        }

        for (int i = 0; i < tilemapTransitions.Length; i++)
        {
            TilemapRadialTransition transition =
                tilemapTransitions[i];

            if (transition == null)
            {
                Debug.LogWarning(
                    $"[Tilemap Sequence] Element {i}가 비어 있습니다.",
                    this
                );

                continue;
            }

            transition.StopTransition();

            SetRendererEnabled(
                transition,
                true
            );

            if (i == 0)
            {
                transition.SetVisibleImmediately();

                SetColliderEnabled(
                    transition,
                    true
                );
            }
            else
            {
                transition.SetHiddenImmediately();

                SetColliderEnabled(
                    transition,
                    false
                );
            }
        }

        currentMapIndex = 0;
        onMapChanged?.Invoke(currentMapIndex);
    }

    public void PlayNext()
    {
        if (sequenceCoroutine != null)
            return;

        if (!HasNextMap())
            return;

        sequenceCoroutine = StartCoroutine(
            PlayNextRoutine()
        );
    }

    public void PlayAll()
    {
        if (sequenceCoroutine != null)
            return;

        if (!HasNextMap())
            return;

        sequenceCoroutine = StartCoroutine(
            PlayAllRoutine()
        );
    }

    public void PlayToIndex(int targetIndex)
    {
        if (sequenceCoroutine != null)
            return;

        if (tilemapTransitions == null ||
            tilemapTransitions.Length == 0)
        {
            return;
        }

        targetIndex = Mathf.Clamp(
            targetIndex,
            0,
            tilemapTransitions.Length - 1
        );

        if (targetIndex <= currentMapIndex)
            return;

        sequenceCoroutine = StartCoroutine(
            PlayToIndexRoutine(targetIndex)
        );
    }

    public void ResetSequence()
    {
        InitializeSequence();
    }

    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (tilemapTransitions == null)
            return;

        foreach (TilemapRadialTransition transition in tilemapTransitions)
        {
            if (transition != null)
                transition.StopTransition();
        }
    }

    private IEnumerator PlayNextRoutine()
    {
        yield return RevealNextMap();

        sequenceCoroutine = null;

        if (!HasNextMap())
            onSequenceComplete?.Invoke();
    }

    private IEnumerator PlayAllRoutine()
    {
        while (HasNextMap())
        {
            yield return RevealNextMap();

            if (HasNextMap() &&
                delayBetweenTransitions > 0f)
            {
                yield return WaitDelay(
                    delayBetweenTransitions
                );
            }
        }

        sequenceCoroutine = null;
        onSequenceComplete?.Invoke();
    }

    private IEnumerator PlayToIndexRoutine(int targetIndex)
    {
        while (currentMapIndex < targetIndex)
        {
            yield return RevealNextMap();

            if (currentMapIndex < targetIndex &&
                delayBetweenTransitions > 0f)
            {
                yield return WaitDelay(
                    delayBetweenTransitions
                );
            }
        }

        sequenceCoroutine = null;

        if (!HasNextMap())
            onSequenceComplete?.Invoke();
    }

    private IEnumerator RevealNextMap()
    {
        if (tilemapTransitions == null)
            yield break;

        int nextIndex = currentMapIndex + 1;

        if (nextIndex < 0 ||
            nextIndex >= tilemapTransitions.Length)
        {
            yield break;
        }

        TilemapRadialTransition nextTransition =
            tilemapTransitions[nextIndex];

        if (nextTransition == null)
        {
            Debug.LogError(
                $"[Tilemap Sequence] Element {nextIndex}가 비어 있습니다.",
                this
            );

            yield break;
        }

        if (nextTransition.CachedTileCount == 0)
        {
            Debug.LogWarning(
                $"[Tilemap Sequence] " +
                $"{nextIndex}번 맵의 캐싱된 타일 개수가 0입니다. " +
                $"오브젝트: {nextTransition.name}",
                nextTransition
            );
        }

        SetRendererEnabled(
            nextTransition,
            true
        );

        SetColliderEnabled(
            nextTransition,
            false
        );

        nextTransition.SetHiddenImmediately();

        if (sharedCenter != null)
        {
            nextTransition.PlayRevealFrom(
                sharedCenter.position
            );
        }
        else
        {
            nextTransition.PlayReveal();
        }

        yield return new WaitUntil(
            () => nextTransition == null ||
                  !nextTransition.IsPlaying
        );

        int previousIndex = currentMapIndex;
        currentMapIndex = nextIndex;

        SetColliderEnabled(
            nextTransition,
            true
        );

        if (previousIndex >= 0 &&
            previousIndex < tilemapTransitions.Length)
        {
            TilemapRadialTransition previousTransition =
                tilemapTransitions[previousIndex];

            if (previousTransition != null)
            {
                if (disablePreviousRenderer)
                {
                    SetRendererEnabled(
                        previousTransition,
                        false
                    );
                }

                if (disablePreviousCollider)
                {
                    SetColliderEnabled(
                        previousTransition,
                        false
                    );
                }
            }
        }

        onMapChanged?.Invoke(currentMapIndex);
    }

    private bool HasNextMap()
    {
        return tilemapTransitions != null &&
               currentMapIndex + 1 < tilemapTransitions.Length;
    }

    private IEnumerator WaitDelay(float delay)
    {
        if (useUnscaledTime)
        {
            float timer = 0f;

            while (timer < delay)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }
    }

    private void SetRendererEnabled(
        TilemapRadialTransition transition,
        bool enabled
    )
    {
        if (transition == null)
            return;

        TilemapRenderer tilemapRenderer =
            transition.GetComponent<TilemapRenderer>();

        if (tilemapRenderer != null)
            tilemapRenderer.enabled = enabled;
    }

    private void SetColliderEnabled(
        TilemapRadialTransition transition,
        bool enabled
    )
    {
        if (transition == null)
            return;

        TilemapCollider2D tilemapCollider =
            transition.GetComponent<TilemapCollider2D>();

        if (tilemapCollider != null)
            tilemapCollider.enabled = enabled;
    }
}