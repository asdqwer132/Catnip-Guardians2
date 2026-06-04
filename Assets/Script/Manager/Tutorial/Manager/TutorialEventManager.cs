using UnityEngine;

public class TutorialEventManager : MonoBehaviour
{
    public GameObject tutoCanvas;
    [Header("Sequences")]
    public TutorialSequenceData[] sequences;

    [Header("Option")]
    public bool invokeOnStart = false;

    private TutorialSequencePlayer subscribedPlayer;

    private void OnEnable()
    {
        if (TutorialProgressManager.instance != null)
            TutorialProgressManager.instance.OnProgressChanged += HandleProgressChanged;

        TrySubscribeSequencePlayer();
    }

    private void Start()
    {
        if(tutoCanvas != null)
        tutoCanvas.SetActive(false);
    }

    private void OnDisable()
    {
        if (TutorialProgressManager.instance != null)
            TutorialProgressManager.instance.OnProgressChanged -= HandleProgressChanged;

        UnsubscribeSequencePlayer();
    }

    public bool TryHandleCurrentProgress()
    {
        if (TutorialProgressManager.instance == null)
        {
            Debug.LogWarning("TutorialProgressManager가 없습니다.");
            return false;
        }

        return PlaySequence(
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
        Time.timeScale = 0f;
        tutoCanvas.SetActive(true);
        return PlaySequence(currentProgress);
    }

    private void HandleProgressChanged(TutorialProgress progress)
    {
        PlaySequence(progress);
    }

    private bool PlaySequence(TutorialProgress progress)
    {
        TutorialSequenceData sequence = GetSequence(progress);

        if (sequence == null)
            return false;

        if (TutorialSequencePlayer.instance == null)
        {
            Debug.LogWarning("TutorialSequencePlayer가 없습니다.");
            return false;
        }

        TrySubscribeSequencePlayer();
        TutorialSequencePlayer.instance.Play(sequence);
        return true;
    }

    private TutorialSequenceData GetSequence(TutorialProgress progress)
    {
        if (sequences == null)
            return null;

        for (int i = 0; i < sequences.Length; i++)
        {
            if (sequences[i] == null)
                continue;

            if (sequences[i].progress == progress)
                return sequences[i];
        }

        return null;
    }

    private void TrySubscribeSequencePlayer()
    {
        TutorialSequencePlayer player = TutorialSequencePlayer.instance;

        if (player == null)
            return;

        if (subscribedPlayer == player)
            return;

        UnsubscribeSequencePlayer();

        subscribedPlayer = player;
        subscribedPlayer.OnSequenceCompleted += HandleSequenceCompleted;
        subscribedPlayer.OnSequenceStopped += HandleSequenceStopped;
    }

    private void UnsubscribeSequencePlayer()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnSequenceCompleted -= HandleSequenceCompleted;
        subscribedPlayer.OnSequenceStopped -= HandleSequenceStopped;
        subscribedPlayer = null;
    }

    private void HandleSequenceCompleted(TutorialSequenceData sequence)
    {
        if (sequence == null)
            return;

        Time.timeScale = 1f;
        tutoCanvas.SetActive(false);
        TutorialProgressManager.instance.IncreaseProgress();
    }

    private void HandleSequenceStopped(TutorialSequenceData sequence)
    {
        if (sequence == null)
            return;

    }
}