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
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
