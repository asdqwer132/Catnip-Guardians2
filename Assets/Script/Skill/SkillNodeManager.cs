using UnityEngine;

public class SkillNodeManager : MonoBehaviour
{
    public SkillNode[] nodes;

    void Start()
    {
        UpdateAllUI();
    }

    public void UpdateAllUI()
    {
        foreach (SkillNode node in nodes)
        {
            if (node != null)
            {
                node.UpdateUI();
            }
        }
    }
}