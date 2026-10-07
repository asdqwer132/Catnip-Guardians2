using UnityEngine;

public enum SummonOrbitCenter { SpawnPosition, Owner }

[CreateAssetMenu(fileName = "SummonOrbit", menuName = "GameData/Items/Summon Modules/Orbit")]
public sealed class SummonOrbitModule : SummonBehaviourModule
{
    public SummonOrbitCenter center = SummonOrbitCenter.Owner;
    [Min(0f)] public float orbitRadius = 2f;
    public float angularSpeed = 90f;
    public float initialAngle;
    [Tooltip("소환 지점의 현재 각도부터 공전합니다. 끄면 Initial Angle을 사용합니다.")]
    public bool startFromSpawnAngle = true;
    public override SummonBehaviourRuntime CreateRuntime(SummonItemThrower summon) => new Runtime(summon, this);

    private sealed class Runtime : SummonBehaviourRuntime
    {
        private readonly SummonOrbitCenter mode;
        private readonly Vector3 spawn;
        private readonly float radius, speed;
        private float angle;
        public Runtime(SummonItemThrower summon, SummonOrbitModule data) : base(summon)
        {
            mode = data.center;
            spawn = summon.transform.position;
            radius = EffectStatUtility.Safe(data.orbitRadius, 0f, 100f, 2f);
            speed = EffectStatUtility.Safe(data.angularSpeed, -3600f, 3600f, 90f);
            Vector3 offset = spawn - Center;
            angle = data.startFromSpawnAngle && offset.sqrMagnitude > 0.000001f ?
                Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg : EffectStatUtility.Safe(data.initialAngle, -360f, 360f, 0f);
        }
        private Vector3 Center => mode == SummonOrbitCenter.Owner && summon.owner != null ? summon.owner.transform.position : spawn;
        public override void Tick(float deltaTime)
        {
            angle = Mathf.Repeat(angle + speed * deltaTime, 360f);
            Vector3 position = Center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * radius;
            position.z = 0f;
            summon.transform.position = position;
        }
    }
}
