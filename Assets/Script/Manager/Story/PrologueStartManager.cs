using UnityEngine;

public class PrologueStartManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string storySceneName = "StoryScene";
    public string gameSceneName = "GameScene";

    public void StartGame()
    {
        if (SceneMoveManager.instance == null)
        {
            Debug.LogWarning("SceneMoveManager가 없습니다.");
            return;
        }

        if (StorySave.HasSeenPrologue())
            SceneMoveManager.instance.LoadScene(gameSceneName);
        else
            SceneMoveManager.instance.LoadScene(storySceneName);
    }

    public void ResetStory()
    {
        StorySave.ResetStoryProgress();
    }
}