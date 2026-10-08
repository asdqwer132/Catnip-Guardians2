using UnityEngine;

[CreateAssetMenu(fileName = "SectorDamageAreaAttackEffect", menuName = "GameData/Items/Effects/Attack/Sector Damage Area")]
public sealed class SectorDamageAreaAttackEffect : DamageAreaAttackEffect
{
    [Range(0f, 360f)] public float sectorAngle = 90f;
    public float directionAngleOffset;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || attackStat == null) return;
        Quaternion rotation;
        TryGetImpactRotation(context, out rotation);
        SectorDamageArea area;
        if (attackPrefab != null)
        {
            SectorDamageArea prefab = attackPrefab as SectorDamageArea;
            if (prefab == null)
            { Debug.LogWarning("SectorDamageAreaAttackEffect: SectorDamageArea 프리팹을 연결하세요.", this); return; }
            area = Instantiate(prefab, context.targetPosition, rotation);
        }
        else
        {
            GameObject host = new GameObject("Sector Damage Area");
            host.transform.SetPositionAndRotation(context.targetPosition, rotation);
            host.AddComponent<CircleCollider2D>();
            Rigidbody2D body = host.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            area = host.AddComponent<SectorDamageArea>();
        }
        area.sectorAngle = EffectStatUtility.Safe(sectorAngle, 0f, 360f, 90f);
        InitDamageArea(area, context);
    }

    protected override bool TryGetImpactRotation(ItemEffectContext context, out Quaternion rotation)
    {
        Vector3 direction;
        if (context == null || !context.TryGetDirection(out direction)) direction = Vector3.right;
        rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + directionAngleOffset);
        return true;
    }
}
