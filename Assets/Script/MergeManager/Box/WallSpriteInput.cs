
using System;
using UnityEngine;

public class WallSpriteInput : MonoBehaviour
{
    [SerializeField] private MergeBoxBindings[] boxes;

    [Header("Sprite Sheet (Resources)")]
    [Tooltip("Resources 기준 경로. (1)은 Sheet Number로 교체됩니다.")]
    [SerializeField]
    private string spriteSheetPath = "Item/MergeBox/MergeBox (1).png";

    [SerializeField, Min(1)] private int sheetNumber = 1;

    [SerializeField] private string sideSpriteName = "SideWall";
    [SerializeField] private string leftDecoSpriteName = "LeftDeco";
    [SerializeField] private string rightDecoSpriteName = "RightDeco";
    [SerializeField] private string bottomSpriteName = "BottomWall";

    [Header("Separate Back Sprite")]
    [SerializeField] private string backSpriteName = "MergeBox Back (1).png";

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

        int number = Mathf.Max(1, sheetNumber);

        string path = spriteSheetPath.Trim()
            .Replace("(1)", $"({number})");

        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - 4);

        // 기본 스프라이트 시트
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);

        Sprite side = MatchSprite(sprites, sideSpriteName, path);
        Sprite bottom = MatchSprite(sprites, bottomSpriteName, path);

        // BackFrame은 같은 폴더의 별도 파일에서 로드
        int slashIndex = path.LastIndexOf('/');
        string folder = slashIndex >= 0
            ? path.Substring(0, slashIndex + 1)
            : "";

        string backPath = folder + backSpriteName.Trim()
            .Replace("(1)", $"({number})");

        if (backPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            backPath = backPath.Substring(0, backPath.Length - 4);

        Sprite back = Resources.Load<Sprite>(backPath);

        if (back == null)
            Debug.LogError($"Back 스프라이트를 찾지 못했습니다: {backPath}", this);

        Sprite leftDeco = null;
        Sprite rightDeco = null;
        bool leftDecoResolved = false;
        bool rightDecoResolved = false;

        if (boxes != null)
        {
            foreach (var box in boxes)
            {
                if (box == null || !box.transform.IsChildOf(transform))
                    continue;

                foreach (var part in box.visuals)
                {
                    if (part == null || part.renderer == null)
                        continue;

                    if (part.role == MergeBoxSpriteRole.LeftDeco && !leftDecoResolved)
                    {
                        leftDeco = MatchSprite(sprites, leftDecoSpriteName, path);
                        leftDecoResolved = true;
                    }

                    if (part.role == MergeBoxSpriteRole.RightDeco && !rightDecoResolved)
                    {
                        rightDeco = MatchSprite(sprites, rightDecoSpriteName, path);
                        rightDecoResolved = true;
                    }

                    Sprite selected =
                        (part.role == MergeBoxSpriteRole.SideWall ||
                         part.role == MergeBoxSpriteRole.LeftWall ||
                         part.role == MergeBoxSpriteRole.RightWall) ? side :
                        part.role == MergeBoxSpriteRole.BottomWall ? bottom :
                        part.role == MergeBoxSpriteRole.BackFrame ? back :
                        part.role == MergeBoxSpriteRole.LeftDeco ? leftDeco :
                        part.role == MergeBoxSpriteRole.RightDeco ? rightDeco :
                        null;

                    if (selected != null)
                        part.renderer.sprite = selected;
                }
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
                Debug.LogError(
                    $"'{path}'에 '{spriteName}' 스프라이트가 중복됩니다.", this);
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
