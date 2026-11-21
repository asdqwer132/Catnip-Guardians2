using UnityEngine;

[CreateAssetMenu(menuName = "Entities/Plant/Battle Visual Data")]
public sealed class PlantBattleVisualData : ScriptableObject
{
    public Sprite sprite;
    public AnimationClip idle;
    public AnimationClip hurt;
    public bool useHurtCooldown = true;
    [Min(0f)] public float hurtCooldown = .5f;

    [Tooltip("이 성장 단계에 도달했을 때 재생할 이펙트입니다.")]
    public EffectVisualData growthEffect;
}
