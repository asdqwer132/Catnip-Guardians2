using UnityEngine;
using UnityEngine.UI;

public class ImageFillUI : MonoBehaviour
{
    [Header("Image")]
    public Image fillImage;

    [Header("Option")]
    public bool clampValue = true;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();
    }

    public void SetFill01(float value)
    {
        if (fillImage == null)
            return;

        if (clampValue)
            value = Mathf.Clamp01(value);

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

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}