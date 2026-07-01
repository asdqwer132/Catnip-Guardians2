using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleAnimationGroup : MonoBehaviour
{
    [Serializable]
    public class ToggleMoveSet
    {
        public Toggle toggle;
        public RectTransform[] moveTargets;
    }

    [Header("Toggle Sets")]
    public ToggleMoveSet[] toggleSets;

    [Header("Move")]
    public float selectedXOffset = 20f;
    public float moveDuration = 0.12f;

    [Header("Effect")]
    public bool useOvershoot = true;
    public float overshootAmount = 6f;

    private int currentIndex = -1;
    private Action<bool>[] valueChangedActions;
    private bool isChanging;

    private readonly Dictionary<RectTransform, Vector2> originPositionMap = new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<RectTransform, Coroutine> coroutineMap = new Dictionary<RectTransform, Coroutine>();

    private void Awake()
    {
        AutoFindTogglesIfEmpty();
        CacheOriginPositions();
        BindToggles();
    }

    private void Start()
    {
        int firstIndex = GetFirstOnIndex();

        if (firstIndex >= 0)
        {
            currentIndex = firstIndex;
            SetMoveInstant(firstIndex, true);
        }
    }

    private void OnDestroy()
    {
        if (toggleSets == null || valueChangedActions == null)
            return;

        for (int i = 0; i < toggleSets.Length; i++)
        {
            if (toggleSets[i]?.toggle != null && valueChangedActions[i] != null)
                toggleSets[i].toggle.onValueChanged.RemoveListener(valueChangedActions[i].Invoke);
        }
    }

    private void AutoFindTogglesIfEmpty()
    {
        if (toggleSets != null && toggleSets.Length > 0)
            return;

        Toggle[] foundToggles = GetComponentsInChildren<Toggle>(true);
        toggleSets = new ToggleMoveSet[foundToggles.Length];

        for (int i = 0; i < foundToggles.Length; i++)
        {
            Toggle toggle = foundToggles[i];

            toggleSets[i] = new ToggleMoveSet();
            toggleSets[i].toggle = toggle;

            RectTransform target = null;
            Transform visual = toggle.transform.Find("Visual");

            if (visual != null)
                target = visual as RectTransform;

            if (target == null)
                target = toggle.transform as RectTransform;

            toggleSets[i].moveTargets = target != null
                ? new[] { target }
                : Array.Empty<RectTransform>();
        }
    }

    private void CacheOriginPositions()
    {
        originPositionMap.Clear();
        coroutineMap.Clear();

        if (toggleSets == null)
            return;

        for (int i = 0; i < toggleSets.Length; i++)
        {
            if (toggleSets[i] == null || toggleSets[i].moveTargets == null)
                continue;

            RectTransform[] targets = toggleSets[i].moveTargets;

            for (int j = 0; j < targets.Length; j++)
            {
                RectTransform target = targets[j];

                if (target == null)
                    continue;

                if (!originPositionMap.ContainsKey(target))
                    originPositionMap.Add(target, target.anchoredPosition);

                if (!coroutineMap.ContainsKey(target))
                    coroutineMap.Add(target, null);
            }
        }
    }

    private void BindToggles()
    {
        valueChangedActions = new Action<bool>[toggleSets.Length];

        for (int i = 0; i < toggleSets.Length; i++)
        {
            Toggle toggle = toggleSets[i].toggle;

            if (toggle == null)
                continue;

            int index = i;

            valueChangedActions[i] = (isOn) =>
            {
                if (isChanging)
                    return;

                if (isOn)
                    SelectToggle(index);
            };

            toggle.onValueChanged.AddListener(valueChangedActions[i].Invoke);
        }
    }

    public void SelectToggle(int selectedIndex)
    {
        if (isChanging)
            return;

        if (!IsValidIndex(selectedIndex))
            return;

        Toggle selectedToggle = toggleSets[selectedIndex].toggle;

        if (selectedToggle == null)
            return;

        if (currentIndex == selectedIndex)
            return;

        isChanging = true;

        int previousIndex = currentIndex;
        currentIndex = selectedIndex;

        if (previousIndex >= 0 && IsValidIndex(previousIndex))
        {
            Toggle previousToggle = toggleSets[previousIndex].toggle;

            if (previousToggle != null)
                previousToggle.SetIsOnWithoutNotify(false);

            MoveSet(previousIndex, false, selectedIndex);
        }

        selectedToggle.SetIsOnWithoutNotify(true);
        MoveSet(selectedIndex, true, previousIndex);

        isChanging = false;
    }

    private void MoveSet(int setIndex, bool selected, int compareIndex)
    {
        if (!IsValidIndex(setIndex))
            return;

        RectTransform[] targets = toggleSets[setIndex].moveTargets;

        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            RectTransform target = targets[i];

            if (target == null)
                continue;

            if (IsTargetSharedWithSet(target, compareIndex))
                continue;

            MoveTarget(target, selected);
        }
    }

    private void MoveTarget(RectTransform target, bool selected)
    {
        if (target == null)
            return;

        if (!originPositionMap.ContainsKey(target))
            originPositionMap.Add(target, target.anchoredPosition);

        if (!coroutineMap.ContainsKey(target))
            coroutineMap.Add(target, null);

        Coroutine runningCoroutine = coroutineMap[target];

        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);

        Vector2 targetPosition = originPositionMap[target];

        if (selected)
            targetPosition.x += selectedXOffset;

        coroutineMap[target] = StartCoroutine(MoveRoutine(target, targetPosition, selected));
    }

    private IEnumerator MoveRoutine(RectTransform target, Vector2 targetPosition, bool selected)
    {
        if (target == null)
            yield break;

        Vector2 startPosition = target.anchoredPosition;
        Vector2 overshootPosition = targetPosition;

        if (useOvershoot)
            overshootPosition.x += selected ? overshootAmount : -overshootAmount;

        float firstDuration = useOvershoot ? moveDuration * 0.7f : moveDuration;
        float secondDuration = moveDuration * 0.3f;

        yield return MoveTo(target, startPosition, overshootPosition, firstDuration);

        if (useOvershoot)
            yield return MoveTo(target, overshootPosition, targetPosition, secondDuration);

        target.anchoredPosition = targetPosition;
        coroutineMap[target] = null;
    }

    private IEnumerator MoveTo(RectTransform target, Vector2 from, Vector2 to, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.anchoredPosition = to;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            if (target == null)
                yield break;

            time += Time.unscaledDeltaTime;

            float t = EaseOutCubic(time / duration);
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);

            yield return null;
        }

        target.anchoredPosition = to;
    }

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void SetMoveInstant(int setIndex, bool selected)
    {
        if (!IsValidIndex(setIndex))
            return;

        RectTransform[] targets = toggleSets[setIndex].moveTargets;

        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            RectTransform target = targets[i];

            if (target == null)
                continue;

            if (!originPositionMap.ContainsKey(target))
                originPositionMap.Add(target, target.anchoredPosition);

            Vector2 position = originPositionMap[target];

            if (selected)
                position.x += selectedXOffset;

            target.anchoredPosition = position;
        }
    }

    private bool IsTargetSharedWithSet(RectTransform target, int setIndex)
    {
        if (target == null)
            return false;

        if (!IsValidIndex(setIndex))
            return false;

        RectTransform[] targets = toggleSets[setIndex].moveTargets;

        if (targets == null)
            return false;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == target)
                return true;
        }

        return false;
    }

    private int GetFirstOnIndex()
    {
        if (toggleSets == null)
            return -1;

        for (int i = 0; i < toggleSets.Length; i++)
        {
            if (toggleSets[i]?.toggle != null && toggleSets[i].toggle.isOn)
                return i;
        }

        return -1;
    }

    private bool IsValidIndex(int index)
    {
        return toggleSets != null &&
               index >= 0 &&
               index < toggleSets.Length &&
               toggleSets[index] != null;
    }
}