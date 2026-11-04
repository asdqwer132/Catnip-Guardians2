using System;
using UnityEngine;

public class WallSpriteInput : MonoBehaviour
{
    [SerializeField] private MergeBoxBindings[] boxes;
    [Header("Sprite Sheet (Resources)")]
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
        if (string.IsNullOrWhiteSpace(spriteSheetPath))
        {
            Debug.LogError("스프라이트 시트 경로가 비어 있습니다.", this);
            return;
        }

        string path = spriteSheetPath.Trim().Replace("(1)", $"({Mathf.Max(1, sheetNumber)})");
        // Resources.LoadAll에는 확장자 없이 전달합니다.
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - 4);

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        Sprite side = MatchSprite(sprites, sideSpriteName, path);
        Sprite bottom = MatchSprite(sprites, bottomSpriteName, path);
        Sprite back = MatchSprite(sprites, backSpriteName, path);

        if (boxes != null) foreach (var box in boxes)
        {
            if (box == null || !box.transform.IsChildOf(transform)) continue;
            foreach (var part in box.visuals)
            {
                if (part == null || part.renderer == null) continue;
                Sprite selected = (part.role == MergeBoxSpriteRole.SideWall || part.role == MergeBoxSpriteRole.LeftWall || part.role == MergeBoxSpriteRole.RightWall) ? side :
                    part.role == MergeBoxSpriteRole.BottomWall ? bottom :
                    part.role == MergeBoxSpriteRole.BackFrame ? back : null;
                if (selected != null) part.renderer.sprite = selected;
            }
        }

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

}
