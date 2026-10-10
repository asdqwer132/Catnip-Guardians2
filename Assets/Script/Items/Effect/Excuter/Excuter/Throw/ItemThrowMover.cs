using System;
using UnityEngine;

public class ItemThrowMover : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Move")]
    [Tooltip("자동 계산됨. 도착 시간과 X거리 기준")]
    public float speed = 10f;
    [Tooltip("고정 모드의 도착 시간 또는 무게 모드의 기본 도착 시간(초)")]
    public float arriveTime = 0.6f;
    [Tooltip("모든 비행 시간의 최종 상한(초). 최소 도착 시간 이상으로 설정하세요.")]
    public float maxMoveTime = 3f;

    [Header("Arrival Timing")]
    public ItemThrowArrivalTiming arrivalTiming = new ItemThrowArrivalTiming();

    [Header("Projectile Arc")]
    [Tooltip("최고점 높이")]
    public float arcHeight = 1.5f;
    public bool autoArcHeightByDistance = true;
    [Tooltip("자동 높이 최소값")]
    public float minArcHeight = 0.6f;
    [Tooltip("자동 높이 최대값")]
    public float maxArcHeight = 3f;
    [Tooltip("X거리 기준으로 높이 계산")]
    public float arcHeightDistanceMultiplier = 0.25f;
    [Range(0.1f, 0.9f)]
    [Tooltip("최고점 위치. 0.5면 가운데, 0.35면 초반에 높이 뜸")]
    public float arcPeakProgress = 0.45f;

    [Header("Rotation")]
    public bool spinWhileMoving = true;
    public float spinSpeed = 540f;
    [Tooltip("포물선의 현재 이동 방향을 바라봅니다. 켜면 기본 스핀보다 우선합니다.")]
    public bool faceMoveDirection;
    [Tooltip("이동 방향 회전 보정(도). 위쪽을 바라보는 스프라이트는 -90, 오른쪽은 0.")]
    public float directionAngleOffset = -90f;

    [Header("Destroy")]
    public bool destroyOnArrive = true;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;
    private float moveDuration;
    private float horizontalDistance;
    private float finalArcHeight;
    private float peakY;
    private bool isMoving;
    private Action onArrive;
    public float MoveDuration => moveDuration;
    public bool IsMoving => isMoving;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
        => Tick(Time.deltaTime);

    public void Tick(float deltaTime)
    {
        if (!isMoving)
            return;

        deltaTime = float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ? 0f : Mathf.Max(0f, deltaTime);
        timer += deltaTime;

        float progress = timer / moveDuration;

        if (progress >= 1f)
        {
            Arrive();
            return;
        }

        progress = Mathf.Clamp01(progress);

        MoveProjectileArc(progress);
        UpdateRotation(deltaTime, progress);
    }

    public void Init(
        Vector3 startPosition,
        Vector3 targetPosition,
        Sprite itemSprite,
        Action onArrive,
        float itemWeight = 0f
    )
    {
        SetSprite(itemSprite);
        InitMove(startPosition, targetPosition, ResolveArrivalTime(itemWeight), onArrive);
    }

    public float ResolveArrivalTime(float itemWeight)
        => arrivalTiming != null
            ? arrivalTiming.Resolve(arriveTime, maxMoveTime, itemWeight,
                arrivalTiming.mode == ItemThrowArrivalMode.Fixed ? 0.5f : UnityEngine.Random.value)
            : ItemThrowArrivalTiming.ClampDuration(arriveTime, maxMoveTime);

    public void InitMove(
        Vector3 startPosition,
        Vector3 targetPosition,
        float arriveTime,
        Action onArrive
    )
    {
        this.startPosition = startPosition;
        this.targetPosition = targetPosition;
        this.onArrive = onArrive;

        this.startPosition.z = 0f;
        this.targetPosition.z = 0f;

        transform.position = this.startPosition;

        horizontalDistance = Mathf.Abs(this.targetPosition.x - this.startPosition.x);

        // Explicit flight times (e.g. Repeat's override) bypass random/weight timing.
        moveDuration = ItemThrowArrivalTiming.ClampDuration(arriveTime, maxMoveTime);

        speed = horizontalDistance / moveDuration;

        finalArcHeight = GetFinalArcHeight();

        peakY = Mathf.Max(this.startPosition.y, this.targetPosition.y) + finalArcHeight;

        timer = 0f;
        isMoving = true;
        if (faceMoveDirection)
            UpdateRotation(0f, 0f);
    }
    public void SetSprite(Sprite itemSprite)
    {
        if (itemSprite == null)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = Vector3.one;

            spriteRenderer = visualObj.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = itemSprite;
    }


    private void MoveProjectileArc(float progress)
    {
        float x = Mathf.Lerp(
            startPosition.x,
            targetPosition.x,
            progress
        );

        float y = GetParabolaY(progress);

        transform.position = new Vector3(
            x,
            y,
            0f
        );
    }

    private float GetParabolaY(float progress)
    {
        if (finalArcHeight == 0f)
            return Mathf.Lerp(startPosition.y, targetPosition.y, progress);

        float p = progress;

        float k = Mathf.Clamp(
            arcPeakProgress,
            0.1f,
            0.9f
        );

        float y0 = startPosition.y;
        float y1 = targetPosition.y;
        float yp = peakY;

        float l0 = ((p - k) * (p - 1f)) / ((0f - k) * (0f - 1f));
        float lp = ((p - 0f) * (p - 1f)) / ((k - 0f) * (k - 1f));
        float l1 = ((p - 0f) * (p - k)) / ((1f - 0f) * (1f - k));

        float y = y0 * l0 + yp * lp + y1 * l1;

        return y;
    }

    private float GetFinalArcHeight()
    {
        if (!autoArcHeightByDistance)
            return arcHeight;

        float height = horizontalDistance * arcHeightDistanceMultiplier;

        return Mathf.Clamp(
            height,
            minArcHeight,
            maxArcHeight
        );
    }

    private void UpdateRotation(float deltaTime, float progress)
    {
        if (faceMoveDirection)
        {
            float dy = targetPosition.y - startPosition.y;
            if (finalArcHeight != 0f)
            {
                float k = Mathf.Clamp(arcPeakProgress, 0.1f, 0.9f);
                // Derivative of the same interpolating parabola used for position.
                dy = startPosition.y * (2f * progress - k - 1f) / k
                    + peakY * (2f * progress - 1f) / (k * (k - 1f))
                    + targetPosition.y * (2f * progress - k) / (1f - k);
            }
            Vector2 direction = new Vector2(targetPosition.x - startPosition.x, dy);
            if (direction.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + directionAngleOffset);
            return;
        }

        if (!spinWhileMoving)
            return;

        transform.Rotate(0f, 0f, spinSpeed * deltaTime);
    }

    private void OnDisable()
    {
        isMoving = false;
        onArrive = null;
    }

    private void Arrive()
    {
        if (!isMoving)
            return;

        isMoving = false;

        transform.position = targetPosition;
        if (faceMoveDirection)
            UpdateRotation(0f, 1f);

        Action arrived = onArrive;
        onArrive = null;
        arrived?.Invoke();

        if (destroyOnArrive && !isMoving)
            Destroy(gameObject);
    }
}
