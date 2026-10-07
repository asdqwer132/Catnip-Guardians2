using System;
using UnityEngine;

[Serializable]
public sealed class RepeatItemEntry
{
    public ItemData item;
    [Range(1, 128)] public int count = 1;
}

public enum RepeatItemTravelMode { Inherit, Instant, Throw, Bounce }
public enum ItemImpactTrigger { OnArrival, AfterDelay, EnemyNearbyOrTimeout }

[Serializable]
public sealed class RepeatItemStep
{
    [Tooltip("각 아이템과 발사 개수. 한 스텝의 모든 발사체를 동시에 생성합니다.")]
    public RepeatItemEntry[] entries;
    [HideInInspector] public ItemData[] items;

    [Tooltip("아이템과 함께 착지마다 실행할 효과. 아이템 없이 이 효과만 사용하는 스텝도 가능합니다.")]
    public ItemEffectData[] onImpactEffects;
    [Range(1, 128)] public int effectOnlyCount = 1;

    public bool overridePlacement;
    public AttackPlacementMode placement;
    public RepeatItemTravelMode travelMode;
    public ItemImpactTrigger impactTrigger;

    public void ConvertLegacyItems()
    {
        if (entries != null && entries.Length > 0) return;
        if (items == null || items.Length == 0) return;
        entries = new RepeatItemEntry[items.Length];
        for (int i = 0; i < items.Length; i++)
            entries[i] = new RepeatItemEntry { item = items[i], count = 1 };
        items = null;
    }

    [Min(0f)]
    [Tooltip("이 묶음을 사용한 뒤 다음 묶음까지 기다리는 시간입니다. 마지막 사용 뒤에는 기다리지 않습니다.")]
    public float intervalAfter = 0.2f;
}
