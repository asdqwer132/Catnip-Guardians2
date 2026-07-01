using System.Collections.Generic;
using UnityEngine;

public class ErrorMessageManager : MonoBehaviour
{
    public static ErrorMessageManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private ErrorMessageUI messagePrefab;

    [Header("Parent")]
    [Tooltip("일반/페이드 메시지가 생성될 부모입니다. 이제 메시지는 1개만 유지되므로 VerticalLayoutGroup은 필요 없습니다.")]
    [SerializeField] private RectTransform normalMessageParent;

    [Tooltip("뉴스 메시지가 생성될 부모입니다. 화면 하단 전체 폭 패널을 추천합니다. RectMask2D를 붙이면 영역 밖 글자가 잘립니다.")]
    [SerializeField] private RectTransform newsMessageParent;

    [Header("Default Effect")]
    public bool useFadeOut;
    public bool useNewsTicker;

    [Header("Fade Out")]
    [Min(0f)] public float fadeStayDuration = 1.2f;
    [Min(0.01f)] public float fadeOutDuration = 0.35f;

    [Header("News Ticker")]
    [Min(1f)] public float newsMoveSpeed = 160f;
    public float newsY = 0f;

    [Header("Option")]
    public bool useUnscaledTime = true;
    public bool dontDestroyOnLoad = false;

    [Tooltip("켜두면 새 메시지가 들어올 때 기존 메시지를 모두 지우고 새 메시지를 처음부터 재생합니다.")]
    public bool clearPreviousOnShow = true;

    private readonly List<ErrorMessageUI> activeMessages = new List<ErrorMessageUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    public static ErrorMessageUI Show(string message)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"ErrorMessageManager is missing. Message: {message}");
            return null;
        }

        return Instance.ShowDefault(message);
    }

    public static ErrorMessageUI ShowFade(string message, float stayDuration = -1f, float fadeDuration = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"ErrorMessageManager is missing. Message: {message}");
            return null;
        }

        if (stayDuration < 0f)
            stayDuration = Instance.fadeStayDuration;

        if (fadeDuration < 0f)
            fadeDuration = Instance.fadeOutDuration;

        return Instance.Show(ErrorMessageRequest.Fade(message, stayDuration, fadeDuration));
    }

    public static ErrorMessageUI ShowNews(string message, float moveSpeed = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"ErrorMessageManager is missing. Message: {message}");
            return null;
        }

        if (moveSpeed < 0f)
            moveSpeed = Instance.newsMoveSpeed;

        return Instance.Show(ErrorMessageRequest.News(message, moveSpeed));
    }

    public ErrorMessageUI ShowDefault(string message)
    {
        ErrorMessageEffectType effectType = GetDefaultEffectType();

        ErrorMessageRequest request = new ErrorMessageRequest
        {
            message = message,
            effectType = effectType,
            fadeStayDuration = fadeStayDuration,
            fadeOutDuration = fadeOutDuration,
            newsMoveSpeed = newsMoveSpeed,
            textColor = Color.white,
            useCustomColor = false
        };

        return Show(request);
    }

    public ErrorMessageUI Show(ErrorMessageRequest request)
    {
        if (messagePrefab == null)
        {
            Debug.LogWarning($"Message Prefab is missing. Message: {request.message}", this);
            return null;
        }

        if (string.IsNullOrEmpty(request.message))
            return null;

        if (clearPreviousOnShow)
            ClearAllMessages();

        RectTransform parent = GetParent(request.effectType);
        ErrorMessageUI messageUI = Instantiate(messagePrefab, parent);
        activeMessages.Add(messageUI);

        messageUI.Init(request, this, parent, useUnscaledTime, newsY);
        return messageUI;
    }

    private ErrorMessageEffectType GetDefaultEffectType()
    {
        if (useNewsTicker)
            return ErrorMessageEffectType.NewsTicker;

        if (useFadeOut)
            return ErrorMessageEffectType.FadeOut;

        return ErrorMessageEffectType.None;
    }

    private RectTransform GetParent(ErrorMessageEffectType effectType)
    {
        RectTransform parent = null;

        if (effectType == ErrorMessageEffectType.NewsTicker)
            parent = newsMessageParent != null ? newsMessageParent : normalMessageParent;
        else
            parent = normalMessageParent != null ? normalMessageParent : newsMessageParent;

        if (parent == null)
            parent = transform as RectTransform;

        return parent;
    }

    public void ClearAllMessages()
    {
        for (int i = activeMessages.Count - 1; i >= 0; i--)
        {
            ErrorMessageUI target = activeMessages[i];

            if (target != null)
                Destroy(target.gameObject);
        }

        activeMessages.Clear();
    }

    public void Unregister(ErrorMessageUI messageUI)
    {
        activeMessages.Remove(messageUI);
    }
}
