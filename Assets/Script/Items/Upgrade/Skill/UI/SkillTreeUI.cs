using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Node Root")]
    [SerializeField] private Transform nodeRoot;

    [Header("Node UIs")]
    [SerializeField] private SkillNodeUI[] skillNodeUIs;

    [Header("Option")]
    [SerializeField] private bool includeInactive = true;
    private SkillTreeManager boundManager;

    private void OnEnable()
    {
        CurrencyManager.InstanceChanged += RefreshAllNodes;
        Subscribe(); RefreshAllNodes();
    }
    private void Subscribe()
    {
        if (boundManager == SkillTreeManager.Instance) return;
        if (boundManager != null) boundManager.OnSkillTreeChanged -= RefreshAllNodes;
        boundManager = SkillTreeManager.Instance;
        if (boundManager != null) boundManager.OnSkillTreeChanged += RefreshAllNodes;
    }

    private void OnDisable()
    {
        CurrencyManager.InstanceChanged -= RefreshAllNodes;
        if (boundManager != null) boundManager.OnSkillTreeChanged -= RefreshAllNodes;
        boundManager = null;
    }

    public void Init()
    {
        Subscribe();
        FindNodeUIs();

        if (skillNodeUIs == null)
            return;

        for (int i = 0; i < skillNodeUIs.Length; i++)
        {
            SkillNodeUI nodeUI = skillNodeUIs[i];

            if (nodeUI == null)
                continue;

            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.mergeBoxUpgrades?.RegisterNode(nodeUI.SkillNodeData);
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
