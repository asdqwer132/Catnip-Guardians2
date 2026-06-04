using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager")]
    public InitManager initManager;
    public RoundManager roundManager;
    public TutorialEventManager tutorialEventManager;

    private void Start()
    {
        if (initManager != null)
            initManager.FirstInit();

        StartNextRound();
    }

    public void Victory()
    {
        initManager.ResetEntity();
        if (roundManager != null)
            roundManager.Victory();
    }

    public void GameOver()
    {
        initManager.ResetEntity();
        if (roundManager != null)
            roundManager.GameOver();

        if (tutorialEventManager != null)
        {
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstItem);
        }
    }
    public void StartNextRound()
    {
        if (initManager != null)
            initManager.InitAll();

        if (roundManager != null)
            roundManager.StartNextRound();

        if (tutorialEventManager != null)
        {
            if (tutorialEventManager.TryHandleProgressChanged(TutorialProgress.None))
            {
                EnemyManager.instance.AllStop();
                Invoke(nameof(Test), 3f);
            }
        }
    }
    private void Test()
    {
        EnemyManager.instance.AllStart();

    }
}