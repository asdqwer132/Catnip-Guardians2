using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllTargetsBuffTarget", menuName = "04.Items/Effects/Direct Effects/Buff/BuffSetting/Targets/All Targets")]
public sealed class AllTargetsBuffTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        // 대상들을 하나씩 복사하지 않고 전체 범위 한 개를 등록합니다.
        // 나중에 생성되는 아이템/가방/IBuffTarget도 같은 범위에 포함됩니다.
        if (results != null) results.Add(BuffTargetHandle.AllTargets());
    }
}
