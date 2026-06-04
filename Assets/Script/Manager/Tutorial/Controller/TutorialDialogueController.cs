using TMPro;
using UnityEngine;

public class TutorialDialogueController : MonoBehaviour
{
    [Header("Root")]
    public GameObject dialogueRoot;

    [Header("Text")]
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;

    [Header("Option")]
    public bool hideOnAwake = true;

    private void Awake()
    {
        if (hideOnAwake)
            Hide();
    }

    public void Show()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);
    }

    public void Hide()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    public void SetSpeakerName(string speakerName)
    {
        if (speakerNameText != null)
            speakerNameText.text = speakerName;
    }

    public void SetText(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
    }

    public void ShowText(string text)
    {
        Show();
        SetText(text);
    }

    public void Clear()
    {
        if (speakerNameText != null)
            speakerNameText.text = "";

        if (dialogueText != null)
            dialogueText.text = "";
    }
}