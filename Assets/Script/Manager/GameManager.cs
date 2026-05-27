using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Manager")]
    public InitManager initManager;
    public RoundManager roundManager;

    private void Start()
    {
        initManager.FirstInit();
        StartNextRound();
    }

    public void Victory()
    {
        roundManager.Victory();
    }

    public void GameOver()
    {
        roundManager.GameOver();
    }

    public void StartNextRound()
    {
        initManager.InitAll();
        roundManager.StartNextRound();
    }
}