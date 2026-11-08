using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryData", menuName = "Story/Story Data")]
public class StoryData : ScriptableObject
{
    [Serializable]
    public class LocalizedDialogue
    {
        public language languageType;
        public string speaker;

        [TextArea(2, 5)]
        public string dialogue;
    }

    [Serializable]
    public class DialogueData
    {
        public bool nextImage;
        public LocalizedDialogue[] translations;

        public LocalizedDialogue GetDialogue(language selected)
        {
            if (translations == null) return null;

            foreach (var data in translations)
            {
                if (data.languageType == selected)
                    return data;
            }

            return null;
        }
    }

    [Header("Sprites")]
    public Sprite[] sprites;

    [Header("Dialogues")]
    public DialogueData[] dialogues;
}