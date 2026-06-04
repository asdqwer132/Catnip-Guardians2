using UnityEngine;

public class StoryChapterPanel : MonoBehaviour
{
    [Header("Cuts In Order")]
    public GameObject[] cutObjects;

    public int CutCount
    {
        get
        {
            if (cutObjects == null)
                return 0;

            return cutObjects.Length;
        }
    }

    public void ShowCutProgress(int cutIndex)
    {
        if (cutObjects == null)
            return;

        for (int i = 0; i < cutObjects.Length; i++)
        {
            if (cutObjects[i] == null)
                continue;

            cutObjects[i].SetActive(i <= cutIndex);
        }
    }

    public void HideAllCuts()
    {
        if (cutObjects == null)
            return;

        for (int i = 0; i < cutObjects.Length; i++)
        {
            if (cutObjects[i] == null)
                continue;

            cutObjects[i].SetActive(false);
        }
    }
}