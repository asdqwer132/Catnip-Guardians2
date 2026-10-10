using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GrantSpecialStatusEffect", menuName = "GameData/Items/Effects/Special Status/Grant Special Status (Time or Uses)")]
public sealed class GrantSpecialStatusEffect : ItemEffectData
{
    [FormerlySerializedAs("status")]
    [FormerlySerializedAs("statusKey")]
    [Tooltip("부여할 상태 키. 같은 키와 대상은 아이템·가방·효과 출처와 무관하게 누적됩니다.")]
    public SpecialStatusDefinition specialStatusKey;
    [Tooltip("이 상태 인스턴스의 해로움·정화 가능 여부·정화 태그입니다. 비워두면 이로운 상태이며 정화 가능합니다. 상태 식별이나 누적 키를 바꾸지 않습니다.")]
    public BuffClassificationDefinition buffClassification;
    public SpecialStatusQueryTarget target = SpecialStatusQueryTarget.PlayerStatus;
    [Tooltip("Time는 Duration, UseCount는 Max Use Count를 부여합니다. AddRemaining은 재부여 시 남은 양에 더합니다.")]
    [FormerlySerializedAs("statusInfo")]
    public BuffInfo lifetimeSettings = new BuffInfo {
        reapplyMode = BuffReapplyMode.AddRemaining,
        useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed
    };
    public bool showInUI = true;
    public Sprite statusIcon;

    protected override bool OwnsEndVisual => false;

    public override void Prepare(ItemEffectContext context)
    {
        if (context != null && lifetimeSettings != null)
            context.GetSnapshotStat(this, lifetimeSettings);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || context.buffManager == null ||
            specialStatusKey == null || lifetimeSettings == null) return;
        BuffTargetHandle handle = SpecialStatusTargetUtility.Handle(SpecialStatusTargetUtility.Resolve(target, context));
        if (handle == null) return;
        BuffInfo info = context.GetCurrentStat(this, lifetimeSettings);
        if (info == null) return;
        info.Clamp();
        context.buffManager.RegisterSpecialStatus(this, info, context, handle);
    }
}
