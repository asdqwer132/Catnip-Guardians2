using UnityEngine;

[CreateAssetMenu(
    fileName = "DirectionalRectangleAttackEffect",
    menuName = "GameData/Item/Item Effect/DirectionalRectangleAttackEffect"
)]
public class DirectionalRectangleAttackEffect : DamageAreaAttackEffect
{
    [Header("Rectangle Size")]
    [Tooltip("공격 방향의 좌우 폭. 월드 단위.")]
    [Min(0.01f)]
    public float attackWidth = 1f;

    [Tooltip("도착 지점부터 공격 방향 앞으로 뻗는 길이. 월드 단위.")]
    [Min(0.01f)]
    public float attackHeight = 4f;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || context.sourceItemData == null)
            return;

        if (attackStat == null || attackPrefab == null)
            return;

        DirectionalRectangleDamageArea rectanglePrefab =
            attackPrefab as DirectionalRectangleDamageArea;

        if (rectanglePrefab == null)
        {
            Debug.LogWarning(
                "DirectionalRectangleAttackEffect: Attack Prefab에 " +
                "DirectionalRectangleDamageArea 프리팹을 연결하세요.",
                this
            );
            return;
        }

        Vector3 spawnPosition = context.targetPosition;
        spawnPosition.z = 0f;

        Quaternion rotation;
        if (!TryGetAttackRotation(context, out rotation))
        {
            Debug.LogWarning(
                "DirectionalRectangleAttackEffect: 투척 방향을 얻을 수 없습니다. " +
                "ExecuteItemEffect()에 유효한 direction 또는 " +
                "던진 순간의 usePosition과 실제 도착 위치 targetPosition을 전달하세요.",
                this
            );
            return;
        }

        DirectionalRectangleDamageArea damageArea = Instantiate(
            rectanglePrefab,
            spawnPosition,
            rotation
        );

        damageArea.SetRectangleSize(attackWidth, attackHeight);

        // 기존 SnapshotOnly + 동적 버프 초기화와 타격 모드 설정을 재사용한다.
        InitDamageArea(damageArea, context);

        // 초기화가 끝난 뒤에도 공격 영역에 계산한 위치와 회전을 적용한다.
        damageArea.transform.SetPositionAndRotation(spawnPosition, rotation);
    }

    protected override bool TryGetImpactRotation(ItemEffectContext context, out Quaternion rotation)
    {
        // 생성·수명·사운드는 공통 연출 코드가 담당하고, 공격의 방향만 전달한다.
        return TryGetAttackRotation(context, out rotation);
    }

    private bool TryGetAttackRotation(ItemEffectContext context, out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (context == null)
            return false;

        Vector3 attackDirection;
        if (!context.TryGetDirection(out attackDirection))
            return false;

        // 프리팹의 로컬 +Y가 공격 방향을 바라보도록 회전시킨다.
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg - 90f;
        rotation = Quaternion.Euler(0f, 0f, angle);
        return true;
    }
}
