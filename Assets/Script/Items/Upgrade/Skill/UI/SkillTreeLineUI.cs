using UnityEngine;

public sealed class SkillTreeLineUI : MonoBehaviour
{
    public GameObject unlockedLine;
    public GameObject lockedLine;

    public void SetUnlocked(bool unlocked)
    {
        if (unlockedLine != null) unlockedLine.SetActive(unlocked);
        if (lockedLine != null) lockedLine.SetActive(!unlocked);
    }
}
