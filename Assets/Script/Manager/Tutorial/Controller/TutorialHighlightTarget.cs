using UnityEngine;

public class TutorialHighlightTarget : MonoBehaviour
{
    [Header("Target")]
    public RectTransform targetRect;

    [Header("Option")]
    public Vector2 padding = new Vector2(30f, 30f);
    public Vector2 sizeOverride;
    public bool useSizeOverride;

    private void Reset()
    {
        targetRect = GetComponent<RectTransform>();
    }
}