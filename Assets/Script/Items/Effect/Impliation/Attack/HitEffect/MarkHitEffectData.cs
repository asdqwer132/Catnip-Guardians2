using UnityEngine;
using UnityEngine.Serialization;

public enum MarkReactionSource { MarkCreator, DamageSource }

[CreateAssetMenu(fileName = "MarkHitEffect", menuName = "GameData/Items/Hit Effects/Configurable Mark")]
public sealed class MarkHitEffectData : HitEffectData
{
    public SpecialStatusDefinition markKey;
    public BuffClassificationDefinition buffClassification;
    [FormerlySerializedAs("statusInfo")]
    public BuffInfo markerLifetime = new BuffInfo { duration = 5f };
    [Tooltip("반응 효과를 연결하지 않으면 표식 자체는 피해나 보상을 추가하지 않습니다.")]
    public ItemEffectData[] onDamagedEffects;
    public ItemEffectData[] onKilledEffects;
    public ItemEffectData[] onNaturalExpiryEffects;
    public bool reactToLethalDamage = true;
    public bool damageReactionOnlyOnce;
    public MarkReactionSource reactionSource;
    protected override bool OwnsEndVisual => false;

    protected override bool ApplyEffect(HitEffectContext context)
    {
        MarkStatusController controller = context.target.GetComponent<MarkStatusController>();
        if (controller == null) controller = context.target.gameObject.AddComponent<MarkStatusController>();
        return controller.Apply(this, context);
    }
}
