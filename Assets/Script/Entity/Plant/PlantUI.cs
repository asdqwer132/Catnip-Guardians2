using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;
    [SerializeField] private Animator displayAnimator;
    [SerializeField] private PlantBattleVisualData defaultBattleVisual;

    private readonly BattleAnimationPlayer animationPlayer = new BattleAnimationPlayer();
    private PlantData plantData;
    private PlantBattleVisualData battleVisual;
    private Sprite growthSprite;
    private int growthStage;
    private bool isHurt;
    private float nextHurtAt;
    public Sprite CurrentSprite => growthSprite != null ? growthSprite : spriteRenderer != null ? spriteRenderer.sprite : null;

    public void SetPlantData(PlantData data)
    {
        plantData = data;
        growthStage = -1;
        nextHurtAt = 0f;
        ApplyGrowthStage(data != null ? data.GetGrowthStageIndex(0f) : -1, false);
    }

    public void SetGrowthProgress(float progress01)
    {
        int stage = plantData != null ? plantData.GetGrowthStageIndex(progress01) : -1;
        var visual = stage >= 0 ? plantData.growthVisuals[stage] : null;
        if (stage != growthStage || (visual != null ? visual : defaultBattleVisual) != battleVisual)
            ApplyGrowthStage(stage, stage > growthStage);
    }

    private void ApplyGrowthStage(int stage, bool playGrowthEffect)
    {
        growthStage = stage;
        var stageVisual = stage >= 0 ? plantData.growthVisuals[stage] : null;
        battleVisual = stageVisual != null ? stageVisual : defaultBattleVisual;
        growthSprite = battleVisual != null ? battleVisual.sprite : null;
        isHurt = false;
        if (isActiveAndEnabled) PlayIdle();
        else
        {
            animationPlayer.Stop();
            if (spriteRenderer != null) spriteRenderer.sprite = growthSprite;
        }

        if (playGrowthEffect && stageVisual != null && stageVisual.growthEffect != null)
        {
            Transform anchor = spriteRenderer != null ? spriteRenderer.transform : transform;
            stageVisual.growthEffect.Play(new EffectVisualContext(anchor.position, anchor.rotation, audioOwner: gameObject));
        }
    }

    public void PlayHurt()
    {
        if (!isActiveAndEnabled || battleVisual == null || displayAnimator == null || battleVisual.hurt == null ||
            (battleVisual.useHurtCooldown && Time.time < nextHurtAt)) return;

        nextHurtAt = Time.time + battleVisual.hurtCooldown;
        animationPlayer.Play(displayAnimator, battleVisual.hurt, restart: true);
        isHurt = true;
    }

    private void LateUpdate()
    {
        animationPlayer.Tick(Time.deltaTime, !isHurt);
        if (isHurt && animationPlayer.IsFinished)
        {
            isHurt = false;
            PlayIdle();
        }
    }

    private void OnEnable()
    {
        if (plantData != null) PlayIdle();
    }

    private void PlayIdle()
    {
        if (spriteRenderer != null) spriteRenderer.sprite = growthSprite;
        animationPlayer.Play(displayAnimator, battleVisual != null ? battleVisual.idle : null, restart: true);
    }

    private void OnDisable()
    {
        isHurt = false;
        animationPlayer.Stop();
        if (spriteRenderer != null && plantData != null) spriteRenderer.sprite = growthSprite;
    }
}
