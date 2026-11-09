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
        var manager = ReadyMoveManager();
        if (manager == null || manager.IsLoading) return;
        if (!GameSaveData.ResetAll()) return;
        manager.GoStoryScene();
    }
    // Keep older serialized button bindings working as New Game.
    public void StartGame() => NewGame();

    public void ContinueGame()
    {
        var manager = ReadyMoveManager();
        if (manager != null && !manager.IsLoading) manager.GoGameScene();
    }
    private SceneMoveManager ReadyMoveManager()
    {
        if (moveManager == null)
            moveManager = moveManagerRoot != null ? moveManagerRoot : SceneMoveManager.instance;
        if (moveManager == null) Debug.LogWarning("SceneMoveManager가 없습니다.", this);
        return moveManager;
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
