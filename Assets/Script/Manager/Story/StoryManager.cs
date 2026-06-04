using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [Header("Chapters")]
    public StoryChapterPanel[] chapterPanels;

    [Header("Scene")]
    public string returnSceneName = "GameScene";

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button skipButton;

    private int currentChapterIndex;
    private int currentCutIndex;

    private void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(PrevCut);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextCut);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipStory);
    }

    private void Start()
    {
        StartStory();
    }

    private void StartStory()
    {
        if (chapterPanels == null || chapterPanels.Length == 0)
        {
            Debug.LogWarning("등록된 챕터 패널이 없습니다.");
            MoveToReturnScene();
            return;
        }

        currentChapterIndex = StorySave.GetStoryChapterProgress();
        currentCutIndex = 0;

        if (currentChapterIndex >= chapterPanels.Length)
        {
            MoveToReturnScene();
            return;
        }

        ClampProgress();
        RefreshView();
    }

    public void NextCut()
    {
        StoryChapterPanel currentChapter = GetCurrentChapter();

        if (currentChapter == null)
            return;

        if (currentCutIndex < currentChapter.CutCount - 1)
        {
            currentCutIndex++;
            RefreshView();
            return;
        }

        CompleteCurrentChapter();
    }

    public void PrevCut()
    {
        if (currentCutIndex <= 0)
            return;

        currentCutIndex--;
        RefreshView();
    }

    public void SkipStory()
    {
        CompleteCurrentChapter();
    }

    private void CompleteCurrentChapter()
    {
        currentChapterIndex++;
        StorySave.SetStoryChapterProgress(currentChapterIndex);
        MoveToReturnScene();
    }

    private void RefreshView()
    {
        RefreshChapters();
        RefreshButtons();
    }

    private void RefreshChapters()
    {
        for (int i = 0; i < chapterPanels.Length; i++)
        {
            StoryChapterPanel chapter = chapterPanels[i];

            if (chapter == null)
                continue;

            bool isCurrentChapter = i == currentChapterIndex;
            chapter.gameObject.SetActive(isCurrentChapter);

            if (isCurrentChapter)
                chapter.ShowCutProgress(currentCutIndex);
            else
                chapter.HideAllCuts();
        }
    }

    private void RefreshButtons()
    {
        if (prevButton != null)
            prevButton.interactable = currentCutIndex > 0;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    private void MoveToReturnScene()
    {
        if (SceneMoveManager.instance != null)
            SceneMoveManager.instance.LoadScene(returnSceneName);
        else
            Debug.LogWarning("SceneMoveManager가 없습니다.");
    }

    private void ClampProgress()
    {
        if (currentChapterIndex < 0)
            currentChapterIndex = 0;

        StoryChapterPanel currentChapter = GetCurrentChapter();

        if (currentChapter == null || currentChapter.CutCount <= 0)
        {
            currentCutIndex = 0;
            return;
        }

        if (currentCutIndex < 0)
            currentCutIndex = 0;

        if (currentCutIndex >= currentChapter.CutCount)
            currentCutIndex = currentChapter.CutCount - 1;
    }

    private StoryChapterPanel GetCurrentChapter()
    {
        if (chapterPanels == null)
            return null;

        if (currentChapterIndex < 0 || currentChapterIndex >= chapterPanels.Length)
            return null;

        return chapterPanels[currentChapterIndex];
    }

    public void ResetStory()
    {
        StorySave.ResetStoryProgress();

        currentChapterIndex = 0;
        currentCutIndex = 0;

        RefreshView();
    }
}