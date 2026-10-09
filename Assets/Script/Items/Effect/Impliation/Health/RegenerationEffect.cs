using System;
using System.Collections.Generic;
using UnityEngine;

public enum RegenerationFirstTick { AfterInterval, Immediate }

[Serializable]
public class RegenerationStat : IGameStat<RegenerationStat>, IEffectScalableStat
{
    [Min(0f)] public float regenerationAmount = 1f;
    [Min(0.01f)] public float regenerationInterval = 1f;
    [Min(0.01f)] public float regenerationDuration = 5f;
    public RegenerationStat Clone() => (RegenerationStat)MemberwiseClone();
    public void Clamp()
    {
        regenerationAmount = EffectStatUtility.Safe(regenerationAmount, 0f, 100000000f, 1f);
        regenerationInterval = EffectStatUtility.Safe(regenerationInterval, 0.01f, 600f, 1f);
        regenerationDuration = EffectStatUtility.Safe(regenerationDuration, 0f, 600f, 5f);
    }
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        regenerationAmount *= scale.Healing;
        regenerationDuration *= scale.Duration;
    }
}

[CreateAssetMenu(fileName = "Regeneration", menuName = "GameData/Items/Effects/Health/Regeneration")]
public class RegenerationEffect : ItemEffectData
{
    public HealthTargetSettings targets = new HealthTargetSettings();
    public HealthAmountMode amountMode;
    public RegenerationFirstTick firstTick = RegenerationFirstTick.AfterInterval;
    public RegenerationStat regenerationStat = new RegenerationStat();
    [Header("Buff UI")]
    public EffectBuffUISettings buffUI = new EffectBuffUISettings();
    public override void Prepare(ItemEffectContext context) { context.GetSnapshotStat(this, regenerationStat); }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || targets == null) return;
        List<Health> resolved = new List<Health>();
        targets.Resolve(context, resolved);
        for (int i = 0; i < resolved.Count && context.CanContinue; i++)
        {
            HealthRegenerationRunner runner = resolved[i].GetComponent<HealthRegenerationRunner>();
            if (runner == null) runner = resolved[i].gameObject.AddComponent<HealthRegenerationRunner>();
            runner.Add(this, context);
        }
    }
}
