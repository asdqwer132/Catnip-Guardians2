using UnityEngine;

public class WallSpriteInput : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] sideWall;
    [SerializeField] private SpriteRenderer bottomWall;
    [SerializeField] private SpriteRenderer backFrame;

    [SerializeField] private string spritePath = "MergeBox";
    [SerializeField] private string series = "01-weapon";

    [Header("Sprite Sheet (Resources)")]
    [SerializeField] private bool useSpriteSheet;
    [Tooltip("Resources-relative sheet path, without extension. Texture Sprite Mode must be Multiple.")]
    [SerializeField] private string spriteSheetPath;
    [SerializeField] private string sideSpriteName = "SideWall";
    [SerializeField] private string bottomSpriteName = "BottomWall";
    [SerializeField] private string backSpriteName = "Back";

    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake) ApplySprites();
    }

    [ContextMenu("Apply Sprites")]
    public void ApplySprites()
    {
        Sprite side, bottom, back;
        if (useSpriteSheet)
        {
            if (string.IsNullOrWhiteSpace(spriteSheetPath))
            {
                Debug.LogError("Wall sprite sheet path is empty.", this);
                return;
            }
            Sprite[] sprites = Resources.LoadAll<Sprite>(spriteSheetPath.Trim());
            side = MatchSprite(sprites, sideSpriteName);
            bottom = MatchSprite(sprites, bottomSpriteName);
            back = MatchSprite(sprites, backSpriteName);
        }
        else
        {
            string folder = $"{spritePath.TrimEnd('/')}/{series}";
            side = LoadSprite($"{folder}/SideWall");
            bottom = LoadSprite($"{folder}/BottomWall");
            back = LoadSprite($"{folder}/BoxBack 1");
        }

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

    private Sprite MatchSprite(Sprite[] sprites, string spriteName)
    {
        Sprite match = null;
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null || !string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (match != null)
            {
                Debug.LogError($"Duplicate wall sprite name '{spriteName}' in '{spriteSheetPath}'.", this);
                return null;
            }
            match = sprite;
        }
        if (match == null)
            Debug.LogError($"Wall sprite '{spriteName}' not found in '{spriteSheetPath}'. Check the Sprite Editor slice name.", this);
        return match;
    }

    private Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogError($"스프라이트를 불러오지 못했습니다: {path}", this);

        return sprite;
    }
}