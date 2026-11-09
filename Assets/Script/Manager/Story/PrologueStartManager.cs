using UnityEngine;

public class PrologueStartManager : MonoBehaviour
{
    public void StartGame()
    {
        if (SceneMoveManager.instance == null)
        {
            Debug.LogWarning("SceneMoveManager가 없습니다.");
            return;
        }
        if (SceneMoveManager.instance.IsLoading) return;
        if (!GameSaveData.ResetAll()) return;
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
