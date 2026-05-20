#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TemplatePrefabPerSpriteBuilder : EditorWindow
{
    private DefaultAsset templatePrefabFolder;
    private DefaultAsset spriteSourceRootFolder;
    private DefaultAsset outputPrefabFolder;

    private float secondsPerFrame = 0.05f;
    private bool loopAnimation = false;
    private bool overwrite = true;
    private bool includeInactiveChildren = true;

    [MenuItem("Tools/Animation/Template Prefab Per Sprite Builder")]
    public static void Open()
    {
        GetWindow<TemplatePrefabPerSpriteBuilder>("Prefab Per Sprite");
    }

    private void OnGUI()
    {
        GUILayout.Label("Template Prefab Per Sprite Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        templatePrefabFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Template Prefab Folder",
            templatePrefabFolder,
            typeof(DefaultAsset),
            false
        );

        spriteSourceRootFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Sprite Source Root",
            spriteSourceRootFolder,
            typeof(DefaultAsset),
            false
        );

        outputPrefabFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Prefab Folder",
            outputPrefabFolder,
            typeof(DefaultAsset),
            false
        );

        EditorGUILayout.Space();

        secondsPerFrame = EditorGUILayout.FloatField("Seconds Per Frame", secondsPerFrame);
        secondsPerFrame = Mathf.Max(0.001f, secondsPerFrame);

        loopAnimation = EditorGUILayout.Toggle("Loop Animation", loopAnimation);
        overwrite = EditorGUILayout.Toggle("Overwrite", overwrite);
        includeInactiveChildren = EditorGUILayout.Toggle("Include Inactive Children", includeInactiveChildren);

        EditorGUILayout.Space();

        if (GUILayout.Button("Build"))
        {
            Build();
        }
    }

    private void Build()
    {
        string templateFolderPath = GetAssetPath(templatePrefabFolder);
        string spriteRootPath = GetAssetPath(spriteSourceRootFolder);
        string outputPath = GetAssetPath(outputPrefabFolder);

        if (!IsValidFolder(templateFolderPath))
        {
            Debug.LogError("Template Prefab Folder가 올바르지 않습니다.");
            return;
        }

        if (!IsValidFolder(spriteRootPath))
        {
            Debug.LogError("Sprite Source Root가 올바르지 않습니다.");
            return;
        }

        if (!IsValidFolder(outputPath))
        {
            Debug.LogError("Output Prefab Folder가 올바르지 않습니다.");
            return;
        }

        GameObject templatePrefab = LoadFirstPrefab(templateFolderPath);

        if (templatePrefab == null)
        {
            Debug.LogError("Template Prefab Folder 안에 프리팹이 없습니다.");
            return;
        }

        string templatePrefabPath = AssetDatabase.GetAssetPath(templatePrefab);

        string animRootPath = $"{outputPath}/Animations";
        string controllerRootPath = $"{outputPath}/Controllers";

        EnsureFolder(animRootPath);
        EnsureFolder(controllerRootPath);

        List<string> spriteAssetPaths = GetSpriteAssetPaths(spriteRootPath);

        if (spriteAssetPaths.Count == 0)
        {
            Debug.LogError("Sprite Source Root 아래에서 스프라이트 파일을 찾지 못했습니다.");
            return;
        }

        int success = 0;
        int failed = 0;

        foreach (string spriteAssetPath in spriteAssetPaths)
        {
            bool result = BuildOne(
                templatePrefabPath,
                spriteAssetPath,
                outputPath,
                animRootPath,
                controllerRootPath
            );

            if (result)
                success++;
            else
                failed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"생성 완료 / 성공: {success}, 실패: {failed}");
    }
    private bool BuildOne(
        string templatePrefabPath,
        string spriteAssetPath,
        string outputPath,
        string animRootPath,
        string controllerRootPath)
    {
        string newPrefabName = Path.GetFileNameWithoutExtension(spriteAssetPath);

        Sprite[] sprites = LoadSpritesFromAsset(spriteAssetPath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[{newPrefabName}] 스프라이트 없음: {spriteAssetPath}");
            return false;
        }

        string targetPrefabPath = $"{outputPath}/{newPrefabName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath) != null)
        {
            if (!overwrite)
            {
                Debug.LogWarning($"[{newPrefabName}] 이미 존재해서 건너뜀");
                return false;
            }

            AssetDatabase.DeleteAsset(targetPrefabPath);
        }

        bool copied = AssetDatabase.CopyAsset(templatePrefabPath, targetPrefabPath);

        if (!copied)
        {
            Debug.LogError($"[{newPrefabName}] 프리팹 복사 실패");
            return false;
        }

        GameObject copiedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(copiedPrefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError($"[{newPrefabName}] 프리팹 인스턴스 생성 실패");
            return false;
        }

        instance.name = newPrefabName;

        Animator animator = instance.GetComponentInChildren<Animator>(includeInactiveChildren);

        if (animator == null)
        {
            Debug.LogWarning($"[{newPrefabName}] Animator가 없습니다.");
            DestroyImmediate(instance);
            return false;
        }

        SpriteRenderer spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>(includeInactiveChildren);

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{newPrefabName}] SpriteRenderer가 없습니다.");
            DestroyImmediate(instance);
            return false;
        }

        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;

        if (originalController == null)
        {
            Debug.LogWarning($"[{newPrefabName}] Animator Controller가 없습니다.");
            DestroyImmediate(instance);
            return false;
        }

        AnimationClip[] originalClips = originalController.animationClips;

        if (originalClips == null || originalClips.Length == 0)
        {
            Debug.LogWarning($"[{newPrefabName}] 기존 Controller에 클립이 없습니다.");
            DestroyImmediate(instance);
            return false;
        }

        AnimationClip baseClip = originalClips[0];

        string spriteRendererPath = GetRelativePath(animator.transform, spriteRenderer.transform);

        // 핵심 수정 1:
        // 기존 DustAnim 이름을 쓰지 않고, 새 프리팹 이름과 같은 클립 이름 사용
        string newClipName = newPrefabName;

        // 핵심 수정 2:
        // GeneratedAnimations/개별폴더/클립.anim 이 아니라
        // GeneratedAnimations/클립.anim 으로 바로 저장
        string newClipPath = $"{animRootPath}/{newClipName}.anim";

        AnimationClip newClip = CreateSpriteAnimationClip(
            newClipName,
            sprites,
            spriteRendererPath
        );

        SaveAnimationClip(newClip, newClipPath);

        string overrideControllerPath = $"{controllerRootPath}/{newPrefabName}_Override.overrideController";

        AnimatorOverrideController overrideController = CreateOverrideController(
            newPrefabName,
            originalController,
            baseClip,
            newClip,
            overrideControllerPath
        );

        animator.runtimeAnimatorController = overrideController;

        spriteRenderer.sprite = sprites[0];

        PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
        DestroyImmediate(instance);

        Debug.Log($"[{newPrefabName}] 생성 완료 / 클립: {newClipName} / 프레임 수: {sprites.Length}");

        return true;
    }

    private AnimationClip CreateSpriteAnimationClip(
        string clipName,
        Sprite[] sprites,
        string spriteRendererPath)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = clipName;
        clip.frameRate = 60f;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * secondsPerFrame,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = spriteRendererPath,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loopAnimation;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);

        return clip;
    }

    private AnimatorOverrideController CreateOverrideController(
        string newPrefabName,
        RuntimeAnimatorController originalController,
        AnimationClip baseClip,
        AnimationClip newClip,
        string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path) != null)
        {
            if (overwrite)
                AssetDatabase.DeleteAsset(path);
            else
                path = AssetDatabase.GenerateUniqueAssetPath(path);
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        overrideController.name = $"{newPrefabName}_Override";
        overrideController.runtimeAnimatorController = originalController;

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();

        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;

            if (originalClip == baseClip)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                    originalClip,
                    newClip
                );
            }
        }

        overrideController.ApplyOverrides(overrides);

        AssetDatabase.CreateAsset(overrideController, path);

        return overrideController;
    }

    private List<string> GetSpriteAssetPaths(string rootPath)
    {
        List<string> result = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { rootPath });

        HashSet<string> uniqueAssetPaths = new HashSet<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            uniqueAssetPaths.Add(path);
        }

        result.AddRange(uniqueAssetPaths);
        result.Sort((a, b) => EditorUtility.NaturalCompare(a, b));

        return result;
    }

    private Sprite[] LoadSpritesFromAsset(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        List<Sprite> sprites = new List<Sprite>();

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

        return sprites.ToArray();
    }

    private void SaveAnimationClip(AnimationClip clip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
        {
            if (overwrite)
                AssetDatabase.DeleteAsset(path);
            else
                path = AssetDatabase.GenerateUniqueAssetPath(path);
        }

        AssetDatabase.CreateAsset(clip, path);
    }

    private GameObject LoadFirstPrefab(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        if (guids == null || guids.Length == 0)
            return null;

        System.Array.Sort(guids);

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
            return "";

        List<string> names = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();

        return string.Join("/", names);
    }

    private string GetAssetPath(DefaultAsset asset)
    {
        if (asset == null)
            return null;

        return AssetDatabase.GetAssetPath(asset);
    }

    private bool IsValidFolder(string path)
    {
        return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
    }

    private void EnsureFolder(string path)
    {
        path = path.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path);
        string folderName = Path.GetFileName(path);

        parent = parent.Replace("\\", "/");

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif