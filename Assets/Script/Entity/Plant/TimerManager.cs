using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TimerIndexEvent : UnityEvent<int> { }

public class TimerManager : MonoBehaviour
{
    [Header("Item")]
    public ItemEffectExecutor itemEffectExecutor;
    public ItemData[] itemDatas;

    [Header("Timer")]
    [Tooltip("각 인덱스마다 기다릴 시간. 예: 60, 60, 60이면 1분마다 3번 실행")]
    public List<float> timerSteps = new List<float>();

    [Tooltip("게임 시작 시 자동 실행")]
    public bool playOnStart = true;

    [Tooltip("마지막 인덱스까지 끝난 뒤 반복할지")]
    public bool loop = false;

    [Header("Event")]
    public TimerIndexEvent onTimerTick;

    [Header("Debug")]
    [SerializeField] private bool isRunning;
    [SerializeField] private int currentIndex;
    [SerializeField] private float timer;

    public bool IsRunning => isRunning;
    public int CurrentIndex => currentIndex;
    public float Timer => timer;

    private void Start()
    {
        if (playOnStart)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (timerSteps == null || timerSteps.Count == 0)
            return;

        if (currentIndex < 0 || currentIndex >= timerSteps.Count)
        {
            FinishTimer();
            return;
        }

        timer += Time.deltaTime;

        float targetTime = Mathf.Max(0f, timerSteps[currentIndex]);

        if (timer < targetTime)
            return;

        timer = 0f;

        ExecuteTimerAction(currentIndex);

        currentIndex++;

        if (currentIndex >= timerSteps.Count)
        {
            if (loop)
            {
                currentIndex = 0;
            }
            else
            {
                FinishTimer();
            }
        }
    }

    public void StartTimer()
    {
        currentIndex = 0;
        timer = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        if (timerSteps == null || timerSteps.Count == 0)
            return;

        isRunning = true;
    }

    public void ResetTimer()
    {
        currentIndex = 0;
        timer = 0f;
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    private void FinishTimer()
    {
        isRunning = false;
        timer = 0f;
    }

    private void ExecuteTimerAction(int index)
    {
        // 함수 A 실행
        FunctionA(index);

        // 인스펙터에서 연결한 함수들도 실행
        onTimerTick?.Invoke(index);
    }

    private void FunctionA(int index)
    {
        itemEffectExecutor.JustExcuteItem(itemDatas[index]);
        Debug.Log($"Timer Function A 실행 / Index: {index}");
    }
}