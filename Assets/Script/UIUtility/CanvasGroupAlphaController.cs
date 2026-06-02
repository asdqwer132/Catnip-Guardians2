using UnityEngine;

public class CanvasGroupAlphaController : MonoBehaviour
{
    [Header("Targets")]
    public CanvasGroup[] canvasGroups;

    [Header("Alpha")]
    [Range(0f, 1f)]
    public float alpha = 1f;

    [Header("Input")]
    public bool changeInteractable = true;
    public bool changeBlocksRaycasts = true;

    private void Start()
    {
        SetAlpha(alpha);
    }

    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);

        if (canvasGroups == null)
            return;

        bool visible = alpha > 0f;

        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] == null)
                continue;

            canvasGroups[i].alpha = alpha;

            if (changeInteractable)
                canvasGroups[i].interactable = visible;

            if (changeBlocksRaycasts)
                canvasGroups[i].blocksRaycasts = visible;
        }
    }

    public void Show()
    {
        SetAlpha(1f);
    }

    public void Hide()
    {
        SetAlpha(0f);
    }

    public void SetHalfAlpha()
    {
        SetAlpha(0.5f);
    }

    public void ApplyCurrentAlpha()
    {
        SetAlpha(alpha);
    }
}