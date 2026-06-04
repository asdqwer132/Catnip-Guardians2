using UnityEngine;

public class TutorialEventExecutor : MonoBehaviour
{
    [Header("Managers")]
    public TutorialProgressManager progressManager;

    [Header("Feature Controllers")]
    public TutorialDialogueController dialogueController;
    public TutorialHighlightController highlightController;

    [Header("Objects")]
    public GameObject[] targetObjects;

    [Header("Scene")]
    public string defaultSceneName = "GameScene";

    private void Awake()
    {
        if (progressManager == null)
            progressManager = TutorialProgressManager.instance;
    }

    public void Execute(TutorialSequenceStep step)
    {
        if (step == null)
            return;

        switch (step.eventType)
        {
            case TutorialEventType.None:
                break;

            case TutorialEventType.DialogueShow:
                ShowDialogue();
                break;

            case TutorialEventType.DialogueHide:
                HideDialogue();
                break;

            case TutorialEventType.DialogueSetText:
                SetDialogueText(step.stringValue);
                break;

            case TutorialEventType.DialogueSetSpeaker:
                SetSpeakerName(step.stringValue);
                break;

            case TutorialEventType.DialogueClear:
                ClearDialogue();
                break;

            case TutorialEventType.HighlightShow:
                ShowHighlight(step.intValue);
                break;

            case TutorialEventType.HighlightHide:
                HideHighlight();
                break;

            case TutorialEventType.ObjectShow:
                ShowTargetObject(step.intValue);
                break;

            case TutorialEventType.ObjectHide:
                HideTargetObject(step.intValue);
                break;

            case TutorialEventType.ObjectShowOnly:
                ShowOnlyTargetObject(step.intValue);
                break;

            case TutorialEventType.ObjectHideAll:
                HideAllTargetObjects();
                break;

            case TutorialEventType.EnemyStop:
                StopEnemies();
                break;

            case TutorialEventType.EnemyStart:
                StartEnemies();
                break;

            case TutorialEventType.GamePause:
                PauseGame();
                break;

            case TutorialEventType.GameResume:
                ResumeGame();
                break;

            case TutorialEventType.ProgressNext:
                NextProgress();
                break;

            case TutorialEventType.ProgressPrev:
                PrevProgress();
                break;

            case TutorialEventType.ProgressSet:
                SetProgressByInt(step.intValue);
                break;

            case TutorialEventType.SceneLoad:
                if (string.IsNullOrEmpty(step.stringValue))
                    LoadSceneByName(defaultSceneName);
                else
                    LoadSceneByName(step.stringValue);
                break;
        }
    }

    public void NextProgress()
    {
        GetProgressManager();

        if (progressManager == null)
            return;

        progressManager.IncreaseProgress();
    }

    public void PrevProgress()
    {
        GetProgressManager();

        if (progressManager == null)
            return;

        progressManager.DecreaseProgress();
    }

    public void SetProgress(TutorialProgress progress)
    {
        GetProgressManager();

        if (progressManager == null)
            return;

        progressManager.SetProgress(progress);
    }

    public void SetProgressByInt(int progress)
    {
        SetProgress((TutorialProgress)progress);
    }

    public void ShowDialogue()
    {
        if (dialogueController != null)
            dialogueController.Show();
    }

    public void HideDialogue()
    {
        if (dialogueController != null)
            dialogueController.Hide();
    }

    public void SetSpeakerName(string speakerName)
    {
        if (dialogueController != null)
            dialogueController.SetSpeakerName(speakerName);
    }

    public void SetDialogueText(string text)
    {
        if (dialogueController != null)
            dialogueController.SetText(text);
    }

    public void ClearDialogue()
    {
        if (dialogueController != null)
            dialogueController.Clear();
    }

    public void ShowHighlight(int index)
    {
        if (highlightController != null)
            highlightController.ShowHighlight(index);
    }

    public void HideHighlight()
    {
        if (highlightController != null)
            highlightController.HideHighlight();
    }

    public void ShowTargetObject(int index)
    {
        GameObject target = GetTargetObject(index);

        if (target != null)
            target.SetActive(true);
    }

    public void HideTargetObject(int index)
    {
        GameObject target = GetTargetObject(index);

        if (target != null)
            target.SetActive(false);
    }

    public void ShowOnlyTargetObject(int index)
    {
        if (targetObjects == null)
            return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] == null)
                continue;

            targetObjects[i].SetActive(i == index);
        }
    }

    public void HideAllTargetObjects()
    {
        if (targetObjects == null)
            return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] == null)
                continue;

            targetObjects[i].SetActive(false);
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void StopEnemies()
    {
        if (EnemyManager.instance != null)
            EnemyManager.instance.AllStop();
    }

    public void StartEnemies()
    {
        if (EnemyManager.instance != null)
            EnemyManager.instance.AllStart();
    }

    public void LoadSceneByName(string sceneName)
    {
        if (SceneMoveManager.instance == null)
        {
            Debug.LogWarning("SceneMoveManager가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어있습니다.");
            return;
        }

        SceneMoveManager.instance.LoadScene(sceneName);
    }

    private void GetProgressManager()
    {
        if (progressManager == null)
            progressManager = TutorialProgressManager.instance;

        if (progressManager == null)
            Debug.LogWarning("TutorialProgressManager가 없습니다.");
    }

    private GameObject GetTargetObject(int index)
    {
        if (targetObjects == null)
            return null;

        if (index < 0 || index >= targetObjects.Length)
            return null;

        return targetObjects[index];
    }
}