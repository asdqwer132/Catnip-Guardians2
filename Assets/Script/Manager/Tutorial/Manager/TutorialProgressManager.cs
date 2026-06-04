using System;
using UnityEngine;

public class TutorialProgressManager : MonoBehaviour
{
    public static TutorialProgressManager instance;

    [Header("Progress")]
    public TutorialProgress currentProgress;

    [Header("Option")]
    public bool loadOnAwake = true;
    public bool saveOnChange = true;

    public event Action<TutorialProgress> OnProgressChanged;
    public event Action<TutorialProgress, TutorialProgress> OnProgressChangedDetailed;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (loadOnAwake)
            currentProgress = TutorialSave.GetProgress();
    }

    public void IncreaseProgress()
    {
        int nextValue = (int)currentProgress + 1;
        SetProgress((TutorialProgress)nextValue);
    }

    public void DecreaseProgress()
    {
        int nextValue = Mathf.Max(0, (int)currentProgress - 1);
        SetProgress((TutorialProgress)nextValue);
    }

    public void SetProgress(TutorialProgress progress)
    {
        TutorialProgress previousProgress = currentProgress;

        if (previousProgress == progress)
            return;

        currentProgress = progress;

        if (saveOnChange)
            TutorialSave.SetProgress(currentProgress);

        NotifyProgressChanged(previousProgress, currentProgress);
    }

    public void SetProgressByInt(int progress)
    {
        SetProgress((TutorialProgress)progress);
    }

    public void ResetProgress()
    {
        TutorialSave.ResetProgress();

        TutorialProgress previousProgress = currentProgress;
        currentProgress = TutorialProgress.FirstEnemyAttack;

        NotifyProgressChanged(previousProgress, currentProgress);
    }

    public bool IsProgress(TutorialProgress progress)
    {
        return currentProgress == progress;
    }

    public bool IsProgressAtLeast(TutorialProgress progress)
    {
        return (int)currentProgress >= (int)progress;
    }

    public void NotifyCurrentProgress()
    {
        NotifyProgressChanged(currentProgress, currentProgress);
    }

    private void NotifyProgressChanged(
        TutorialProgress previousProgress,
        TutorialProgress nextProgress
    )
    {
        OnProgressChanged?.Invoke(nextProgress);
        OnProgressChangedDetailed?.Invoke(previousProgress, nextProgress);
    }
}