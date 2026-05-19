using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Node Root")]
    [SerializeField] private Transform nodeRoot;

    [Header("Node UIs")]
    [SerializeField] private SkillNodeUI[] skillNodeUIs;

    [Header("Option")]
    [SerializeField] private bool includeInactive = true;

    private void OnEnable()
    {
        if (SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnSkillTreeChanged += RefreshAllNodes;
        }
    }

    private void OnDisable()
    {
        if (SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnSkillTreeChanged -= RefreshAllNodes;
        }
    }

    public void Init()
    {
        FindNodeUIs();

        if (skillNodeUIs == null)
            return;

        for (int i = 0; i < skillNodeUIs.Length; i++)
        {
            SkillNodeUI nodeUI = skillNodeUIs[i];

            if (nodeUI == null)
                continue;

            nodeUI.Init(this);
        }

        RefreshAllNodes();
    }

    private void FindNodeUIs()
    {
        if (nodeRoot == null)
        {
            nodeRoot = transform;
        }

        skillNodeUIs = nodeRoot.GetComponentsInChildren<SkillNodeUI>(includeInactive);
    }

    public void RefreshAllNodes()
    {
        if (skillNodeUIs == null)
            return;

        for (int i = 0; i < skillNodeUIs.Length; i++)
        {
            SkillNodeUI nodeUI = skillNodeUIs[i];

            if (nodeUI == null)
                continue;

            nodeUI.Refresh();
        }
    }
}