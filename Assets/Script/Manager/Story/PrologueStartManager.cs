using UnityEngine;

public class PrologueStartManager : MonoBehaviour
{
    public void StartGame()
    {
        StorySave.ResetStoryProgress();
        TutorialCompletionHistory.ResetAll();
        if (SceneMoveManager.instance == null)
        {
            Debug.LogWarning("SceneMoveManager가 없습니다.");
            return;
        }
        SceneMoveManager.instance.GoStoryScene();
    }
}
