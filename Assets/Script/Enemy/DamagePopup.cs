using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI damageText;

    [Header("Move")]
    public float moveSpeed = 1.5f;
    public float lifeTime = 0.8f;

    [Header("Scale")]
    public bool useScaleAnimation = true;
    public float startScale = 1.2f;
    public float endScale = 0.8f;

    private float timer;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(float damage)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damage).ToString();
        }

        timer = 0f;
        transform.localScale = Vector3.one * startScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        float progress = timer / lifeTime;
        progress = Mathf.Clamp01(progress);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f - progress;

        if (useScaleAnimation)
        {
            float scale = Mathf.Lerp(startScale, endScale, progress);
            transform.localScale = Vector3.one * scale;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}