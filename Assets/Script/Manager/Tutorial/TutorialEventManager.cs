using UnityEngine;

public class TutorialEventManager : MonoBehaviour
{
    [Header("Events")]
    public TutorialProgressEvent[] progressEvents;

    [Header("Option")]
    public bool invokeOnStart = false;

    private void OnEnable()
    {
        if (TutorialProgressManager.instance != null)
            TutorialProgressManager.instance.OnProgressChanged += HandleProgressChanged;
    }


    private void OnDisable()
    {
        if (TutorialProgressManager.instance != null)
            TutorialProgressManager.instance.OnProgressChanged -= HandleProgressChanged;
    }

    public void TryHandleCurrentProgress()
    {
        if (TutorialProgressManager.instance == null)
        {
            Debug.LogWarning("TutorialProgressManager가 없습니다.");
            return;
        }

        HandleProgressChanged(
            TutorialProgressManager.instance.currentProgress
        );
    }

    public bool TryHandleProgressChanged(TutorialProgress targetProgress)
    {
        if (TutorialProgressManager.instance == null)
        {
            Debug.LogWarning("TutorialProgressManager가 없습니다.");
            return false;
        }

        TutorialProgress currentProgress =
            TutorialProgressManager.instance.currentProgress;

        if (currentProgress != targetProgress)
            return false;

        HandleProgressChanged(currentProgress);
        return true;
    }

    private void HandleProgressChanged(TutorialProgress progress)
    {
        InvokeProgressEvent(progress);
    }

    private void InvokeProgressEvent(TutorialProgress progress)
    {
        if (progressEvents == null)
            return;

        for (int i = 0; i < progressEvents.Length; i++)
        {
            TutorialProgressEvent progressEvent = progressEvents[i];

            if (progressEvent == null)
                continue;

            if (progressEvent.progress != progress)
                continue;

            if (progressEvent.invokeOnlyOnce && progressEvent.hasInvoked)
                continue;

            progressEvent.hasInvoked = true;
            progressEvent.onReached?.Invoke();
        }
    }

    public void ResetInvokeState()
    {
        if (progressEvents == null)
            return;

        for (int i = 0; i < progressEvents.Length; i++)
        {
            if (progressEvents[i] == null)
                continue;

            progressEvents[i].hasInvoked = false;
        }
    }
}