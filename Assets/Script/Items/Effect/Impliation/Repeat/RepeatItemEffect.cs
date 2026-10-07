using System.Collections.Generic;
using UnityEngine;

public enum RepeatItemSelection { InOrder, Random }
public enum RepeatItemStartPosition { EffectPosition, OwnerPosition }

[CreateAssetMenu(fileName = "RepeatItemEffect", menuName = "GameData/Items/Effects/Repeat Item")]
public class RepeatItemEffect : ItemEffectData
{
    [Header("Repeat Steps")]
    [Tooltip("묶음마다 동시에 사용할 아이템과 다음 묶음까지의 간격을 설정합니다.")]
    public RepeatItemStep[] steps;
    [Tooltip("InOrder: 묶음을 순서대로 선택 / Random: 사용할 묶음을 무작위 선택")]
    public RepeatItemSelection selection;
    public RepeatItemStat repeatStat = new RepeatItemStat();
    [Header("Placement")]
    public AttackPlacementMode placement;
    public AttackDirectionMode directionMode;
    public Vector2 fixedWorldDirection = Vector2.right;
    public RepeatItemStartPosition startPosition;
    [Header("Throw")]
    [Tooltip("끄면 목표 위치에서 효과를 바로 실행합니다. 켜면 도착할 때 실행합니다.")]
    public bool throwItems = true;
    public ItemThrowMover projectilePrefab;
    public bool showTargetRange;
    public TargetRangeIndicator targetRangeIndicatorPrefab;
    [Tooltip("추가 공격 상태를 하위 아이템마다 발동할지 설정합니다. 기본은 최초 아이템만 발동합니다.")]
    public bool triggerSpecialItemsForChildren;

    // 기존 에셋과 호출 코드를 위한 필드. 에디터에서는 steps로 자동 변환한다.
    [HideInInspector] public ItemData[] items;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, repeatStat);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || repeatStat == null) return;
        RepeatItemStep[] runtimeSteps = CreateRuntimeSteps();
        if (runtimeSteps.Length == 0) return;
        GameObject host = new GameObject("RepeatItems");
        RepeatItemRunner runner = host.AddComponent<RepeatItemRunner>();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        runner.Init(this, context, runtimeSteps);
    }

    private void OnValidate()
    {
        ConvertLegacyItemsToSteps();
    }

    [ContextMenu("Convert Legacy Items To Steps")]
    public void ConvertLegacyItemsToSteps()
    {
        if (steps != null && steps.Length > 0)
        {
            items = null;
            return;
        }
        if (items == null || items.Length == 0)
            return;

        float interval = GetLegacyInterval();
        steps = new RepeatItemStep[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            steps[i] = new RepeatItemStep
            {
                items = new[] { items[i] },
                intervalAfter = interval
            };
        }
        items = null;
    }

    internal RepeatItemStep[] CreateRuntimeSteps()
    {
        List<RepeatItemStep> validSteps = new List<RepeatItemStep>();
        if (steps != null && steps.Length > 0)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                RepeatItemStep step = steps[i];
                if (step != null)
                    AddValidStep(validSteps, step.items, step.intervalAfter);
            }
        }
        else if (items != null)
        {
            // 변환되지 않은 기존 에셋도 플레이 중에는 정상적으로 사용할 수 있다.
            float interval = GetLegacyInterval();
            for (int i = 0; i < items.Length; i++)
                AddValidStep(validSteps, new[] { items[i] }, interval);
        }
        return validSteps.ToArray();
    }

    private float GetLegacyInterval()
    {
        return EffectStatUtility.Safe(repeatStat != null ? repeatStat.itemRepeatInterval : 0.2f,
            0f, 60f, 0.2f);
    }

    private static void AddValidStep(List<RepeatItemStep> output, ItemData[] sourceItems, float interval)
    {
        if (sourceItems == null) return;
        List<ItemData> validItems = new List<ItemData>(sourceItems.Length);
        for (int i = 0; i < sourceItems.Length; i++)
        {
            if (ItemEffectExecutor.CanExecuteItemEffect(sourceItems[i]))
                validItems.Add(sourceItems[i]);
        }
        if (validItems.Count == 0) return;
        output.Add(new RepeatItemStep
        {
            items = validItems.ToArray(),
            intervalAfter = EffectStatUtility.Safe(interval, 0f, 60f, 0.2f)
        });
    }
}
