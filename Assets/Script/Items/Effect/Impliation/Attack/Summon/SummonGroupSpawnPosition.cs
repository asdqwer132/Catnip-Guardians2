using System;
using UnityEngine;

public enum SummonGroupSpawnPositionMode { LastSummonPosition, PlayerPosition, OwnerPosition, FixedWorldPosition, RandomPosition, RegisteredAnchor }
public enum SummonGroupRandomCenter { LastSummonPosition, PlayerPosition, OwnerPosition, FixedWorldPosition, RegisteredAnchor }

[Serializable]
public sealed class SummonGroupSpawnPosition
{
    public SummonGroupSpawnPositionMode mode;
    public Vector3 fixedWorldPosition;
    public Vector3 offset;
    [Header("랜덤 위치")]
    public SummonGroupRandomCenter randomCenter;
    [Min(0f)] public float randomRadius = 5f;
    [Tooltip("Registered Anchor에 사용할 이름입니다. Player Position은 Player 키를 사용합니다.")]
    public string anchorKey = "Player";

    public bool TryResolve(SummonGroupReactionContext context, out Vector3 position)
    {
        position = Vector3.zero;
        if (context == null || context.source == null) return false;
        SummonGroupRandomCenter center = mode == SummonGroupSpawnPositionMode.RandomPosition ? randomCenter :
            mode == SummonGroupSpawnPositionMode.PlayerPosition ? SummonGroupRandomCenter.PlayerPosition :
            mode == SummonGroupSpawnPositionMode.OwnerPosition ? SummonGroupRandomCenter.OwnerPosition :
            mode == SummonGroupSpawnPositionMode.FixedWorldPosition ? SummonGroupRandomCenter.FixedWorldPosition :
            mode == SummonGroupSpawnPositionMode.RegisteredAnchor ? SummonGroupRandomCenter.RegisteredAnchor :
            SummonGroupRandomCenter.LastSummonPosition;
        switch (center)
        {
            case SummonGroupRandomCenter.PlayerPosition:
            case SummonGroupRandomCenter.RegisteredAnchor:
                if (!EffectPositionAnchor.TryGetPosition(center == SummonGroupRandomCenter.PlayerPosition ? "Player" : anchorKey,
                    context.source.owner, out position)) return false;
                break;
            case SummonGroupRandomCenter.OwnerPosition:
                if (context.source.owner == null) return false;
                position = context.source.owner.transform.position;
                break;
            case SummonGroupRandomCenter.FixedWorldPosition: position = fixedWorldPosition; break;
            default: position = context.lastSummonPosition; break;
        }
        position += offset;
        if (mode == SummonGroupSpawnPositionMode.RandomPosition)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(UnityEngine.Random.value) * EffectStatUtility.Safe(randomRadius, 0f, 1000f, 5f);
            position += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        }
        position.z = 0f;
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsInfinity(position.x) && !float.IsInfinity(position.y);
    }
}
