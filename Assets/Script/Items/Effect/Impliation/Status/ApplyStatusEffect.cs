using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "GameData/Items/Effects/Status/Grant Status (Time or Uses)")]
public sealed class ApplyStatusEffect : ItemEffectData
{
    [FormerlySerializedAs("status")]
    [Tooltip("부여할 상태 키. 같은 키와 대상은 아이템·가방·효과 출처와 무관하게 누적됩니다.")]
    public StatusDefinition statusKey;
    public StatusQueryTarget target = StatusQueryTarget.PlayerStatus;
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
            statusKey == null || lifetimeSettings == null) return;
        BuffTargetHandle handle = StatusTargetUtility.Handle(StatusTargetUtility.Resolve(target, context));
        if (handle == null) return;
        BuffInfo info = context.GetCurrentStat(this, lifetimeSettings);
        if (info == null) return;
        info.statusDefinition = statusKey;
        info.Clamp();
        context.buffManager.RegisterStatus(this, info, context, handle);
    }
}
