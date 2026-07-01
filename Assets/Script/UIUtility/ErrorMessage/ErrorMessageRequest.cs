using UnityEngine;

public struct ErrorMessageRequest
{
    public string message;
    public ErrorMessageEffectType effectType;
    public float fadeStayDuration;
    public float fadeOutDuration;
    public float newsMoveSpeed;
    public Color textColor;
    public bool useCustomColor;

    public static ErrorMessageRequest None(string message)
    {
        return new ErrorMessageRequest
        {
            message = message,
            effectType = ErrorMessageEffectType.None,
            fadeStayDuration = 0f,
            fadeOutDuration = 0f,
            newsMoveSpeed = 0f,
            textColor = Color.white,
            useCustomColor = false
        };
    }

    public static ErrorMessageRequest Fade(string message, float stayDuration, float fadeDuration)
    {
        return new ErrorMessageRequest
        {
            message = message,
            effectType = ErrorMessageEffectType.FadeOut,
            fadeStayDuration = stayDuration,
            fadeOutDuration = fadeDuration,
            newsMoveSpeed = 0f,
            textColor = Color.white,
            useCustomColor = false
        };
    }

    public static ErrorMessageRequest News(string message, float moveSpeed)
    {
        return new ErrorMessageRequest
        {
            message = message,
            effectType = ErrorMessageEffectType.NewsTicker,
            fadeStayDuration = 0f,
            fadeOutDuration = 0f,
            newsMoveSpeed = moveSpeed,
            textColor = Color.white,
            useCustomColor = false
        };
    }
}
