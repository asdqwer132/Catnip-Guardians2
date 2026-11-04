using System;
using UnityEngine;

public class WallSpriteInput : MonoBehaviour
{
    [SerializeField] private MergeBoxBindings[] boxes;
    [SerializeField] private SpriteRenderer[] sideWall;
    [SerializeField] private SpriteRenderer bottomWall;
    [SerializeField] private SpriteRenderer backFrame;

    [SerializeField] private string spritePath = "MergeBox";
    [SerializeField] private string series = "01-weapon";

    [Header("Sprite Sheet (Resources)")]
    [SerializeField] private bool useSpriteSheet;

    [Tooltip("Resources 기준 경로. (1)은 Sheet Number로 교체됩니다.")]
    [SerializeField]
    private string spriteSheetPath =
        "Item/MergeBox/MergeBox (1).png";

    [SerializeField, Min(1)] private int sheetNumber = 1;

    [SerializeField] private string sideSpriteName = "SideWall";
    [SerializeField] private string bottomSpriteName = "BottomWall";
    [SerializeField] private string backSpriteName = "Back";

    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
            ApplySprites();
    }

    [ContextMenu("Apply Sprites")]
    public void ApplySprites()
    {
        Sprite side;
        Sprite bottom;
        Sprite back;

        if (useSpriteSheet)
        {
            if (string.IsNullOrWhiteSpace(spriteSheetPath))
            {
                Debug.LogError("스프라이트 시트 경로가 비어 있습니다.", this);
                return;
            }

            string path = spriteSheetPath.Trim()
                .Replace("(1)", $"({sheetNumber})");

            // Resources.LoadAll에는 확장자 없이 전달합니다.
            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 4);

            Sprite[] sprites = Resources.LoadAll<Sprite>(path);

            side = MatchSprite(sprites, sideSpriteName, path);
            bottom = MatchSprite(sprites, bottomSpriteName, path);
            back = MatchSprite(sprites, backSpriteName, path);
        }
        else
        {
            string folder = $"{spritePath.TrimEnd('/')}/{series}";

            side = LoadSprite($"{folder}/SideWall");
            bottom = LoadSprite($"{folder}/BottomWall");
            back = LoadSprite($"{folder}/BoxBack 1");
        }

        if (boxes != null) foreach (var box in boxes)
        {
            if (box == null || !box.transform.IsChildOf(transform)) continue;
            foreach (var part in box.visuals)
            {
                if (part == null || part.renderer == null) continue;
                Sprite selected = part.role == MergeBoxSpriteRole.SideWall ? side :
                    part.role == MergeBoxSpriteRole.BottomWall ? bottom :
                    part.role == MergeBoxSpriteRole.BackFrame ? back : null;
                if (selected != null) part.renderer.sprite = selected;
            }
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

    private Sprite MatchSprite(Sprite[] sprites, string spriteName, string path)
    {
        Sprite match = null;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null ||
                !string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (match != null)
            {
                Debug.LogError($"'{path}'에 '{spriteName}' 스프라이트가 중복됩니다.", this);
                return null;
            }

            match = sprite;
        }

        if (match == null)
        {
            Debug.LogError(
                $"'{path}'에서 '{spriteName}'을 찾지 못했습니다. Sprite Editor의 슬라이스 이름을 확인하세요.",
                this);
        }

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