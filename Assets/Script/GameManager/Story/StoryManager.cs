using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour, ISceneLocalizedUI
{
    [Header("Stories")]
    [SerializeField] private StoryData[] stories;

    [Header("Visual Novel")]
    [SerializeField] private Image background;
    [SerializeField] private Image leftCharacter;
    [SerializeField] private Image rightCharacter;
    [SerializeField] private Animator backgroundAnimator;
    [SerializeField] private Animator leftAnimator;
    [SerializeField] private Animator rightAnimator;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text pageText;
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
    private bool isPlaying;
    private StoryData requestedStory;
    private bool independentStory;
    private string requestedReturnScene;

    private StoryData CurrentStory => requestedStory != null ? requestedStory : stories[currentStoryIndex];

    private void Awake()
    {
        foreach (var btn in (prevButtons ?? System.Array.Empty<Button>()))
            if (btn != null)
                btn.onClick.AddListener(PreviousDialogue);

        foreach (var btn in (nextButtons ?? System.Array.Empty<Button>()))
            if (btn != null)
                btn.onClick.AddListener(NextDialogue);

        foreach (var btn in (skipButtons ?? System.Array.Empty<Button>()))
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

        if (StoryProgressManager.Instance != null && StoryProgressManager.Instance.PlayingStory != null)
        {
            requestedStory = StoryProgressManager.Instance.PlayingStory;
            independentStory = true;
            currentDialogueIndex = 0;
            isPlaying = true;
            RefreshView();
            return;
        }

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

        requestedStory = null;
        independentStory = false;
        requestedReturnScene = null;
        currentStoryIndex = index;
        currentDialogueIndex = 0;
        isPlaying = true;

        RefreshView();
    }

    public void NextDialogue()
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

    public void PreviousDialogue()
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

        if (independentStory)
            requestedReturnScene = StoryProgressManager.Instance != null
                ? StoryProgressManager.Instance.CompletePlayingStory() : returnSceneName;
        else
            StorySave.SetStoryChapterProgress(Mathf.Max(StorySave.GetStoryChapterProgress(), currentStoryIndex + 1));

        MoveToReturnScene();
    }

    private void RefreshView()
    {
        RefreshVisuals();
        RefreshLanguage();
        RefreshButtons();
    }

    private void RefreshVisuals()
    {
        var dialogues = CurrentStory.dialogues;
        var line = dialogues != null && currentDialogueIndex < dialogues.Length ? dialogues[currentDialogueIndex] : null;
        ShowLayer(background, backgroundAnimator, line?.background, line?.backgroundTrigger);
        ShowLayer(leftCharacter, leftAnimator, line?.leftCharacter, line?.leftTrigger);
        ShowLayer(rightCharacter, rightAnimator, line?.rightCharacter, line?.rightTrigger);
    }

    private static void ShowLayer(Image image, Animator animator, Sprite sprite, string trigger)
    {
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        if (sprite == null || string.IsNullOrWhiteSpace(trigger) || animator == null ||
            !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger || parameter.name != trigger) continue;
            animator.SetTrigger(parameter.nameHash);
            return;
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
        if (pageText != null)
            pageText.text = (currentDialogueIndex + 1) +" / " + (dialogues?.Length ?? 0);

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
        foreach (var btn in (prevButtons ?? System.Array.Empty<Button>()))
            if (btn != null) btn.interactable = currentDialogueIndex > 0;

        foreach (var btn in (nextButtons ?? System.Array.Empty<Button>()))
            if (btn != null) btn.interactable = true;
    }

    private void MoveToReturnScene()
    {
        if (SceneMoveManager.instance != null)
            SceneMoveManager.instance.LoadScene(string.IsNullOrWhiteSpace(requestedReturnScene) ? returnSceneName : requestedReturnScene);
        else
            Debug.LogWarning("SceneMoveManager가 없습니다.");
    }

    public void ResetStory()
    {
        StorySave.ResetStoryProgress();
        StartStory(0);
    }
}