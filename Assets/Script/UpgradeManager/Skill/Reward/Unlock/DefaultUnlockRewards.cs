using System;
using UnityEngine;

public sealed class DefaultUnlockRewards : ScriptableObject
{
    public UnlockSkillReward[] rewards = Array.Empty<UnlockSkillReward>();

    public static DefaultUnlockRewards Load() => Resources.Load<DefaultUnlockRewards>("DefaultUnlockRewards");
}
