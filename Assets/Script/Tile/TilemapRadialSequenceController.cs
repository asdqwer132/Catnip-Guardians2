using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField]
    private bool initializeOnAwake = true;

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

    private void Awake()
    {
        if (initializeOnAwake)
            InitializeSequence();
    }

    private void Start()
    {
        if (autoPlayOnStart)
            PlayAll();
    }

    /// <summary>
    /// 0번 맵만 보이게 하고 나머지 맵은 모두 숨깁니다.
    /// </summary>
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
                continue;

            transition.StopTransition();

            SetRendererEnabled(transition, true);

            if (i == 0)
            {
                transition.SetVisibleImmediately();
                SetColliderEnabled(transition, true);
            }
            else
            {
                transition.SetHiddenImmediately();
                SetColliderEnabled(transition, false);
            }
        }

        currentMapIndex = 0;
        onMapChanged?.Invoke(currentMapIndex);
    }

    /// <summary>
    /// 다음 맵 하나만 나타냅니다.
    /// UI 버튼에서 호출하기 좋습니다.
    /// </summary>
    public void PlayNext()
    {
        if (sequenceCoroutine != null)
            return;

        if (!HasNextMap())
            return;

        sequenceCoroutine =
            StartCoroutine(PlayNextRoutine());
    }

    /// <summary>
    /// 현재 맵부터 마지막 맵까지 자동으로 순차 재생합니다.
    /// </summary>
    public void PlayAll()
    {
        if (sequenceCoroutine != null)
            return;

        if (!HasNextMap())
            return;

        sequenceCoroutine =
            StartCoroutine(PlayAllRoutine());
    }

    /// <summary>
    /// 지정한 인덱스까지 순서대로 재생합니다.
    /// 예: PlayToIndex(4) → 현재 맵부터 4번 맵까지 재생
    /// </summary>
    public void PlayToIndex(int targetIndex)
    {
        if (sequenceCoroutine != null)
            return;

        if (tilemapTransitions == null ||
            tilemapTransitions.Length == 0)
            return;

        targetIndex = Mathf.Clamp(
            targetIndex,
            0,
            tilemapTransitions.Length - 1
        );

        if (targetIndex <= currentMapIndex)
            return;

        sequenceCoroutine =
            StartCoroutine(
                PlayToIndexRoutine(targetIndex)
            );
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
            currentMapIndex = nextIndex;
            yield break;
        }

        SetRendererEnabled(nextTransition, true);
        SetColliderEnabled(nextTransition, false);

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

        SetColliderEnabled(nextTransition, true);

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

    /// <summary>
    /// 모든 맵을 초기 상태로 되돌립니다.
    /// </summary>
    public void ResetSequence()
    {
        InitializeSequence();
    }

    /// <summary>
    /// 현재 진행 중인 순차 재생을 중지합니다.
    /// </summary>
    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (tilemapTransitions == null)
            return;

        foreach (TilemapRadialTransition transition
                 in tilemapTransitions)
        {
            if (transition != null)
                transition.StopTransition();
        }
    }

    private bool HasNextMap()
    {
        return tilemapTransitions != null &&
               currentMapIndex + 1 <
               tilemapTransitions.Length;
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
        TilemapCollider2D tilemapCollider =
            transition.GetComponent<TilemapCollider2D>();

        if (tilemapCollider != null)
            tilemapCollider.enabled = enabled;
    }
}