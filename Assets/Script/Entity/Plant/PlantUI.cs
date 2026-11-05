using UnityEngine;

public class PlantUI : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    public void SetPlantData(PlantData data)
    {
        if (spriteRenderer != null) spriteRenderer.sprite = data != null ? data.visualSprite : null;
    }
}
