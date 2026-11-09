using UnityEngine;
using UnityEngine.UI;

public class PrologueStartManager : MonoBehaviour
{
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private SceneMoveManager moveManagerRoot;
    private SceneMoveManager moveManager;
    private void Awake()
    {
        moveManager = moveManagerRoot != null ? moveManagerRoot : SceneMoveManager.instance;
        if (continueBtn != null) continueBtn.onClick.AddListener(ContinueGame);
        if (newGameBtn != null) newGameBtn.onClick.AddListener(NewGame);
        if (exitBtn != null) exitBtn.onClick.AddListener(ExitGame);
    }
    public void NewGame()
    {
        StorySave.ResetStoryProgress();
        TutorialCompletionHistory.ResetAll();

        moveManager.GoStoryScene();
    }
    public void ContinueGame()
    {
        moveManager.GoGameScene();
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
