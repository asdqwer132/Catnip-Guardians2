using UnityEngine;
using UnityEngine.UI;

public class ChildrenUIControler : MonoBehaviour
{
    Graphic[] graphics;
    Color[] colors;
    private bool isColorApply = false;
    private void Awake()
    {
        graphics = gameObject.GetComponentsInChildren<Graphic>();
        colors = new Color[graphics.Length];
    }
    public void ResetColor()
    {
        if(!isColorApply) return;
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic.gameObject == gameObject)
                continue;
            graphic.color = colors[i];
        }
    }
    public void ApplyColor()
    {
        isColorApply = true;
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic.gameObject == gameObject)
                continue;

            Color color = graphic.color;
            colors[i] = color;
            graphic.color = new Color(
                color.r * 0.5f,
                color.g * 0.5f,
                color.b * 0.5f,
                color.a
            );
        }
    }
}
