using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;
    private PlantData plantData;
    public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

    public void SetPlantData(PlantData data)
    {
        plantData = data;
        SetGrowthProgress(0f);
    }

    public void SetGrowthProgress(float progress01)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = plantData != null ? plantData.GetGrowthSprite(progress01) : null;
    }
}
