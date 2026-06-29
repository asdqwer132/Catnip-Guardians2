using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShopPurchaseEffectPlayer : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas targetCanvas;
    public RectTransform effectRoot;

    [Header("Flying Item Icon")]
    public Image flyingItemImagePrefab;

    [Header("Default Points")]
    public RectTransform defaultStartPoint;
    public RectTransform defaultEndPoint;

    [Header("Box Open Animation")]
    public Animator defaultBoxOpenAnimator;
    public string openTriggerName = "Open";
    public float delayAfterBoxOpen = 0.25f;

    [Header("Move")]
    public float moveDuration = 0.55f;
    public float arcHeight = 120f;
    public bool useUnscaledTime = true;

    [Header("Scale")]
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = new Vector3(0.65f, 0.65f, 1f);

    [Header("Rotation")]
    public float spinAngle = 180f;

    [Header("Fade")]
    public bool fadeOutAtEnd = true;
    [Range(0f, 1f)] public float fadeStartTime = 0.75f;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (effectRoot == null && targetCanvas != null)
            effectRoot = targetCanvas.transform as RectTransform;
    }

    public void PlayPurchaseEffect(
        ShopBoxButton sourceButton,
        ItemData resultItem,
        Action onArrived
    )
    {
        if (resultItem == null)
        {
            onArrived?.Invoke();
            return;
        }

        if (gameObject.activeInHierarchy == false)
        {
            onArrived?.Invoke();
            return;
        }

        StartCoroutine(PlayRoutine(sourceButton, resultItem, onArrived));
    }

    private IEnumerator PlayRoutine(
        ShopBoxButton sourceButton,
        ItemData resultItem,
        Action onArrived
    )
    {
        IsPlaying = true;

        PlayBoxOpenAnimation(sourceButton);

        if (delayAfterBoxOpen > 0f)
            yield return Wait(delayAfterBoxOpen);

        Image flyingImage = CreateFlyingImage(resultItem.icon);

        if (flyingImage == null)
        {
            onArrived?.Invoke();
            IsPlaying = false;
            yield break;
        }

        RectTransform flyingRect = flyingImage.rectTransform;

        Vector2 startPos = GetStartPosition(sourceButton);
        Vector2 endPos = GetEndPosition();

        flyingRect.anchoredPosition = startPos;
        flyingRect.localScale = startScale;
        flyingRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = flyingImage.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = flyingImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        float timer = 0f;

        while (timer < moveDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += deltaTime;

            float t = Mathf.Clamp01(timer / moveDuration);
            float smoothT = t * t * (3f - 2f * t);

            Vector2 position = Vector2.Lerp(startPos, endPos, smoothT);

            float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
            position.y += arc;

            flyingRect.anchoredPosition = position;
            flyingRect.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            flyingRect.localRotation = Quaternion.Euler(0f, 0f, spinAngle * t);

            if (fadeOutAtEnd && t >= fadeStartTime)
            {
                float fadeT = Mathf.InverseLerp(fadeStartTime, 1f, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        Destroy(flyingImage.gameObject);

        onArrived?.Invoke();

        IsPlaying = false;
    }

    private void PlayBoxOpenAnimation(ShopBoxButton sourceButton)
    {
        Animator animator = null;

        if (sourceButton != null)
            animator = sourceButton.GetComponentInChildren<Animator>();

        if (animator == null)
            animator = defaultBoxOpenAnimator;

        if (animator == null)
            return;

        if (string.IsNullOrEmpty(openTriggerName))
            return;

        animator.ResetTrigger(openTriggerName);
        animator.SetTrigger(openTriggerName);
    }

    private Image CreateFlyingImage(Sprite itemSprite)
    {
        Image image;

        if (flyingItemImagePrefab != null)
        {
            image = Instantiate(flyingItemImagePrefab, effectRoot);
        }
        else
        {
            GameObject iconObject = new GameObject("Flying Item Icon");
            iconObject.transform.SetParent(effectRoot, false);

            image = iconObject.AddComponent<Image>();

            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64f, 64f);
        }

        image.sprite = itemSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.SetActive(true);

        return image;
    }

    private Vector2 GetStartPosition(ShopBoxButton sourceButton)
    {
        RectTransform startRect = null;

        if (sourceButton != null)
            startRect = sourceButton.transform as RectTransform;

        if (startRect == null)
            startRect = defaultStartPoint;

        return WorldToEffectRootPosition(startRect);
    }

    private Vector2 GetEndPosition()
    {
        return WorldToEffectRootPosition(defaultEndPoint);
    }

    private Vector2 WorldToEffectRootPosition(RectTransform target)
    {
        if (target == null || effectRoot == null)
            return Vector2.zero;

        Camera uiCamera = null;

        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = targetCanvas.worldCamera;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectRoot,
            screenPosition,
            uiCamera,
            out Vector2 localPosition
        );

        return localPosition;
    }

    private WaitForSeconds Wait(float time)
    {
        return new WaitForSeconds(time);
    }
}