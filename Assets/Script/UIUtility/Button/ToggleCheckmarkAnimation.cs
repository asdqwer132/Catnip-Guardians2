using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToggleCheckmarkScaleAnimation : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private RectTransform checkmark;

    [Header("Animation")]
    [SerializeField] private float startScale = 0.7f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        if (checkmark != null)
            checkmark.localScale = Vector3.one;
    }

    private void OnEnable()
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (checkmark != null)
            checkmark.localScale = Vector3.one;
    }

    private void OnToggleChanged(bool isOn)
    {
        if (!isOn || checkmark == null)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(PlayScaleAnimation());
    }

    private IEnumerator PlayScaleAnimation()
    {
        float halfDuration = duration * 0.5f;

        checkmark.localScale = Vector3.one * startScale;

        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += GetDeltaTime();

            float t = Mathf.Clamp01(timer / halfDuration);

            checkmark.localScale = Vector3.Lerp(
                Vector3.one * startScale,
                Vector3.one * maxScale,
                t
            );

            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += GetDeltaTime();

            float t = Mathf.Clamp01(timer / halfDuration);

            checkmark.localScale = Vector3.Lerp(
                Vector3.one * maxScale,
                Vector3.one,
                t
            );

            yield return null;
        }

        checkmark.localScale = Vector3.one;
        animationCoroutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }
}