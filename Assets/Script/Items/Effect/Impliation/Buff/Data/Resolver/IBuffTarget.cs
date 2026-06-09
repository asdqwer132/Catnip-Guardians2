using UnityEngine;

public interface IBuffTarget
{
    Object BuffTargetObject { get; }
    string BuffTargetGroup { get; }
    string BuffTargetDebugName { get; }
    void RefreshBuffedStat();
}
