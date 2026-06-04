using System;
using System.Collections;
using UnityEngine;

public class TutorialSequencePlayer : MonoBehaviour
{
    public static TutorialSequencePlayer instance;

    [Header("Executor")]
    public TutorialEventExecutor executor;

    [Header("Runtime")]
    [SerializeField] private TutorialSequenceData currentSequence;
    [SerializeField] private int currentStepIndex = -1;
    [SerializeField] private bool isPlaying;
    [SerializeField] private bool waitingForInput;

    private Coroutine autoNextCoroutine;

    public bool IsPlaying => isPlaying;
    public bool WaitingForInput => waitingForInput;
    public TutorialSequenceData CurrentSequence => currentSequence;
    public int CurrentStepIndex => currentStepIndex;

    public event Action<TutorialSequenceData> OnSequenceCompleted;
    public event Action<TutorialSequenceData> OnSequenceStopped;

    private void Awake()
    {
        instance = this;
    }

    public void Play(TutorialSequenceData sequence)
    {
        if (sequence == null)
            return;

        if (isPlaying)
            StopSequence();


        ClearRuntime();

        currentSequence = sequence;
        currentStepIndex = -1;
        isPlaying = true;
        waitingForInput = false;

        NextStep();
    }

    public void StopSequence()
    {
        if (!isPlaying && currentSequence == null)
            return;

        TutorialSequenceData stoppedSequence = currentSequence;

        ClearRuntime();

        Debug.Log("튜토리얼 시퀀스 중단");

        OnSequenceStopped?.Invoke(stoppedSequence);
    }

    private void CompleteSequence()
    {
        if (currentSequence == null)
        {
            ClearRuntime();
            return;
        }

        TutorialSequenceData completedSequence = currentSequence;

        ClearRuntime();


        OnSequenceCompleted?.Invoke(completedSequence);
    }

    private void ClearRuntime()
    {
        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }

        currentSequence = null;
        currentStepIndex = -1;
        isPlaying = false;
        waitingForInput = false;
    }

    public void NextStep()
    {
        if (!isPlaying)
            return;

        if (currentSequence == null || currentSequence.steps == null)
        {
            StopSequence();
            return;
        }

        if (autoNextCoroutine != null)
        {
            StopCoroutine(autoNextCoroutine);
            autoNextCoroutine = null;
        }

        waitingForInput = false;
        currentStepIndex++;

        if (currentStepIndex >= currentSequence.steps.Length)
        {
            CompleteSequence();
            return;
        }

        TutorialSequenceStep step = currentSequence.steps[currentStepIndex];

        if (step == null)
        {
            NextStep();
            return;
        }

        ExecuteStep(step);

        if (step.autoNextDelay > 0f)
        {
            autoNextCoroutine = StartCoroutine(AutoNextRoutine(step.autoNextDelay));
            return;
        }

        if (step.waitForNextInput)
        {
            waitingForInput = true;
            return;
        }

        NextStep();
    }

    public void OnClickNext()
    {
        if (!isPlaying)
            return;

        if (!waitingForInput)
            return;

        NextStep();
    }

    private void ExecuteStep(TutorialSequenceStep step)
    {
        if (executor == null)
        {
            Debug.LogWarning("TutorialEventExecutor가 없습니다.");
            return;
        }

        executor.Execute(step);
    }

    private IEnumerator AutoNextRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        autoNextCoroutine = null;
        NextStep();
    }
}