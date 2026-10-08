using UnityEngine;

[CreateAssetMenu(fileName = "TimeStopEffect", menuName = "GameData/Items/Effects/Time Stop")]
public sealed class TimeStopEffect : ItemEffectData
{
    [Min(0.01f)] public float duration = 5f;
    [Tooltip("식물 성장, 아군 쿨다운, 플레이어 시간은 변경하지 않습니다.")]
    public TimeStopTargets targets = TimeStopTargets.EnemyActions | TimeStopTargets.EnemyProjectiles | TimeStopTargets.EnemySpawning;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue || targets == TimeStopTargets.None ||
            float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) return;
        float scaledDuration = duration * context.durationMultiplier;
        if (scaledDuration <= 0f || float.IsNaN(scaledDuration) || float.IsInfinity(scaledDuration)) return;
        GameObject host = new GameObject("TimeStopRuntime");
        host.AddComponent<TimeStopRunner>().Init(context, targets, scaledDuration);
    }
}
