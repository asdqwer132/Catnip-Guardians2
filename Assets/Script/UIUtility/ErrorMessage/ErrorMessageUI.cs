using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ErrorMessageUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;

    [Header("News Size")]
    [SerializeField] private bool resizeWidthToText = true;
    [SerializeField] private float textWidthPadding = 40f;

    [Tooltip("뉴스 이동을 시작할 때 현재 Rect 높이를 보존합니다. Stretch 상태에서 Anchor를 고정 Anchor로 바꾸면 sizeDelta.y가 0이 되는 문제를 막습니다.")]
    [SerializeField] private bool preserveHeightOnNews = true;

    [Tooltip("보존할 높이를 못 구했을 때 사용할 뉴스 메시지 최소 높이입니다.")]
    [Min(1f)][SerializeField] private float fallbackNewsHeight = 40f;

    [Tooltip("왼쪽 끝을 지난 뒤 추가로 더 이동할 거리입니다. 0이면 마지막 글자가 경계에 걸칠 수 있어서 2~20 정도 추천합니다.")]
    [Min(0f)][SerializeField] private float newsEndPadding = 8f;

    private ErrorMessageManager owner;
    private Coroutine routine;
    private bool useUnscaledTime;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;
        messageText = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();
    }

    public void Init(ErrorMessageRequest request, ErrorMessageManager owner, RectTransform viewport, bool useUnscaledTime, float newsY)
    {
        this.owner = owner;
        this.useUnscaledTime = useUnscaledTime;

        if (messageText != null)
        {
            messageText.text = request.message;

            if (request.useCustomColor)
                messageText.color = request.textColor;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (routine != null)
            StopCoroutine(routine);

        switch (request.effectType)
        {
            case ErrorMessageEffectType.FadeOut:
                routine = StartCoroutine(FadeOutRoutine(request.fadeStayDuration, request.fadeOutDuration));
                break;

            case ErrorMessageEffectType.NewsTicker:
                routine = StartCoroutine(NewsTickerRoutine(viewport, request.newsMoveSpeed, newsY));
                break;

            case ErrorMessageEffectType.None:
            default:
                break;
        }
    }

    private IEnumerator FadeOutRoutine(float stayDuration, float fadeDuration)
    {
        if (stayDuration > 0f)
            yield return Wait(stayDuration);

        fadeDuration = Mathf.Max(0.01f, fadeDuration);
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += DeltaTime();
            float t = Mathf.Clamp01(timer / fadeDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator NewsTickerRoutine(RectTransform viewport, float speed, float y)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (viewport == null)
            viewport = transform.parent as RectTransform;

        if (rectTransform == null || viewport == null)
            yield break;

        speed = Mathf.Max(1f, speed);

        if (messageText != null)
        {
            messageText.textWrappingMode = TextWrappingModes.NoWrap;
            messageText.overflowMode = TextOverflowModes.Overflow;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();

        float currentHeight = GetCurrentHeightBeforeChangingAnchor();
        float textWidth = GetTextWidth();

        float targetWidth = resizeWidthToText
            ? Mathf.Max(1f, textWidth + textWidthPadding)
            : Mathf.Max(1f, rectTransform.rect.width, textWidth);

        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        if (preserveHeightOnNews)
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

        float width = Mathf.Max(rectTransform.rect.width, textWidth, 1f);

        float startX = viewport.rect.width;
        float endX = -width - newsEndPadding;

        rectTransform.anchoredPosition = new Vector2(startX, y);

        while (rectTransform.anchoredPosition.x > endX)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x -= speed * DeltaTime();
            rectTransform.anchoredPosition = pos;
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector2(endX, y);
        Destroy(gameObject);
    }

    private float GetCurrentHeightBeforeChangingAnchor()
    {
        float height = rectTransform != null ? rectTransform.rect.height : 0f;

        if (height <= 1f && messageText != null)
        {
            messageText.ForceMeshUpdate();
            Vector2 preferred = messageText.GetPreferredValues(messageText.text, Mathf.Infinity, Mathf.Infinity);
            height = preferred.y + 12f;
        }

        if (height <= 1f)
            height = fallbackNewsHeight;

        return Mathf.Max(1f, height);
    }

    private float GetTextWidth()
    {
        if (messageText == null)
            return rectTransform != null ? rectTransform.rect.width : 0f;

        messageText.ForceMeshUpdate();
        Vector2 preferred = messageText.GetPreferredValues(messageText.text, Mathf.Infinity, Mathf.Infinity);
        return preferred.x;
    }

    private IEnumerator Wait(float duration)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return new WaitForSeconds(duration);
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (owner != null)
            owner.Unregister(this);
    }
}