using UnityEngine;

[CreateAssetMenu(menuName = "Entities/Player/Battle Visual Data")]
public sealed class PlayerBattleVisualData : ScriptableObject
{
    public AnimationClip idle;
    public AnimationClip move;
    public AnimationClip attack;

    [Header("Circle Movement")]
    public bool useCircleMovement = true;
    [Min(0f)] public float radius = 1.5f;
    [Min(.01f)] public float idleInterval = 3f;
    [Min(0f)] public float intervalVariation = 1f;
}
