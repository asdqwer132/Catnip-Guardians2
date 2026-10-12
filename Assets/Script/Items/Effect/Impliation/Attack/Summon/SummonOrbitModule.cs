using UnityEngine;

public enum SummonOrbitCenter { SpawnPosition, Owner }

[CreateAssetMenu(fileName = "SummonOrbit", menuName = "04.Items/Effects/Direct Effects/Summon/Summon Modules/Orbit")]
public sealed class SummonOrbitModule : SummonBehaviourModule
{
    public SummonOrbitCenter center = SummonOrbitCenter.Owner;
    [Header("기본 스탯 프로필 · 공전")]
    public SummonOrbitStatDefinition stats;
    public override SummonModuleStatDefinition StatDefinition => stats;
    [Tooltip("소환 지점의 현재 각도부터 공전합니다. 끄면 Initial Angle을 사용합니다.")]
    public bool startFromSpawnAngle = true;
    public override SummonBehaviourRuntime CreateRuntime(SummonActor summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonModuleRuntime<SummonOrbitStat>
    {
        private readonly SummonOrbitCenter mode;
        private readonly Vector3 spawn;
        private float angle;
        public Runtime(SummonActor summon, SummonOrbitModule data) : base(summon, data, data.stats)
        {
            mode = data.center;
            spawn = summon.transform.position;
            Vector3 offset = spawn - Center;
            angle = data.startFromSpawnAngle && offset.sqrMagnitude > 0.000001f ?
                Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg : stat.initialAngle;
        }
        private Vector3 Center => mode == SummonOrbitCenter.Owner && summon.Owner != null ? summon.Owner.transform.position : spawn;
        public override void Tick(float deltaTime)
        {
            angle = Mathf.Repeat(angle + stat.angularSpeed * deltaTime, 360f);
            Vector3 position = Center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * stat.radius;
            position.z = 0f;
            summon.transform.position = position;
        }
    }
}
