using System;
using UnityEngine;

public class FoodMergeItemBurningEffect : MonoBehaviour
{
    public SpriteRenderer frame;
    public SpriteRenderer item;

    public Color[] colors;


    public void Bake(int index)
    {
        if (index > colors.Length) return;
        frame.color = colors[index];
        item.color = colors[index]; 
    }
}
