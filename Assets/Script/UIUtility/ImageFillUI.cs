using UnityEngine;
using UnityEngine.UI;

public class ImageFillUI : MonoBehaviour
{
    [Header("Image")]
    public Image fillImage;

    [Header("Option")]
    public bool clampValue = true;
    public bool reverseFill = false;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();
    }

    public void SetFill01(float value)
    {
        if (clampValue)
            value = Mathf.Clamp01(value);

        if (reverseFill)
            value = 1f - value;

        if (fillImage != null)
            fillImage.fillAmount = value;
    }

    public void SetFill(float current, float max)
    {
        if (max <= 0f)
        {
            SetFill01(0f);
            return;
        }

        SetFill01(current / max);
    }

    public void Clear()
    {
        SetFill01(0f);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}