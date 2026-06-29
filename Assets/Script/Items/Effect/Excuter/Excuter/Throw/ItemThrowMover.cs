using System;
using UnityEngine;

public class ItemThrowMover : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Move")]
    [Tooltip("자동 계산됨. 도착 시간과 X거리 기준")]
    public float speed = 10f;
    [Tooltip("목표 지점까지 도착하는 시간")]
    public float arriveTime = 0.6f;
    public float maxMoveTime = 3f;

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

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (!isMoving)
            return;

        timer += Time.deltaTime;

        float progress = timer / moveDuration;

        if (progress >= 1f)
        {
            Arrive();
            return;
        }

        progress = Mathf.Clamp01(progress);

        MoveProjectileArc(progress);
        UpdateRotation();
    }

    public void Init(
        Vector3 startPosition,
        Vector3 targetPosition,
        Sprite itemSprite,
        Action onArrive
    )
    {
        SetSprite(itemSprite);
        InitMove(startPosition, targetPosition, arriveTime, onArrive);
    }
    private void InitMove(
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

        moveDuration = Mathf.Max(0.01f, arriveTime);
        moveDuration = Mathf.Min(moveDuration, maxMoveTime);

        speed = horizontalDistance / moveDuration;

        finalArcHeight = GetFinalArcHeight();

        peakY = Mathf.Max(this.startPosition.y, this.targetPosition.y) + finalArcHeight;

        timer = 0f;
        isMoving = true;
    }
    private void SetSprite(Sprite itemSprite)
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

    private void UpdateRotation()
    {
        if (!spinWhileMoving)
            return;

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    private void Arrive()
    {
        if (!isMoving)
            return;

        isMoving = false;

        transform.position = targetPosition;

        onArrive?.Invoke();

        if (destroyOnArrive)
            Destroy(gameObject);
    }
}