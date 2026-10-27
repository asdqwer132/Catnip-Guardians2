using UnityEngine;

public class WallSpriteInput : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] sideWall;
    [SerializeField] private SpriteRenderer bottomWall;
    [SerializeField] private SpriteRenderer backFrame;

    [SerializeField] private string spritePath = "MergeBox";
    [SerializeField] private string series = "01-weapon";

    private void Awake()
    {
        ApplySprites();
    }

    [ContextMenu("Apply Sprites")]
    public void ApplySprites()
    {
        string folder = $"{spritePath.TrimEnd('/')}/{series}";

        Sprite side = LoadSprite($"{folder}/SideWall");
        Sprite bottom = LoadSprite($"{folder}/BottomWall");
        Sprite back = LoadSprite($"{folder}/BoxBack 1");

        if (side != null && sideWall != null)
        {
            foreach (SpriteRenderer wall in sideWall)
            {
                if (wall != null)
                    wall.sprite = side;
            }
        }

        if (bottom != null && bottomWall != null)
            bottomWall.sprite = bottom;

        if (back != null && backFrame != null)
            backFrame.sprite = back;
    }

    private Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogError($"스프라이트를 불러오지 못했습니다: {path}", this);

        return sprite;
    }
}