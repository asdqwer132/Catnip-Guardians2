using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CleanseEffect", menuName = "GameData/Items/Effects/Status/Cleanse")]
public sealed class CleanseEffect : ItemEffectData
{
    public BuffTargetResolver targetResolver;
    public BuffCleanseFilter filter = new BuffCleanseFilter();
    [Tooltip("켜면 조건에 맞는 수치 변경 버프도 제거합니다. 끄면 Modifier가 없는 버프만 제거합니다.")]
    public bool includeModifierBuffs = true;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || context.buffManager == null || targetResolver == null) return;
        List<BuffTargetHandle> targets = new List<BuffTargetHandle>();
        targetResolver.ResolveTargets(new BuffRegisterContext(context, context.buffManager), targets);
        foreach (BuffTargetHandle target in targets) context.buffManager.Cleanse(target, filter, includeModifierBuffs);
    }
}
