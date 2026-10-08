using UnityEngine;

[CreateAssetMenu(fileName = "ExecuteEffectOnHitData", menuName = "GameData/Items/Hit Effects/Execute Effects")]
public sealed class ExecuteEffectOnHitData : HitEffectData
{
    public ItemEffectData[] effects;
    public HitEffectApplyMode activationScope = HitEffectApplyMode.FirstHitPerAttack;
    [Tooltip("원래 공격 방향에 더하는 각도. 수직 분열은 90도와 -90도로 두 에셋을 연결합니다.")]
    public float directionAngleOffset;
    public Vector2 localPositionOffset;
    protected override bool RequiresLivingTarget => false;

    protected override bool CanApply(HitEffectContext context)
        => context.attackState.TryClaim(this, activationScope, context);

    protected override bool ApplyEffect(HitEffectContext context)
    {
        if (effects == null || effects.Length == 0) return false;
        ItemEffectContext child = context.CreateItemContext();
        Vector3 direction = context.attackDirection;
        direction.z = 0f;
        if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
        direction = Quaternion.Euler(0f, 0f, directionAngleOffset) * direction.normalized;
        child.direction = direction;
        child.targetPosition = context.hitPosition + direction * localPositionOffset.y +
            new Vector3(direction.y, -direction.x, 0f) * localPositionOffset.x;
        bool executed = false;
        foreach (ItemEffectData effect in effects)
        {
            if (!child.CanContinue) break;
            if (effect == null) continue;
            effect.Execute(child);
            executed = true;
        }
        return executed;
    }
}
