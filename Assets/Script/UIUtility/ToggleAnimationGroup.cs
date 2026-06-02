using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToggleAnimationGroup : MonoBehaviour
{
    [Header("Toggles")]
    public Toggle[] toggles;

    [Header("Move Target")]
    public RectTransform[] moveTargets;

    [Header("Move")]
    public float selectedXOffset = 20f;
    public float moveDuration = 0.12f;

    [Header("Effect")]
    public bool useOvershoot = true;
    public float overshootAmount = 6f;

    private Toggle currentToggle;
    private Vector2[] originPositions;
    private Coroutine[] moveCoroutines;
    private bool isChanging;

    private void Awake()
    {
        if (toggles == null || toggles.Length == 0)
            toggles = GetComponentsInChildren<Toggle>(true);

        InitMoveTargets();
        CacheOriginPositions();

        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];

            if (toggle == null)
                continue;

            int index = i;

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    SelectToggle(index);
            });
        }
    }

    private void Start()
    {
        int firstIndex = GetFirstOnIndex();

        if (firstIndex >= 0)
        {
            currentToggle = toggles[firstIndex];
            SetMoveInstant(firstIndex, true);
        }
    }

    private void OnDestroy()
    {
        if (toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] != null)
                toggles[i].onValueChanged.RemoveAllListeners();
        }
    }

    private void InitMoveTargets()
    {
        if (moveTargets != null && moveTargets.Length == toggles.Length)
            return;

        moveTargets = new RectTransform[toggles.Length];

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == null)
                continue;

            Transform visual = toggles[i].transform.Find("Visual");

            if (visual != null)
                moveTargets[i] = visual.GetComponent<RectTransform>();
            else
                moveTargets[i] = toggles[i].GetComponent<RectTransform>();
        }
    }

    private void CacheOriginPositions()
    {
        originPositions = new Vector2[moveTargets.Length];
        moveCoroutines = new Coroutine[moveTargets.Length];

        for (int i = 0; i < moveTargets.Length; i++)
        {
            if (moveTargets[i] != null)
                originPositions[i] = moveTargets[i].anchoredPosition;
        }
    }

    public void SelectToggle(int selectedIndex)
    {
        if (isChanging)
            return;

        if (selectedIndex < 0 || selectedIndex >= toggles.Length)
            return;

        Toggle selectedToggle = toggles[selectedIndex];

        if (selectedToggle == null)
            return;

        if (currentToggle == selectedToggle)
            return;

        isChanging = true;

        int previousIndex = GetToggleIndex(currentToggle);

        currentToggle = selectedToggle;

        if (previousIndex >= 0)
        {
            toggles[previousIndex].SetIsOnWithoutNotify(false);
            MoveToggle(previousIndex, false);
        }

        selectedToggle.SetIsOnWithoutNotify(true);
        MoveToggle(selectedIndex, true);

        isChanging = false;
    }

    private void MoveToggle(int index, bool selected)
    {
        if (index < 0 || index >= moveTargets.Length)
            return;

        if (moveTargets[index] == null)
            return;

        if (moveCoroutines[index] != null)
            StopCoroutine(moveCoroutines[index]);

        Vector2 targetPosition = originPositions[index];

        if (selected)
            targetPosition.x += selectedXOffset;

        moveCoroutines[index] = StartCoroutine(MoveRoutine(index, targetPosition, selected));
    }

    private IEnumerator MoveRoutine(int index, Vector2 targetPosition, bool selected)
    {
        RectTransform target = moveTargets[index];

        if (target == null)
            yield break;

        Vector2 startPosition = target.anchoredPosition;
        Vector2 overshootPosition = targetPosition;

        if (useOvershoot)
        {
            if (selected)
                overshootPosition.x += overshootAmount;
            else
                overshootPosition.x -= overshootAmount;
        }

        float firstDuration = useOvershoot ? moveDuration * 0.7f : moveDuration;
        float secondDuration = moveDuration * 0.3f;

        yield return MoveTo(target, startPosition, overshootPosition, firstDuration);

        if (useOvershoot)
            yield return MoveTo(target, overshootPosition, targetPosition, secondDuration);

        target.anchoredPosition = targetPosition;
        moveCoroutines[index] = null;
    }

    private IEnumerator MoveTo(RectTransform target, Vector2 from, Vector2 to, float duration)
    {
        if (duration <= 0f)
        {
            target.anchoredPosition = to;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / duration;
            t = EaseOutCubic(t);

            target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);

            yield return null;
        }

        target.anchoredPosition = to;
    }

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t = 1f - Mathf.Pow(1f - t, 3f);
        return t;
    }

    private void SetMoveInstant(int index, bool selected)
    {
        if (index < 0 || index >= moveTargets.Length)
            return;

        if (moveTargets[index] == null)
            return;

        Vector2 position = originPositions[index];

        if (selected)
            position.x += selectedXOffset;

        moveTargets[index].anchoredPosition = position;
    }

    private int GetFirstOnIndex()
    {
        if (toggles == null)
            return -1;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] != null && toggles[i].isOn)
                return i;
        }

        return -1;
    }

    private int GetToggleIndex(Toggle target)
    {
        if (target == null)
            return -1;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == target)
                return i;
        }

        return -1;
    }
}