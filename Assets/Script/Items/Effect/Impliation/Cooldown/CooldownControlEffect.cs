using System.Collections.Generic;
using UnityEngine;

public enum CooldownOperation { ReduceSeconds, ReduceFraction, Ready }
public enum CooldownBagSelection { SourceBag, AllBags }

[CreateAssetMenu(fileName = "CooldownControlEffect", menuName = "GameData/Items/Effects/Cooldown Control")]
public sealed class CooldownControlEffect : ItemEffectData
{
    public CooldownBagSelection bags;
    public CooldownOperation operation;
    [Min(0f)] public float amount = 1f;
    [Tooltip("가방 공통 쿨다운도 조작합니다. 특정 뼈/깃털 슬롯만 조작하려면 끕니다.")]
    public bool affectBagCooldown;
    public ItemData[] items;
    public ItemTagDefinition tag;
    public bool filterSeries;
    public ItemSeries series;
    [Tooltip("사용 시 확정한 지연 후 실행합니다. 전투 초기화 시 취소됩니다.")]
    [Min(0f)] public float delay;

    public bool Matches(ItemData item)
    {
        if (item == null) return false;
        if (filterSeries && item.series != series) return false;
        if (tag != null && (item.tags == null || System.Array.IndexOf(item.tags, tag) < 0)) return false;
        return items == null || items.Length == 0 || System.Array.IndexOf(items, item) >= 0;
    }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null) return;
        CooldownControlEffect settings = Instantiate(this);
        settings.delay = 0f;
        settings.items = items != null ? (ItemData[])items.Clone() : null;
        if (delay <= 0f) { settings.Apply(context); Destroy(settings); return; }
        GameObject host = new GameObject("DelayedCooldownControl");
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        host.AddComponent<CooldownControlRunner>().Init(settings, context.Copy(context.targetPosition, context.direction),
            EffectStatUtility.Safe(delay, 0f, 3600f, 0f));
    }
    internal void Apply(ItemEffectContext context)
    {
        // 피해 이벤트 중 활성 목록이 바뀌어도 동일 실행에서 반복 적용하지 않는다.
        BagItemUseManager[] managers = BagItemUseManager.ActiveManagers.ToArray();
        foreach (BagItemUseManager manager in managers)
            if (manager != null && (bags == CooldownBagSelection.AllBags ||
                (context.sourceBag != null && manager.bag == context.sourceBag))) manager.ApplyCooldownControl(this);
    }
}
