using UnityEngine;
using UnityEngine.UI;

public class SkillTreeUIInjecter : MonoBehaviour
{
    [Header("Triggers")]
    public Transform[] NodesParents;

    [Header("Setting")]
    public ItemTooltipUI tooltipUI;
    public Toggle infoToggel;
    public Toggle invenToggle;

    [Header("Option")]
    [Tooltip("비활성화된 노드도 포함해서 찾습니다.")]
    public bool includeInactive = true;

    [Tooltip("Awake에서 자동으로 주입합니다.")]
    public bool injectOnAwake = true;

    private void Awake()
    {
        if (injectOnAwake)
            Inject();
    }

    [ContextMenu("Inject Toggle Tooltip Triggers")]
    public void Inject()
    {
        if (NodesParents == null || NodesParents.Length == 0)
        {
            Debug.LogWarning("NodesParents가 설정되지 않았습니다.", this);
            return;
        }

        int injectedCount = 0;

        for (int i = 0; i < NodesParents.Length; i++)
        {
            Transform nodesParent = NodesParents[i];

            if (nodesParent == null)
                continue;

            ToggleTooltipTrigger[] triggers =
                nodesParent.GetComponentsInChildren<ToggleTooltipTrigger>(
                    includeInactive
                );

            for (int j = 0; j < triggers.Length; j++)
            {
                ToggleTooltipTrigger trigger = triggers[j];

                if (trigger == null)
                    continue;

                trigger.tooltipUI = tooltipUI;
                trigger.infoToggle = infoToggel;
                trigger.invenToggle = invenToggle;

                injectedCount++;
            }
        }

        Debug.Log(
            $"ToggleTooltipTrigger 주입 완료: {injectedCount}개",
            this
        );
    }
}