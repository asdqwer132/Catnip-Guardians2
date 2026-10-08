using UnityEngine;

[CreateAssetMenu(fileName = "SummonItemThrow", menuName = "GameData/Items/Summon Modules/Item Throw")]
public sealed class SummonItemThrowModule : SummonBehaviourModule
{
    public ItemData item;
    public SummonThrowTargetMode targetMode;
    [Range(1, 128)] public int count = 1;
    public AttackPlacementMode spreadMode = AttackPlacementMode.FixedPoint;
    [Min(0f)] public float spreadRadius = 1f;
    [Range(0f, 360f)] public float spreadAngle = 30f;
    [Min(0.01f)] public float intervalMultiplier = 1f;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private readonly ItemData item;
        private readonly SummonThrowTargetMode mode;
        private readonly AttackPlacementMode spread;
        private readonly int count;
        private readonly float radius, angle, multiplier;
        private float timer;
        public Runtime(SummonItemThrower summon, SummonItemThrowModule data) : base(summon)
        {
            item = data.item;
            mode = data.targetMode;
            spread = data.spreadMode;
            count = Mathf.Clamp(data.count, 1, 128);
            radius = EffectStatUtility.Safe(data.spreadRadius, 0f, 100f, 1f);
            angle = EffectStatUtility.Safe(data.spreadAngle, 0f, 360f, 30f);
            multiplier = EffectStatUtility.Safe(data.intervalMultiplier, 0.01f, 100f, 1f);
        }
        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            if (timer < summon.AttackInterval * multiplier) return;
            timer = 0f;
            Vector3 target;
            if (!summon.TryTarget(mode, out target)) return;
            Vector3 forward = target - summon.transform.position;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.right;
            forward.Normalize();
            int profileVersion = summon.ProfileVersion;
            for (int i = 0; i < count && summon.CanAct; i++)
            {
                Vector3 position = spread == AttackPlacementMode.Shotgun ?
                    AttackPlacement.Position(spread, summon.transform.position, forward, i, count, 0f, 0f,
                        Vector3.Distance(target, summon.transform.position), angle) :
                    AttackPlacement.Position(spread, target, forward, i, count, 0f, 0f, radius, angle);
                summon.ThrowItem(summon.ResolveAttackItem(item), position);
                if (summon.ProfileVersion != profileVersion) break;
            }
        }
    }
}
