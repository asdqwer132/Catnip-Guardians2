
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FishingMiniGame : MonoBehaviour
{
    public enum Result
    {
        Fail,
        Success,
        Perfect
    }

    [Header("UI")]
    [SerializeField] private RectTransform barArea;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private RectTransform successArea;
    [SerializeField] private RectTransform perfectArea;
    [SerializeField] private Button fishingButton;

    [Header("Cursor")]
    [SerializeField, Min(0.01f)]
    private float cursorSpeed = 1f;

    [SerializeField]
    private bool useUnscaledTime = true;

    [Header("Success Area (0 ~ 1)")]
    [SerializeField, Range(0f, 1f)]
    private float successCenter = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float successWidth = 0.4f;

    [Header("Perfect Area (0 ~ 1)")]
    [SerializeField, Range(0f, 1f)]
    private float perfectWidth = 0.1f;

    [Header("Options")]
    [SerializeField]
    private bool randomizeSuccessPosition = true;

    [SerializeField]
    private bool stopAfterResult = true;

    [Header("Events")]
    public UnityEvent onFail;
    public UnityEvent onSuccess;
    public UnityEvent onPerfect;

    private float position;
    private int direction = 1;
    private bool isPlaying;

    public float Position => position;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (fishingButton != null)
            fishingButton.onClick.AddListener(TryFishing);

        UpdateAreas();
        UpdateCursor();
    }

    private void OnDestroy()
    {
        if (fishingButton != null)
            fishingButton.onClick.RemoveListener(TryFishing);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        MoveCursor(deltaTime);
    }

    // ==============================
    // MiniGame
    // ==============================

    public void StartMiniGame()
    {
        position = 0f;
        direction = 1;
        isPlaying = true;

        if (randomizeSuccessPosition)
            RandomizeSuccessArea();

        UpdateAreas();
        UpdateCursor();

        if (fishingButton != null)
            fishingButton.interactable = true;
    }

    public void StopMiniGame()
    {
        isPlaying = false;

        if (fishingButton != null)
            fishingButton.interactable = false;
    }

    // ==============================
    // Cursor
    // ==============================

    private void MoveCursor(float deltaTime)
    {
        position += direction * cursorSpeed * deltaTime;

        // 경계를 넘어간 거리까지 반사
        while (position > 1f || position < 0f)
        {
            if (position > 1f)
            {
                position = 2f - position;
                direction = -1;
            }
            else if (position < 0f)
            {
                position = -position;
                direction = 1;
            }
        }

        UpdateCursor();
    }

    private void UpdateCursor()
    {
        if (barArea == null || cursor == null)
            return;

        float width = barArea.rect.width;

        Vector2 anchoredPosition = cursor.anchoredPosition;

        anchoredPosition.x = (position - 0.5f) * width;

        cursor.anchoredPosition = anchoredPosition;
    }

    // ==============================
    // Areas
    // ==============================

    private void RandomizeSuccessArea()
    {
        float halfWidth = successWidth * 0.5f;

        successCenter = Random.Range(
            halfWidth,
            1f - halfWidth
        );
    }

    private void UpdateAreas()
    {
        if (barArea == null)
            return;

        successWidth = Mathf.Clamp01(successWidth);
        perfectWidth = Mathf.Clamp(
            perfectWidth, 0f, successWidth
        );

        float halfWidth = successWidth * 0.5f;

        successCenter = Mathf.Clamp(
            successCenter,
            halfWidth,
            1f - halfWidth
        );

        SetArea(successArea, successCenter, successWidth);
        SetArea(perfectArea, successCenter, perfectWidth);
    }

    private void SetArea(
        RectTransform area,
        float center,
        float normalizedWidth)
    {
        if (area == null)
            return;

        float width = barArea.rect.width;

        area.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width * normalizedWidth
        );

        Vector2 anchoredPosition = area.anchoredPosition;

        anchoredPosition.x = (center - 0.5f) * width;

        area.anchoredPosition = anchoredPosition;
    }

    // ==============================
    // Result
    // ==============================

    public void TryFishing()
    {
        if (!isPlaying)
            return;

        Result result = EvaluateResult();

        switch (result)
        {
            case Result.Fail:
                onFail?.Invoke();
                break;

            case Result.Success:
                onSuccess?.Invoke();
                break;

            case Result.Perfect:
                onPerfect?.Invoke();
                break;
        }

        if (stopAfterResult)
            StopMiniGame();
    }

    private Result EvaluateResult()
    {
        float perfectHalf = perfectWidth * 0.5f;
        float successHalf = successWidth * 0.5f;

        if (Mathf.Abs(position - successCenter) <= perfectHalf)
            return Result.Perfect;

        if (Mathf.Abs(position - successCenter) <= successHalf)
            return Result.Success;

        return Result.Fail;
    }

    // ==============================
    // Public Settings
    // ==============================

    public void SetCursorSpeed(float speed)
    {
        cursorSpeed = Mathf.Max(0.01f, speed);
    }

    public void SetSuccessWidth(float width)
    {
        successWidth = Mathf.Clamp01(width);
        UpdateAreas();
    }

    public void SetPerfectWidth(float width)
    {
        perfectWidth = Mathf.Clamp01(width);
        UpdateAreas();
    }

    public void SetSuccessCenter(float center)
    {
        successCenter = Mathf.Clamp01(center);
        UpdateAreas();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (barArea == null)
            return;

        UpdateAreas();
        UpdateCursor();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        successWidth = Mathf.Clamp01(successWidth);

        perfectWidth = Mathf.Clamp(
            perfectWidth, 0f, successWidth
        );

        float halfWidth = successWidth * 0.5f;

        successCenter = Mathf.Clamp(
            successCenter,
            halfWidth,
            1f - halfWidth
        );
    }
#endif
}
