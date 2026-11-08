using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour, ISceneLocalizedUI
{
    [Header("Stories")]
    [SerializeField] private StoryData[] stories;

    [Header("Cuts")]
    [SerializeField] private Image[] cuts;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerText;

    [Header("Scene")]
    [SerializeField] private string returnSceneName = GameSceneRoutes.General;

    [Header("Buttons")]
    [SerializeField] private Button[] prevButtons;
    [SerializeField] private Button[] nextButtons;
    [SerializeField] private Button[] skipButtons;

    [Header("Index")]
    [SerializeField] private bool isTest = false;
    [SerializeField] private int testIndex = 0;

    private int currentStoryIndex;
    private int currentDialogueIndex;
    private int currentCutIndex;
    private bool isPlaying;

    private StoryData CurrentStory => stories[currentStoryIndex];

    private void Awake()
    {
        foreach (var btn in prevButtons)
            if (btn != null)
                btn.onClick.AddListener(PrevCut);

        foreach (var btn in nextButtons)
            if (btn != null)
                btn.onClick.AddListener(NextCut);

        foreach (var btn in skipButtons)
            if (btn != null)
                btn.onClick.AddListener(SkipStory);
    }

    private void OnEnable()
    {
        SceneLanguageMediator.Register(this);
    }

    private void OnDisable()
    {
        SceneLanguageMediator.Unregister(this, gameObject.scene);
    }

    private void Start()
    {
        if (isPlaying) return;

        StartStory(isTest
            ? testIndex
            : StorySave.GetStoryChapterProgress());
    }

    public void StartStory(int index)
    {
        if (stories == null ||
            index < 0 ||
            index >= stories.Length ||
            stories[index] == null)
        {
            MoveToReturnScene();
            return;
        }

        currentStoryIndex = index;
        currentDialogueIndex = 0;
        currentCutIndex = 0;
        isPlaying = true;

        RefreshView();
    }

    public void NextCut()
    {
        if (!isPlaying) return;

        var dialogues = CurrentStory.dialogues;

        if (dialogues == null ||
            currentDialogueIndex >= dialogues.Length - 1)
        {
            CompleteCurrentStory();
            return;
        }

        currentDialogueIndex++;
        RefreshView();
    }

    public void PrevCut()
    {
        if (!isPlaying || currentDialogueIndex <= 0)
            return;

        currentDialogueIndex--;
        RefreshView();
    }

    public void SkipStory()
    {
        if (isPlaying)
            CompleteCurrentStory();
    }

    private void CompleteCurrentStory()
    {
        isPlaying = false;

        StorySave.SetStoryChapterProgress(
            Mathf.Max(
                StorySave.GetStoryChapterProgress(),
                currentStoryIndex + 1
            )
        );

        MoveToReturnScene();
    }

    private void RefreshView()
    {
        RefreshCuts();
        RefreshLanguage();
        RefreshButtons();
    }

    private void RefreshCuts()
    {
        var story = CurrentStory;
        var dialogues = story.dialogues;

        currentCutIndex = 0;

        if (dialogues != null)
        {
            for (int i = 0;
                 i <= currentDialogueIndex && i < dialogues.Length;
                 i++)
            {
                if (dialogues[i] != null && dialogues[i].nextImage)
                    currentCutIndex++;
            }
        }

        int spriteCount = story.sprites?.Length ?? 0;

        currentCutIndex = Mathf.Clamp(
            currentCutIndex,
            0,
            Mathf.Max(0, spriteCount - 1)
        );

        for (int i = 0; i < cuts.Length; i++)
        {
            if (cuts[i] == null) continue;

            bool active = i < spriteCount;
            cuts[i].gameObject.SetActive(active);

            if (active)
            {
                cuts[i].sprite = story.sprites[i];
                cuts[i].enabled = i <= currentCutIndex;
            }
        }
    }

    private void RefreshDialogue(language selected)
    {
        if (!isPlaying) return;

        var dialogues = CurrentStory.dialogues;
        StoryData.LocalizedDialogue data = null;

        if (dialogues != null &&
            currentDialogueIndex < dialogues.Length &&
            dialogues[currentDialogueIndex] != null)
        {
            data = dialogues[currentDialogueIndex]
                .GetDialogue(selected);
        }

        if (dialogueText != null)
            dialogueText.text = data?.dialogue ?? "";

        if (speakerText != null)
            speakerText.text = data?.speaker ?? "";
    }

    public void RefreshLanguage()
    {
        RefreshDialogue(
            SceneLanguageMediator.GetLanguage(gameObject.scene)
        );
    }

    public void ApplyLanguage(
        language selected,
        LocalizedTextTable table)
    {
        RefreshDialogue(selected);
    }

    private void RefreshButtons()
    {
        foreach (var btn in prevButtons)
            btn.interactable = currentDialogueIndex > 0;

        foreach (var btn in nextButtons)
            btn.interactable = true;
    }

    private void MoveToReturnScene()
    {
        if (SceneMoveManager.instance != null)
            SceneMoveManager.instance.LoadScene(returnSceneName);
        else
            Debug.LogWarning("SceneMoveManager가 없습니다.");
    }

    public void ResetStory()
    {
        StorySave.ResetStoryProgress();
        StartStory(0);
    }
}