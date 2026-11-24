using System;
using UnityEngine;

public class FoodMergeItemBurningEffect : MonoBehaviour
{
    public SpriteRenderer frame;
    public SpriteRenderer item;

    public Color[] colors;


    public void Bake(int index)
    {
        if (colors == null || index < 0 || index >= colors.Length) return;
        if (frame != null) frame.color = colors[index];
        if (item != null) item.color = colors[index];
    }
}
