using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    [Header("Data")]
    public PlantData plantData;

    void Start()
    {
        if (PlantManager.instance != null &&
            PlantManager.instance.CurrentPlant != null)
        {
            plantData = PlantManager.instance.CurrentPlant;
        }

        UpdateSprite();
    }

    public void SetPlantData(PlantData data)
    {
        plantData = data;
        UpdateSprite();
    }

    public void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null || plantData == null)
            return;

        spriteRenderer.sprite = plantData.plantSprite;
    }
}