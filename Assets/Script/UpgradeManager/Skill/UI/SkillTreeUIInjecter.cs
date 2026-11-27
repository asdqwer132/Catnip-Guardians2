using UnityEngine;

public class SkillTreeUIInjecter : MonoBehaviour
{
    [Header("Triggers")]
    public Transform[] NodesParents;

    [Header("Setting")]
    public TooltipUI tooltipUI;

    [Header("Option")]
    [Tooltip("비활성화된 노드도 포함해서 찾습니다.")]
    public bool includeInactive = true;

    [Tooltip("Awake에서 자동으로 주입합니다.")]
    public bool injectOnAwake = true;

    [Tooltip("Keep the last hovered skill information until a skill is unlocked or the map changes.")]
    public bool keepInfoOnPointerExit = true;

    private void Awake()
    {
        if (injectOnAwake)
            Inject();
    }

    [ContextMenu("Inject Item Tooltip Triggers")]
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

            ItemTooltipTrigger[] triggers =
                nodesParent.GetComponentsInChildren<ItemTooltipTrigger>(
                    includeInactive
                );

            for (int j = 0; j < triggers.Length; j++)
            {
                ItemTooltipTrigger trigger = triggers[j];

                if (trigger == null)
                    continue;

                trigger.tooltipUI = tooltipUI;
                trigger.hideOnPointerExit = !keepInfoOnPointerExit;

                injectedCount++;
            }
        }

        //Debug.Log(
        //    $"ItemTooltipTrigger 주입 완료: {injectedCount}개",
        //    this
        //);
    }
}