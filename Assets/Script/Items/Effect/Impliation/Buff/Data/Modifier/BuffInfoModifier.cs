
using UnityEngine;

[CreateAssetMenu(fileName = "Buff Info Modifier", menuName = "Game/Buff/Modifier/Buff Info")]
public class BuffInfoModifier : BuffModifier
{
    public float duration;
    public float durationM;
    public int addMaxUseCount;

    public override bool CanApplyTo(object stat, BuffQueryContext query)
    {
        return stat is BuffInfo;
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext query)
    {
        BuffInfo target = stat as BuffInfo;
        if (target == null)
            return;

        for (int i = 0; i < Mathf.Max(1, stack); i++)
        {
            target.duration += duration;
            target.duration *= 1f + durationM;
            target.maxUseCount += addMaxUseCount;
        }

        target.Clamp();
    }
}
