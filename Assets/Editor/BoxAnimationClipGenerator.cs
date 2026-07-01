#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BoxAnimationClipGenerator : EditorWindow
{
    [Header("Sprite Sheet")]
    [SerializeField] private Texture2D spriteSheet;

    [Header("Grid")]
    [SerializeField] private int rowCount = 4;      // 세로: 상자 종류 개수
    [SerializeField] private int frameCount = 6;    // 가로: 애니메이션 프레임 개수

    [Header("Clip")]
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private string outputFolder = "Assets/Animations/Box";
    [SerializeField] private string clipPrefix = "Box";

    [Header("Sort")]
    [Tooltip("일반적인 스프라이트시트처럼 위쪽 줄부터 0번으로 볼지")]
    [SerializeField] private bool topToBottom = true;

    [MenuItem("Tools/Box/Create Box Animation Clips")]
    public static void Open()
    {
        GetWindow<BoxAnimationClipGenerator>("Box Clip Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite Sheet", EditorStyles.boldLabel);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            "Sprite Sheet",
            spriteSheet,
            typeof(Texture2D),
            false
        );

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

        rowCount = EditorGUILayout.IntField("Row Count / 상자 종류 개수", rowCount);
        frameCount = EditorGUILayout.IntField("Frame Count / 가로 프레임 개수", frameCount);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Clip", EditorStyles.boldLabel);

        frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        clipPrefix = EditorGUILayout.TextField("Clip Prefix", clipPrefix);

        EditorGUILayout.Space(8f);
        topToBottom = EditorGUILayout.Toggle("Top To Bottom", topToBottom);

        EditorGUILayout.Space(12f);

        GUI.enabled = spriteSheet != null && rowCount > 0 && frameCount > 0;

        if (GUILayout.Button("Create Animation Clips", GUILayout.Height(32f)))
        {
            CreateClips();
        }

        GUI.enabled = true;
    }

    private void CreateClips()
    {
        string sheetPath = AssetDatabase.GetAssetPath(spriteSheet);

        if (string.IsNullOrEmpty(sheetPath))
        {
            Debug.LogError("스프라이트시트 경로를 찾을 수 없습니다.");
            return;
        }

        Sprite[] sprites = LoadAndSortSprites(sheetPath);

        int requiredCount = rowCount * frameCount;

        if (sprites.Length < requiredCount)
        {
            Debug.LogError($"스프라이트 개수가 부족합니다. 필요: {requiredCount}, 현재: {sprites.Length}");
            return;
        }

        EnsureFolder(outputFolder);

        for (int row = 0; row < rowCount; row++)
        {
            Sprite idleSprite = GetSprite(sprites, row, 0);

            List<Sprite> openSprites = new List<Sprite>();

            for (int frame = 0; frame < frameCount; frame++)
            {
                openSprites.Add(GetSprite(sprites, row, frame));
            }

            AnimationClip idleClip = CreateSpriteAnimationClip(
                new List<Sprite> { idleSprite },
                frameRate,
                true
            );

            AnimationClip openClip = CreateSpriteAnimationClip(
                openSprites,
                frameRate,
                false
            );

            string idlePath = $"{outputFolder}/{clipPrefix}_{row:00}_Idle.anim";
            string openPath = $"{outputFolder}/{clipPrefix}_{row:00}_Open.anim";

            AssetDatabase.CreateAsset(idleClip, idlePath);
            AssetDatabase.CreateAsset(openClip, openPath);

            Debug.Log($"생성 완료: {idlePath}");
            Debug.Log($"생성 완료: {openPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("상자 애니메이션 클립 생성 완료");
    }

    private Sprite GetSprite(Sprite[] sprites, int row, int frame)
    {
        int index = row * frameCount + frame;
        return sprites[index];
    }

    private Sprite[] LoadAndSortSprites(string sheetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);

        List<Sprite> sprites = new List<Sprite>();

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        sprites.Sort((a, b) =>
        {
            Rect ar = a.rect;
            Rect br = b.rect;

            int yCompare;

            if (topToBottom)
                yCompare = br.y.CompareTo(ar.y);
            else
                yCompare = ar.y.CompareTo(br.y);

            if (yCompare != 0)
                return yCompare;

            return ar.x.CompareTo(br.x);
        });

        return sprites.ToArray();
    }

    private AnimationClip CreateSpriteAnimationClip(List<Sprite> sprites, float fps, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(Image),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];

        float frameTime = 1f / Mathf.Max(1f, fps);

        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');

        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
#endif