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
    private bool isHurt;
    private float nextHurtAt;
    public Sprite CurrentSprite => growthSprite != null ? growthSprite : spriteRenderer != null ? spriteRenderer.sprite : null;

    public void SetPlantData(PlantData data)
    {
        plantData = data;
        battleVisual = data != null && data.battleVisual != null ? data.battleVisual : defaultBattleVisual;
        nextHurtAt = 0f;
        isHurt = false;
        animationPlayer.Stop();
        SetGrowthProgress(0f);
        PlayIdle();
    }

    public void SetGrowthProgress(float progress01)
    {
        growthSprite = plantData != null ? plantData.GetGrowthSprite(progress01) : null;
        if (!isHurt && spriteRenderer != null) spriteRenderer.sprite = growthSprite;
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
