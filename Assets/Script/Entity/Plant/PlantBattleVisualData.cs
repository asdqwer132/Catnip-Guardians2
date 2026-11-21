using UnityEngine;

[CreateAssetMenu(menuName = "Entities/Plant/Battle Visual Data")]
public sealed class PlantBattleVisualData : ScriptableObject
{
    public AnimationClip idle;
    public AnimationClip hurt;
    public bool useHurtCooldown = true;
    [Min(0f)] public float hurtCooldown = .5f;
}
