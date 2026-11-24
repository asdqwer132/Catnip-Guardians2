using UnityEngine;
using UnityEngine.UI;

public class SpriteButton : MonoBehaviour
{
    public Button button;
    public Image sprite;

    public void SetSprite(Sprite sprite) => this.sprite.sprite = sprite;    
}
