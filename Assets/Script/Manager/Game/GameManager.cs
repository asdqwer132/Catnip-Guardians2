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
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstItemEquip);
        }
        AudioManager.instance.PlayBgm("Ambient 6 ");
    }
    public void StartNextRound()
    {
        if (initManager != null)
            initManager.InitAll();

        if (roundManager != null)
            roundManager.StartNextRound();

        if (tutorialEventManager != null)
        {
            tutorialEventManager.TryHandleProgressChanged(TutorialProgress.FirstEnemyAttack);
        }


        AudioManager.instance.PlayBgm("Ambient 7 ");
    }
}