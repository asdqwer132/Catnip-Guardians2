using System.Collections;
using UnityEngine;

public class UIPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    public RectTransform[] panels;

    [Header("Animation")]
    public float duration = 0.12f;
    public float moveDistance = 24f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentIndex = -1;
    private Coroutine routine;

    private Vector2[] originPositions;
    private CanvasGroup[] canvasGroups;

    private void Awake()
    {
        originPositions = new Vector2[panels.Length];
        canvasGroups = new CanvasGroup[panels.Length];

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null)
                continue;

            originPositions[i] = panels[i].anchoredPosition;

            canvasGroups[i] = panels[i].GetComponent<CanvasGroup>();

            if (canvasGroups[i] == null)
                canvasGroups[i] = panels[i].gameObject.AddComponent<CanvasGroup>();

            panels[i].gameObject.SetActive(false);
            canvasGroups[i].alpha = 0f;
        }
    }

    private void Start()
    {
        if (panels.Length > 0)
            ShowPanelInstant(0);
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= panels.Length)
            return;

        if (index == currentIndex)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SwitchRoutine(index));
    }

    private IEnumerator SwitchRoutine(int nextIndex)
    {
        int prevIndex = currentIndex;

        RectTransform prevPanel = prevIndex >= 0 ? panels[prevIndex] : null;
        RectTransform nextPanel = panels[nextIndex];

        CanvasGroup prevGroup = prevIndex >= 0 ? canvasGroups[prevIndex] : null;
        CanvasGroup nextGroup = canvasGroups[nextIndex];

        Vector2 nextOrigin = originPositions[nextIndex];
        Vector2 nextStart = nextOrigin + Vector2.up * moveDistance;

        nextPanel.gameObject.SetActive(true);
        nextPanel.anchoredPosition = nextStart;
        nextGroup.alpha = 0f;
        nextGroup.interactable = false;
        nextGroup.blocksRaycasts = false;

        if (prevGroup != null)
        {
            prevGroup.interactable = false;
            prevGroup.blocksRaycasts = false;
        }


        if (prevPanel != null)
        {
            prevPanel.gameObject.SetActive(false);
            prevPanel.anchoredPosition = originPositions[prevIndex];
            prevGroup.alpha = 0f;
        }
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float e = curve.Evaluate(t);

            if (prevPanel != null)
            {
                Vector2 prevOrigin = originPositions[prevIndex];
                prevPanel.anchoredPosition = Vector2.Lerp(
                    prevOrigin,
                    prevOrigin + Vector2.down * moveDistance,
                    e
                );

                prevGroup.alpha = Mathf.Lerp(1f, 0f, e);
            }

            nextPanel.anchoredPosition = Vector2.Lerp(nextStart, nextOrigin, e);
            nextGroup.alpha = Mathf.Lerp(0f, 1f, e);

            yield return null;
        }

        nextPanel.anchoredPosition = nextOrigin;
        nextGroup.alpha = 1f;
        nextGroup.interactable = true;
        nextGroup.blocksRaycasts = true;

        currentIndex = nextIndex;
        routine = null;
    }

    public void ShowPanelInstant(int index)
    {
        if (index < 0 || index >= panels.Length)
            return;

        for (int i = 0; i < panels.Length; i++)
        {
            bool active = i == index;

            panels[i].gameObject.SetActive(active);
            panels[i].anchoredPosition = originPositions[i];

            canvasGroups[i].alpha = active ? 1f : 0f;
            canvasGroups[i].interactable = active;
            canvasGroups[i].blocksRaycasts = active;
        }

        currentIndex = index;
    }
}