using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyPatternPickGroup
{
    None,
    Random1,
    Random2,
    Reactive,
    Death
}

public enum EnemyPatternConditionType
{
    Always,
    HpRatioBelow,
    HpRatioAbove,
    DistanceToTargetLess,
    DistanceToTargetGreater,
    AfterDamaged,
    OnLethalDamage
}

public enum EnemyPatternActionType
{
    None,
    StatModifier,
    DamageReductionStance,
    ChargeToTarget,
    JumpToTarget,
    RetreatThenJump,
    CircleThenCharge,
    RangedAttack,
    AreaAttack,
    MultiAreaAttack,
    ZigzagMoveToTarget,
    TeleportBehindTarget,
    SupportNearbyEnemies,
    SpawnPrefab
}

[CreateAssetMenu(fileName = "EnemyPatternData", menuName = "Game/Enemy/Enemy Pattern Data")]
public class EnemyPatternData : ScriptableObject
{
    [Header("Interval")]
    public float random1Interval = 4f;
    public float random2Interval = 7f;
    public bool useRandom2OnlyBelowHp = false;
    [Range(0f, 1f)] public float random2HpRatio = 0.5f;

    [Header("Debug")]
    public bool showLog = false;

    [Header("Patterns")]
    public List<EnemyPatternInfo> patterns = new List<EnemyPatternInfo>();
}

[Serializable]
public class EnemyPatternInfo
{
    [Header("Basic")]
    public string patternName;
    [TextArea] public string memo;
    public bool enabled = true;
    public EnemyPatternPickGroup pickGroup = EnemyPatternPickGroup.Random1;
    public EnemyPatternConditionType conditionType = EnemyPatternConditionType.Always;
    [Min(0f)] public float weight = 1f;
    [Range(0f, 1f)] public float chance = 1f;
    [Min(0f)] public float cooldown = 0f;
    public bool consumeOnce = false;
    public bool blockDefaultAI = true;

    [Header("Condition Value")]
    [Range(0f, 1f)] public float hpRatio = 0.5f;
    [Min(0f)] public float distance = 3f;

    [Header("Action")]
    public EnemyPatternActionType actionType = EnemyPatternActionType.None;
    [Min(0f)] public float telegraphTime = 0.4f;
    [Min(0f)] public float duration = 0.6f;
    [Min(0f)] public float interval = 0.2f;
    [Min(1)] public int repeatCount = 1;

    [Header("Move / Attack Value")]
    [Min(0f)] public float speed = 5f;
    [Min(0f)] public float range = 1.5f;
    [Min(0f)] public float radius = 1f;
    public float damage = 0f;
    public float damageMultiplier = 1f;
    public float additionalDamage = 0f;
    public float retreatDistance = 1.5f;
    public float circleAngle = 180f;
    public float zigzagAmplitude = 0.8f;
    public float zigzagFrequency = 8f;
    public float teleportDistanceFromTarget = 1f;

    [Header("Runtime Stat Modifier")]
    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;
    public float attackCooldownMultiplier = 1f;
    public float attackRangeMultiplier = 1f;
    public float incomingDamageMultiplier = 1f;

    [Header("Support / Spawn")]
    public LayerMask targetLayerMask = ~0;
    [Min(0f)] public float healAmount = 0f;
    public GameObject spawnPrefab;
    [Min(1)] public int spawnCount = 1;
    [Min(0f)] public float spawnSpreadRadius = 0.5f;

    [Header("Visual Prefab")]
    public GameObject telegraphPrefab;
    public GameObject effectPrefab;
    public GameObject projectilePrefab;
}
