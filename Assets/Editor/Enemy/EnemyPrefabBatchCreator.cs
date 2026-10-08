#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyPrefabBatchCreator : EditorWindow
{
    [Header("Folders")]
    [SerializeField] private DefaultAsset templateFolder;
    [SerializeField] private DefaultAsset resourceRootFolder;
    [SerializeField] private DefaultAsset outputFolder;

    [Header("Template Names")]
    [SerializeField] private string templateEnemyName = "DefaultEnemy";

    [Header("Sprite Name Rules")]
    [SerializeField] private string idleSpriteSuffix = "-Idle";
    [SerializeField] private string attackSpriteSuffix = "-Attack01";
    [SerializeField] private string hitSpriteSuffix = "-Hurt";
    [SerializeField] private string dieSpriteSuffix = "-Death";
    [SerializeField] private string walkSpriteSuffix = "-Walk";

    [Header("Generated Clip Names")]
    [SerializeField] private string idleClipName = "Idle";
    [SerializeField] private string attackClipName = "Attack";
    [SerializeField] private string hitClipName = "Hit";
    [SerializeField] private string dieClipName = "Die";
    [SerializeField] private string walkClipName = "Walk";

    [Header("Animation Settings")]
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loopIdle = true;
    [SerializeField] private bool loopWalk = true;
    [SerializeField] private bool loopAttack = false;
    [SerializeField] private bool loopHit = false;
    [SerializeField] private bool loopDie = false;

    [Header("Debug")]
    [SerializeField] private bool logFrameCount = true;

    [MenuItem("Tools/Enemy/Create Enemy Prefabs From Sprite Folders")]
    public static void Open()
    {
        GetWindow<EnemyPrefabBatchCreator>("Enemy Prefab Creator");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);

        templateFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Template Folder",
            templateFolder,
            typeof(DefaultAsset),
            false
        );

        templateEnemyName = EditorGUILayout.TextField("Template Enemy Name", templateEnemyName);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Resource / Output", EditorStyles.boldLabel);

        resourceRootFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Resource Root Folder",
            resourceRootFolder,
            typeof(DefaultAsset),
            false
        );

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Sprite Name Rules", EditorStyles.boldLabel);

        idleSpriteSuffix = EditorGUILayout.TextField("Idle Sprite Suffix", idleSpriteSuffix);
        attackSpriteSuffix = EditorGUILayout.TextField("Attack Sprite Suffix", attackSpriteSuffix);
        hitSpriteSuffix = EditorGUILayout.TextField("Hit Sprite Suffix", hitSpriteSuffix);
        dieSpriteSuffix = EditorGUILayout.TextField("Die Sprite Suffix", dieSpriteSuffix);
        walkSpriteSuffix = EditorGUILayout.TextField("Walk Sprite Suffix", walkSpriteSuffix);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Generated Clip Names", EditorStyles.boldLabel);

        idleClipName = EditorGUILayout.TextField("Idle Clip Name", idleClipName);
        attackClipName = EditorGUILayout.TextField("Attack Clip Name", attackClipName);
        hitClipName = EditorGUILayout.TextField("Hit Clip Name", hitClipName);
        dieClipName = EditorGUILayout.TextField("Die Clip Name", dieClipName);
        walkClipName = EditorGUILayout.TextField("Walk Clip Name", walkClipName);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

        frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);

        loopIdle = EditorGUILayout.Toggle("Loop Idle", loopIdle);
        loopWalk = EditorGUILayout.Toggle("Loop Walk", loopWalk);
        loopAttack = EditorGUILayout.Toggle("Loop Attack", loopAttack);
        loopHit = EditorGUILayout.Toggle("Loop Hit", loopHit);
        loopDie = EditorGUILayout.Toggle("Loop Die", loopDie);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        logFrameCount = EditorGUILayout.Toggle("Log Frame Count", logFrameCount);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Create Enemy Prefabs", GUILayout.Height(36)))
        {
            CreateEnemyPrefabs();
        }
    }

    private void CreateEnemyPrefabs()
    {
        string templateFolderPath = GetAssetPath(templateFolder);
        string resourceRootPath = GetAssetPath(resourceRootFolder);
        string outputPath = GetAssetPath(outputFolder);

        if (string.IsNullOrEmpty(templateFolderPath))
        {
            Debug.LogError("Template Folder가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(resourceRootPath))
        {
            Debug.LogError("Resource Root Folder가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Debug.LogError("Output Folder가 없습니다.");
            return;
        }

        string templatePrefabPath = FindAssetPath<GameObject>(
            templateFolderPath,
            templateEnemyName
        );

        string templateControllerPath = FindAssetPath<AnimatorController>(
            templateFolderPath,
            templateEnemyName
        );

        string templateStatPath = FindAssetPath<EnemyStatData>(
            templateFolderPath,
            templateEnemyName
        );

        if (string.IsNullOrEmpty(templatePrefabPath))
        {
            Debug.LogError($"{templateEnemyName} 프리팹을 찾지 못했습니다.");
            return;
        }

        if (string.IsNullOrEmpty(templateControllerPath))
        {
            Debug.LogError($"{templateEnemyName} AnimatorController를 찾지 못했습니다.");
            return;
        }

        if (string.IsNullOrEmpty(templateStatPath))
        {
            Debug.LogError($"{templateEnemyName} EnemyStatData를 찾지 못했습니다.");
            return;
        }

        string[] enemyFolders = AssetDatabase.GetSubFolders(resourceRootPath);

        if (enemyFolders == null || enemyFolders.Length == 0)
        {
            Debug.LogWarning("Resource Root Folder 안에 적 폴더가 없습니다.");
            return;
        }

        int createdCount = 0;

        foreach (string enemyFolderPath in enemyFolders)
        {
            string enemyName = Path.GetFileName(enemyFolderPath);

            List<Sprite> idleSprites = FindSprites(
                enemyFolderPath,
                enemyName + idleSpriteSuffix,
                logFrameCount
            );

            List<Sprite> attackSprites = FindSprites(
                enemyFolderPath,
                enemyName + attackSpriteSuffix,
                logFrameCount
            );

            List<Sprite> hitSprites = FindSprites(
                enemyFolderPath,
                enemyName + hitSpriteSuffix,
                logFrameCount
            );

            List<Sprite> dieSprites = FindSprites(
                enemyFolderPath,
                enemyName + dieSpriteSuffix,
                logFrameCount
            );

            List<Sprite> walkSprites = FindSprites(
                enemyFolderPath,
                enemyName + walkSpriteSuffix,
                logFrameCount
            );

            if (!HasSprites(enemyName, "Idle", idleSprites)) continue;
            if (!HasSprites(enemyName, "Attack", attackSprites)) continue;
            if (!HasSprites(enemyName, "Hit", hitSprites)) continue;
            if (!HasSprites(enemyName, "Die", dieSprites)) continue;
            if (!HasSprites(enemyName, "Walk", walkSprites)) continue;

            string enemyOutputFolder = $"{outputPath}/{enemyName}";

            if (!AssetDatabase.IsValidFolder(enemyOutputFolder))
            {
                AssetDatabase.CreateFolder(outputPath, enemyName);
            }

            string newPrefabPath = $"{enemyOutputFolder}/{enemyName}.prefab";
            string newControllerPath = $"{enemyOutputFolder}/{enemyName}.controller";
            string newStatPath = $"{enemyOutputFolder}/{enemyName}.asset";

            DeleteIfExists(newPrefabPath);
            DeleteIfExists(newControllerPath);
            DeleteIfExists(newStatPath);

            AssetDatabase.CopyAsset(templatePrefabPath, newPrefabPath);
            AssetDatabase.CopyAsset(templateControllerPath, newControllerPath);
            AssetDatabase.CopyAsset(templateStatPath, newStatPath);

            AssetDatabase.ImportAsset(newPrefabPath);
            AssetDatabase.ImportAsset(newControllerPath);
            AssetDatabase.ImportAsset(newStatPath);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(newPrefabPath);

            prefabRoot.name = enemyName;

            Animator animator = prefabRoot.GetComponentInChildren<Animator>();
            SpriteRenderer spriteRenderer = prefabRoot.GetComponentInChildren<SpriteRenderer>();

            if (animator == null)
            {
                Debug.LogWarning($"{enemyName} 프리팹에 Animator가 없습니다. 스킵합니다.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                continue;
            }

            if (spriteRenderer == null)
            {
                Debug.LogWarning($"{enemyName} 프리팹에 SpriteRenderer가 없습니다. 스킵합니다.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                continue;
            }

            string spriteRendererPath = GetRelativePath(
                animator.transform,
                spriteRenderer.transform
            );

            AnimationClip idleClip = CreateSpriteAnimationClip(
                $"{enemyOutputFolder}/{enemyName}-{idleClipName}.anim",
                idleSprites,
                spriteRendererPath,
                frameRate,
                loopIdle
            );

            AnimationClip attackClip = CreateSpriteAnimationClip(
                $"{enemyOutputFolder}/{enemyName}-{attackClipName}.anim",
                attackSprites,
                spriteRendererPath,
                frameRate,
                loopAttack
            );

            AnimationClip hitClip = CreateSpriteAnimationClip(
                $"{enemyOutputFolder}/{enemyName}-{hitClipName}.anim",
                hitSprites,
                spriteRendererPath,
                frameRate,
                loopHit
            );

            AnimationClip dieClip = CreateSpriteAnimationClip(
                $"{enemyOutputFolder}/{enemyName}-{dieClipName}.anim",
                dieSprites,
                spriteRendererPath,
                frameRate,
                loopDie
            );

            AnimationClip walkClip = CreateSpriteAnimationClip(
                $"{enemyOutputFolder}/{enemyName}-{walkClipName}.anim",
                walkSprites,
                spriteRendererPath,
                frameRate,
                loopWalk
            );

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(newControllerPath);

            EnemyStatData statData =
                AssetDatabase.LoadAssetAtPath<EnemyStatData>(newStatPath);

            ReplaceControllerClips(
                controller,
                idleClip,
                attackClip,
                hitClip,
                dieClip,
                walkClip
            );

            animator.runtimeAnimatorController = controller;

            Enemy enemy = prefabRoot.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemy.statData = statData;
                EditorUtility.SetDirty(enemy);
            }
            else
            {
                Debug.LogWarning($"{enemyName} 프리팹에 Enemy 컴포넌트가 없습니다.");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, newPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(statData);

            createdCount++;

            Debug.Log($"{enemyName} 생성 완료");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"적 생성 완료: {createdCount}개");
    }

    private static AnimationClip CreateSpriteAnimationClip(
        string clipPath,
        List<Sprite> sprites,
        string spriteRendererPath,
        float frameRate,
        bool loop
    )
    {
        DeleteIfExists(clipPath);

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = spriteRendererPath,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[sprites.Count];

        float safeFrameRate = Mathf.Max(1f, frameRate);
        float frameTime = 1f / safeFrameRate;

        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes
        );

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.ImportAsset(clipPath);

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    }

    private static void ReplaceControllerClips(
        AnimatorController controller,
        AnimationClip idleClip,
        AnimationClip attackClip,
        AnimationClip hitClip,
        AnimationClip dieClip,
        AnimationClip walkClip
    )
    {
        if (controller == null)
            return;

        Dictionary<string, AnimationClip> clipMap = new Dictionary<string, AnimationClip>
        {
            { "Idle", idleClip },
            { "Attack", attackClip },
            { "Hit", hitClip },
            { "Hurt", hitClip },
            { "Damage", hitClip },
            { "Die", dieClip },
            { "Death", dieClip },
            { "Walk", walkClip },
            { "Move", walkClip },
            { "Run", walkClip }
        };

        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (layer == null || layer.stateMachine == null)
                continue;

            ReplaceStateMachineClips(layer.stateMachine, clipMap);
        }
    }

    private static void ReplaceStateMachineClips(
        AnimatorStateMachine stateMachine,
        Dictionary<string, AnimationClip> clipMap
    )
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            AnimatorState state = childState.state;

            if (state == null)
                continue;

            foreach (var pair in clipMap)
            {
                if (state.name.Contains(pair.Key))
                {
                    state.motion = pair.Value;
                    EditorUtility.SetDirty(state);
                    break;
                }
            }
        }

        foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines)
        {
            if (childMachine.stateMachine == null)
                continue;

            ReplaceStateMachineClips(childMachine.stateMachine, clipMap);
        }
    }

    private static List<Sprite> FindSprites(
        string folderPath,
        string prefix,
        bool logFrameCount
    )
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { folderPath }
        );

        List<Sprite> sprites = new List<Sprite>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            foreach (Object subAsset in subAssets)
            {
                Sprite sprite = subAsset as Sprite;

                if (sprite == null)
                    continue;

                if (sprite.name.StartsWith(prefix))
                {
                    if (!sprites.Contains(sprite))
                        sprites.Add(sprite);
                }
            }

            Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (singleSprite != null && singleSprite.name.StartsWith(prefix))
            {
                if (!sprites.Contains(singleSprite))
                    sprites.Add(singleSprite);
            }
        }

        sprites = sprites
            .OrderBy(sprite => sprite.name, new NaturalStringComparer())
            .ToList();

        if (logFrameCount)
        {
            Debug.Log($"{prefix} 프레임 수: {sprites.Count}");
        }

        return sprites;
    }

    private static bool HasSprites(string enemyName, string stateName, List<Sprite> sprites)
    {
        if (sprites != null && sprites.Count > 0)
            return true;

        Debug.LogWarning($"{enemyName} 스킵됨. {stateName} 스프라이트가 없습니다.");
        return false;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
            return "";

        List<string> paths = new List<string>();

        Transform current = target;

        while (current != null && current != root)
        {
            paths.Add(current.name);
            current = current.parent;
        }

        paths.Reverse();

        return string.Join("/", paths);
    }

    private static string GetAssetPath(DefaultAsset asset)
    {
        if (asset == null)
            return null;

        string path = AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(path))
            return null;

        if (!AssetDatabase.IsValidFolder(path))
            return null;

        return path;
    }

    private static string FindAssetPath<T>(string folderPath, string assetName) where T : Object
    {
        string typeName = typeof(T).Name;

        string[] guids = AssetDatabase.FindAssets(
            $"{assetName} t:{typeName}",
            new[] { folderPath }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
                continue;

            if (asset.name == assetName)
                return path;
        }

        return null;
    }

    private static void DeleteIfExists(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            string[] xParts = Regex.Split(x, "([0-9]+)");
            string[] yParts = Regex.Split(y, "([0-9]+)");

            int count = Mathf.Min(xParts.Length, yParts.Length);

            for (int i = 0; i < count; i++)
            {
                bool xIsNumber = int.TryParse(xParts[i], out int xNumber);
                bool yIsNumber = int.TryParse(yParts[i], out int yNumber);

                if (xIsNumber && yIsNumber)
                {
                    int numberCompare = xNumber.CompareTo(yNumber);

                    if (numberCompare != 0)
                        return numberCompare;
                }
                else
                {
                    int stringCompare = string.Compare(xParts[i], yParts[i]);

                    if (stringCompare != 0)
                        return stringCompare;
                }
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}
#endif