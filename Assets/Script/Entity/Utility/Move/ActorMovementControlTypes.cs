using System;
using UnityEngine;

public enum ActorMovementControlMode
{
    PushAway = 0,
    PullTowards = 1
}

public enum ActorMovementControlSpeedCurve
{
    Constant = 0,
    EaseOut = 1
}

public enum ActorMovementControlReapplyMode
{
    Replace = 0,
    IgnoreWhileActive = 1,
    KeepStronger = 2
}

[Serializable]
public struct ActorMovementControlRequest
{
    public ActorMovementControlMode mode;
    public ActorMovementControlSpeedCurve speedCurve;
    public ActorMovementControlReapplyMode reapplyMode;
    public Vector2 center;
    public Vector2 fallbackDirection;
    public float strength;
    public float duration;
    public float maxDistance;
    public float pullStopDistance;
    public bool suppressBaseMovement;
    public bool clearExternalVelocity;
    public bool allowWhileStopped;
    public bool holdAtCenter;

    // 지속 영역이 자신이 적용한 이동만 해제하기 위한 런타임 소유자.
    [NonSerialized] public object source;
}
