using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DirectBuffTargetResolver", menuName = "GameData/Buffs/Targets/Direct")]
public class DirectBuffTargetResolver : BuffTargetResolver
{
    [Tooltip("DamageArea 등 IBuffTarget 컴포넌트 또는 해당 오브젝트의 Transform/Collider를 지정합니다. 생성되는 공격 전체는 Group을 사용하세요.")]
    public Component targetComponent;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results == null || targetComponent == null)
            return;

        IBuffTarget target = targetComponent as IBuffTarget ?? targetComponent.GetComponentInParent<IBuffTarget>();

        if (target != null)
            results.Add(BuffTargetHandle.Target(target));
    }
}
