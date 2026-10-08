using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShieldReapplyMode { Refresh, Add }

[Serializable]
public class ShieldStat : IGameStat<ShieldStat>, IEffectScalableStat
{
    [Min(0f)] public float shieldAmount = 15f;
    [Min(0f)] public float shieldDuration = 10f;
    public ShieldStat Clone() => (ShieldStat)MemberwiseClone();
    public void Clamp()
    {
        shieldAmount = EffectStatUtility.Safe(shieldAmount, 0f, 100000000f, 15f);
        shieldDuration = EffectStatUtility.Safe(shieldDuration, 0f, 600f, 10f);
    }
    public void ApplyExecutionScale(EffectExecutionScale scale) { shieldDuration *= scale.Duration; }
}

[CreateAssetMenu(fileName = "Shield", menuName = "GameData/Items/Effects/Health/Shield")]
public class ShieldEffect : ItemEffectData
{
    public HealthTargetSettings targets = new HealthTargetSettings();
    public ShieldStat shieldStat = new ShieldStat();
    public ShieldReapplyMode reapplyMode;
    protected override bool OwnsEndVisual => false;
    public override void Prepare(ItemEffectContext context) { context.GetSnapshotStat(this, shieldStat); }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || targets == null) return;
        ShieldStat current = context.GetCurrentStat(this, shieldStat);
        if (current == null) return;
        List<Health> resolved = new List<Health>();
        targets.Resolve(context, resolved);
        for (int i = 0; i < resolved.Count && context.CanContinue; i++)
            resolved[i].AddShield(current.shieldAmount, current.shieldDuration, reapplyMode, this, context, endVisualData);
    }
}
