using UnityEngine;

[CreateAssetMenu(fileName = "DebuffZoneEffect", menuName = "Game/Item Effect/Debuff Zone")]
public class DebuffZoneEffect : ItemEffectData
{
    public GameObject zonePrefab;
    public float duration = 5f;
    public float slowRate = 0.3f;
    public float damagePerSecond = 2f;

    public override void Execute(ItemEffectContext context)
    {
        if (zonePrefab == null)
            return;

        GameObject obj = Instantiate(
            zonePrefab,
            context.targetPosition,
            Quaternion.identity
        );

        DebuffZone zone = obj.GetComponent<DebuffZone>();

        if (zone != null)
        {
            zone.owner = context.owner;
            zone.duration = duration;
            zone.slowRate = slowRate;
            zone.damagePerSecond = damagePerSecond;
        }

        Debug.Log(context.itemData.itemName + " 디버프 장판 생성");
    }
}