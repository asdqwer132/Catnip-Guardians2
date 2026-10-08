using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class SpriteBatchSlicer : EditorWindow
{
    private DefaultAsset rootFolder;

    private float pixelPerUnit = 32f;
    private int sliceWidth = 100;
    private int sliceHeight = 100;

    private bool skipEmptySlices = true;

    [MenuItem("Tools/Utility/Sprite/Batch Slice Custom")]
    public static void Open()
    {
        GetWindow<SpriteBatchSlicer>("Sprite Batch Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("하위 폴더 전체 스프라이트 자동 설정", EditorStyles.boldLabel);

        rootFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Root Folder",
            rootFolder,
            typeof(DefaultAsset),
            false
        );

        GUILayout.Space(5);

        pixelPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelPerUnit);
        sliceWidth = EditorGUILayout.IntField("Slice Width", sliceWidth);
        sliceHeight = EditorGUILayout.IntField("Slice Height", sliceHeight);

        GUILayout.Space(5);

        skipEmptySlices = EditorGUILayout.Toggle("빈 조각 제거", skipEmptySlices);

        GUILayout.Space(10);

        if (GUILayout.Button("적용"))
        {
            if (rootFolder == null)
            {
                Debug.LogWarning("루트 폴더를 넣어주세요.");
                return;
            }

            if (pixelPerUnit <= 0)
            {
                Debug.LogWarning("Pixels Per Unit은 0보다 커야 합니다.");
                return;
            }

            if (sliceWidth <= 0 || sliceHeight <= 0)
            {
                Debug.LogWarning("Slice Width / Height는 0보다 커야 합니다.");
                return;
            }

            string rootPath = AssetDatabase.GetAssetPath(rootFolder);

            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                Debug.LogWarning("선택한 오브젝트가 폴더가 아닙니다.");
                return;
            }

            ProcessFolder(
                rootPath,
                pixelPerUnit,
                sliceWidth,
                sliceHeight,
                skipEmptySlices
            );
        }
    }

    private static void ProcessFolder(
        string rootPath,
        float ppu,
        int sliceWidth,
        int sliceHeight,
        bool skipEmptySlices
    )
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootPath });

        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            ApplySpriteSettings(
                importer,
                path,
                ppu,
                sliceWidth,
                sliceHeight,
                skipEmptySlices
            );

            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"스프라이트 일괄 처리 완료: {count}개");
    }

    private static void ApplySpriteSettings(
        TextureImporter importer,
        string path,
        float ppu,
        int sliceWidth,
        int sliceHeight,
        bool skipEmptySlices
    )
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;

        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // 픽셀 검사하려면 Read/Write가 켜져 있어야 함
        importer.isReadable = true;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            Debug.LogWarning($"Texture를 불러올 수 없음: {path}");
            return;
        }

        SliceWithSpriteDataProvider(
            importer,
            texture,
            path,
            sliceWidth,
            sliceHeight,
            skipEmptySlices
        );
    }

    private static void SliceWithSpriteDataProvider(
        TextureImporter importer,
        Texture2D texture,
        string path,
        int sliceWidth,
        int sliceHeight,
        bool skipEmptySlices
    )
    {
        int columns = texture.width / sliceWidth;
        int rows = texture.height / sliceHeight;

        if (columns <= 0 || rows <= 0)
        {
            Debug.LogWarning($"이미지가 슬라이스 크기보다 작아서 스킵됨: {path}");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider dataProvider =
            factory.GetSpriteEditorDataProviderFromObject(importer);

        if (dataProvider == null)
        {
            Debug.LogWarning($"SpriteDataProvider를 가져올 수 없음: {path}");
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();

        List<SpriteRect> spriteRects = new List<SpriteRect>();
        string fileName = Path.GetFileNameWithoutExtension(path);

        int index = 0;
        int skipped = 0;

        for (int y = rows - 1; y >= 0; y--)
        {
            for (int x = 0; x < columns; x++)
            {
                Rect rect = new Rect(
                    x * sliceWidth,
                    y * sliceHeight,
                    sliceWidth,
                    sliceHeight
                );

                if (skipEmptySlices && IsRectEmpty(texture, rect))
                {
                    skipped++;
                    continue;
                }

                SpriteRect spriteRect = new SpriteRect
                {
                    name = $"{fileName}_{index}",
                    rect = rect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };

                spriteRects.Add(spriteRect);
                index++;
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log(
            $"처리됨: {path} / 생성={spriteRects.Count}개 / 빈 조각 제거={skipped}개"
        );
    }

    private static bool IsRectEmpty(Texture2D texture, Rect rect)
    {
        int startX = Mathf.RoundToInt(rect.x);
        int startY = Mathf.RoundToInt(rect.y);
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        Color32[] pixels = texture.GetPixels32();

        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                int index = y * texture.width + x;

                if (index < 0 || index >= pixels.Length)
                    continue;

                if (pixels[index].a > 0)
                    return false;
            }
        }

        return true;
    }
}