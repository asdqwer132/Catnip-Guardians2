using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CraftSuccessEffectUI : MonoBehaviour
{
    [Header("UI")]
    public Image itemIcon;
    public Image glowImage;
    public CanvasGroup canvasGroup;

    [Header("Show Option")]
    [Tooltip("제작 결과 아이템 이미지를 표시할지 여부")]
    public bool showResultItemImage = true;

    [Tooltip("글로우 이미지를 표시할지 여부")]
    public bool showGlowImage = true;

    [Header("Animation Option")]
    [Tooltip("결과 아이템 이미지에 팝 애니메이션을 적용할지 여부")]
    public bool usePopEffect = true;

    [Tooltip("글로우 이미지에 퍼지는 애니메이션을 적용할지 여부")]
    public bool useGlowEffect = true;

    [Header("Display")]
    public float displayDuration = 0.6f;
    public float fadeDuration = 0.15f;

    [Header("Pop Setting")]
    public float startScale = 0.8f;
    public float popScale = 1.15f;
    public float endScale = 1f;
    public float popDuration = 0.18f;

    [Header("Glow Setting")]
    public float glowStartScale = 0.7f;
    public float glowEndScale = 1.4f;
    public float glowMaxAlpha = 0.55f;

    [Header("Static Glow Setting")]
    [Tooltip("글로우 애니메이션을 끄고 표시만 할 때의 알파값")]
    public float staticGlowAlpha = 0.35f;

    [Tooltip("글로우 애니메이션을 끄고 표시만 할 때의 크기")]
    public float staticGlowScale = 1f;

    private RectTransform itemRect;
    private RectTransform glowRect;

    private Coroutine playCoroutine;

    private void Awake()
    {
        if (itemIcon != null)
            itemRect = itemIcon.rectTransform;

        if (glowImage != null)
            glowRect = glowImage.rectTransform;

        gameObject.SetActive(false);
    }

    public void Play(Sprite icon)
    {
        ErrorMessageManager.ShowFade("Success!", 0.5f, 0.5f);

        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        gameObject.SetActive(true);

        SetupItemIcon(icon);

        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private void SetupItemIcon(Sprite icon)
    {
        if (itemIcon == null)
            return;

        itemIcon.gameObject.SetActive(showResultItemImage);

        if (!showResultItemImage)
            return;

        itemIcon.sprite = icon;
        itemIcon.enabled = icon != null;
    }

    private IEnumerator PlayRoutine()
    {
        InitEffectState();

        float visibleDuration = Mathf.Max(0f, displayDuration - fadeDuration);
        float t = 0f;

        while (t < visibleDuration)
        {
            t += Time.unscaledDeltaTime;

            float progress = visibleDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / visibleDuration);

            UpdatePop(t);
            UpdateGlow(progress);

            yield return null;
        }

        yield return FadeOutRoutine();

        gameObject.SetActive(false);
        playCoroutine = null;
    }

    private void InitEffectState()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        InitItemState();
        InitGlowState();
    }

    private void InitItemState()
    {
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(showResultItemImage);

        if (!showResultItemImage)
            return;

        if (itemRect != null)
        {
            float scale = usePopEffect ? startScale : endScale;
            itemRect.localScale = Vector3.one * scale;
        }
    }

    private void InitGlowState()
    {
        if (glowImage == null)
            return;

        glowImage.gameObject.SetActive(showGlowImage);

        if (!showGlowImage)
            return;

        if (useGlowEffect)
        {
            SetImageAlpha(glowImage, 0f);

            if (glowRect != null)
                glowRect.localScale = Vector3.one * glowStartScale;
        }
        else
        {
            SetImageAlpha(glowImage, staticGlowAlpha);

            if (glowRect != null)
                glowRect.localScale = Vector3.one * staticGlowScale;
        }
    }

    private void UpdatePop(float elapsedTime)
    {
        if (!showResultItemImage)
            return;

        if (itemRect == null)
            return;

        if (!usePopEffect)
        {
            itemRect.localScale = Vector3.one * endScale;
            return;
        }

        float p = popDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsedTime / popDuration);

        float scale;

        if (p < 0.65f)
        {
            float np = p / 0.65f;
            scale = Mathf.Lerp(startScale, popScale, EaseOutBack(np));
        }
        else
        {
            float np = (p - 0.65f) / 0.35f;
            scale = Mathf.Lerp(popScale, endScale, EaseOutQuad(np));
        }

        itemRect.localScale = Vector3.one * scale;
    }

    private void UpdateGlow(float progress)
    {
        if (!showGlowImage)
            return;

        if (!useGlowEffect)
            return;

        if (glowImage == null || glowRect == null)
            return;

        float alpha = Mathf.Sin(progress * Mathf.PI) * glowMaxAlpha;
        float scale = Mathf.Lerp(glowStartScale, glowEndScale, progress);

        SetImageAlpha(glowImage, alpha);
        glowRect.localScale = Vector3.one * scale;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeDuration <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            yield break;
        }

        float t = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, p);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private float EaseOutQuad(float x)
    {
        return 1f - (1f - x) * (1f - x);
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}