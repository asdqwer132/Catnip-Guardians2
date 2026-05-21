using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    private PlantData plantData;

    void Start()
    {
        if (PlantManager.instance != null && PlantManager.instance.CurrentPlant != null)
            plantData = PlantManager.instance.CurrentPlant;

        UpdateSprite();
    }

    public void SetPlantData(PlantData data)
    {
        plantData = data;
        UpdateSprite();
    }

    public void UpdateSprite()
    {
        if (spriteRenderer == null || plantData == null)
            return;

        spriteRenderer.sprite = plantData.icon;
    }
}