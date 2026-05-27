using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    private PlantData plantData;

    public void SetPlantData(PlantData data)
    {
        plantData = data;
    }

    public void SetGrowingSprite(int index)
    {
        if (spriteRenderer == null)
            return;

        if (plantData == null)
            return;

        if (plantData.growing == null || plantData.growing.Length == 0)
            return;

        if (index < 0 || index >= plantData.growing.Length)
            return;

        Sprite sprite = plantData.growing[index];

        if (sprite == null)
            return;

        spriteRenderer.sprite = sprite;
    }

    public void SetGrownUpSprite()
    {
        if (spriteRenderer == null)
            return;

        if (plantData == null)
            return;

        if (plantData.grownUp == null)
            return;

        spriteRenderer.sprite = plantData.grownUp;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = sprite;
    }
}