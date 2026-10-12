using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SummonItemThrow", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Item Throw")]
public sealed class SummonItemThrowModule : SummonBehaviourModule
{
    public ItemData item;
    [Tooltip("켜면 던질 아이템의 기본 피해 대신 이 투척 스탯의 Damage를 사용합니다.")]
    [FormerlySerializedAs("useActorAttackPower")]
    public bool useModuleDamage;
    public SummonThrowTargetMode targetMode;
    [Header("기본 스탯 프로필 · 투척")]
    public SummonItemThrowStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    public AttackPlacementMode spreadMode = AttackPlacementMode.FixedPoint;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonModuleRuntime<SummonItemThrowStat>
    {
        private readonly ItemData item;
        private readonly SummonThrowTargetMode mode;
        private readonly AttackPlacementMode spread;
        private readonly bool useModuleDamage;
        private float timer;
        public Runtime(SummonActor summon, SummonItemThrowModule data) : base(summon, data, data.stats)
        {
            item = data.item;
            useModuleDamage = data.useModuleDamage;
            mode = data.targetMode;
            spread = data.spreadMode;
        }
        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            if (timer < stat.interval) return;
            timer = 0f;
            Vector3 target;
            if (summon.ResolveAttackItem(item) == null) return;
            if (!summon.TryTarget(mode, stat.range, out target)) return;
            Vector3 forward = target - summon.transform.position;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.right;
            forward.Normalize();
            int profileVersion = summon.ProfileVersion;
            int life = summon.LifeId;
            SummonItemThrowStat shot = CaptureActivationStat();
            if (!summon.CanAct || summon.LifeId != life || summon.ProfileVersion != profileVersion) return;
            int count = Mathf.Clamp(Mathf.FloorToInt(shot.shotCount), 1, 128);
            for (int i = 0; i < count && summon.CanAct; i++)
            {
                Vector3 position = spread == AttackPlacementMode.Shotgun ?
                    AttackPlacement.Position(spread, summon.transform.position, forward, i, count, 0f, 0f,
                        Vector3.Distance(target, summon.transform.position), shot.spreadAngle) :
                    AttackPlacement.Position(spread, target, forward, i, count, shot.forwardSpacing, shot.sideSpacing, shot.spreadRadius, shot.spreadAngle);
                summon.ThrowItem(summon.ResolveAttackItem(item), position, useModuleDamage ? (float?)shot.damage : null, shot.damageMultiplier, shot.healingMultiplier);
                if (summon.LifeId != life || summon.ProfileVersion != profileVersion) break;
            }
        }
    }
}
