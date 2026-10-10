using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "GameData/Items/Effects/Status/Apply Status")]
public sealed class ApplyStatusEffect : ItemEffectData
{
    public StatusDefinition status;
    public StatusQueryTarget target = StatusQueryTarget.PlayerStatus;
    [Tooltip("Time는 Duration, UseCount는 Max Use Count를 부여합니다. AddRemaining은 재부여 시 남은 양에 더합니다.")]
    public BuffInfo statusInfo = new BuffInfo {
        reapplyMode = BuffReapplyMode.AddRemaining,
        useCountConsumeMode = BuffUseCountConsumeMode.AnyItemUsed
    };
    public bool showInUI = true;
    public Sprite statusIcon;

    protected override bool OwnsEndVisual => false;

    public override void Prepare(ItemEffectContext context)
    {
        if (context != null && statusInfo != null)
            context.GetSnapshotStat(this, statusInfo);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || context.buffManager == null ||
            status == null || statusInfo == null) return;
        BuffTargetHandle handle = StatusTargetUtility.Handle(StatusTargetUtility.Resolve(target, context));
        if (handle == null) return;
        BuffInfo info = context.GetCurrentStat(this, statusInfo);
        if (info == null) return;
        info.statusDefinition = status;
        info.Clamp();
        context.buffManager.RegisterStatus(this, info, context, handle);
    }
}
