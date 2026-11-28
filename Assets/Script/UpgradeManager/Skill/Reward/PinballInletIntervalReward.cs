using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/Skills/Rewards/Pinball Inlet Interval")]
public sealed class PinballInletIntervalReward : SkillRewardData
{
    [Tooltip("Seconds added to each inlet interval, ordered from left to right.")]
    [SerializeField] private float[] intervalOffsets = new float[5];

    public void AddOffsets(float[] totals)
    {
        for (int i = 0; i < Math.Min(totals.Length, intervalOffsets.Length); i++)
            totals[i] += intervalOffsets[i];
    }

    // The item scene reads this reward from the round's purchased nodes.
    public override void Apply(SkillApplyContext context) { }
}
