using System.Collections.Generic;
using UnityEngine;

public sealed class CombatEffectRecordingEffect : ItemEffectData
{
    public readonly List<ItemEffectContext> calls = new List<ItemEffectContext>();
    public override void ExecuteEffect(ItemEffectContext context) => calls.Add(context.Copy(context.targetPosition, context.direction));
}

