#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Copies an existing VFX prefab, builds a timed sprite animation and connects
/// either an AnimatorOverrideController or an independent AnimatorController.
/// Editor-only: put this file anywhere below an Assets/Editor directory.
/// </summary>
public sealed class SlicedSpriteVfxGeneratorWindow : EditorWindow
{
    private enum ControllerMode
    {
        OverrideSharedController,
        CreateIndependentController
    }

    private GameObject sourcePrefab;
    private Texture2D spriteSheet;
    private string effectName = "NewEffect";
    private string outputFolder = "Assets/GeneratedVFX";
    private ControllerMode controllerMode = ControllerMode.OverrideSharedController;
    private AnimatorController sharedController;
    private int overrideClipIndex;
    private int animatorIndex;
    private int rendererIndex;
    private bool allSprites = true;
    private int customFrameCount = 10;
    private float clipDuration = 0.5f;
    private bool reverseOrder;
    private bool loop;
    private Vector2 scroll;

    [MenuItem("Tools/Utility/Sliced Sprite VFX Generator")]
    private static void ShowWindow()
    {
        var window = GetWindow<SlicedSpriteVfxGeneratorWindow>("Sprite VFX Generator");
        window.minSize = new Vector2(445f, 485f);
        window.Show();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Sprite VFX 자동 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "원본 Prefab을 복사한 뒤 SpriteRenderer의 프레임 애니메이션과 " +
            "컨트롤러를 생성해 연결합니다. 원본 에셋은 수정하지 않습니다.", MessageType.Info);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("1. 원본 및 스프라이트", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("원본 Prefab", sourcePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            animatorIndex = 0;
            rendererIndex = 0;
            sharedController = null;
        }

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("슬라이스된 텍스처", spriteSheet, typeof(Texture2D), false);
        effectName = EditorGUILayout.TextField("생성할 이펙트 이름", effectName);

        Animator selectedAnimator;
        SpriteRenderer selectedRenderer;
        bool validComponents = DrawComponentPickers(out selectedAnimator, out selectedRenderer);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. 애니메이션", EditorStyles.boldLabel);
        allSprites = EditorGUILayout.ToggleLeft("슬라이스된 스프라이트 수 = 프레임 수", allSprites);
        using (new EditorGUI.DisabledScope(allSprites))
            customFrameCount = EditorGUILayout.IntField("직접 지정할 프레임 수", customFrameCount);
        clipDuration = EditorGUILayout.FloatField("총 재생 시간 (초)", clipDuration);
        reverseOrder = EditorGUILayout.Toggle("프레임 순서 반대로", reverseOrder);
        loop = EditorGUILayout.Toggle("반복 재생 (Loop)", loop);

        Sprite[] sprites = LoadSlicedSprites(spriteSheet);
        int frames = allSprites ? sprites.Length : customFrameCount;
        if (sprites.Length > 0 && frames > 0 && IsValidDuration(clipDuration))
        {
            float interval = clipDuration / frames;
            EditorGUILayout.HelpBox(
                string.Format("스프라이트 {0}장 → 애니메이션 {1}프레임 / {2:0.###}초 / 프레임당 {3:0.####}초\n" +
                              "첫 프레임 0초, 마지막 프레임 {4:0.####}초부터 재생 종료까지 유지",
                              sprites.Length, frames, clipDuration, interval, (frames - 1) * interval),
                MessageType.None);
            if (frames / clipDuration > 240f)
                EditorGUILayout.HelpBox("초당 프레임 수가 240을 초과합니다. 시간 키는 균등하게 배치하지만, 실제 화면에서는 일부 프레임이 건너뛰어질 수 있습니다.", MessageType.Warning);
        }
        else if (spriteSheet != null && sprites.Length == 0)
        {
            EditorGUILayout.HelpBox("Sprite Mode = Multiple로 설정해 슬라이스하고 Apply하세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Animator 컨트롤러", EditorStyles.boldLabel);
        controllerMode = (ControllerMode)EditorGUILayout.EnumPopup("생성 방식", controllerMode);
        AnimatorController baseController = null;
        AnimationClip[] originalClips = Array.Empty<AnimationClip>();
        if (controllerMode == ControllerMode.OverrideSharedController)
        {
            sharedController = (AnimatorController)EditorGUILayout.ObjectField(
                "공용 Controller (선택)", sharedController, typeof(AnimatorController), false);
            baseController = sharedController != null
                ? sharedController
                : FindBaseController(selectedAnimator != null ? selectedAnimator.runtimeAnimatorController : null);
            if (baseController != null)
            {
                originalClips = GetDistinctClips(baseController);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("사용할 기본 Controller", baseController, typeof(AnimatorController), false);
                if (originalClips.Length > 0)
                {
                    overrideClipIndex = Mathf.Clamp(overrideClipIndex, 0, originalClips.Length - 1);
                    string[] choices = originalClips.Select(c => c.name).ToArray();
                    overrideClipIndex = EditorGUILayout.Popup("교체할 원본 클립", overrideClipIndex, choices);
                }
                else
                    EditorGUILayout.HelpBox("기본 Controller에 교체 가능한 AnimationClip이 없습니다. 독립 Controller 생성 방식을 선택하세요.", MessageType.Warning);
            }
            else
                EditorGUILayout.HelpBox("공용 Controller를 지정하거나, 원본 Prefab의 Animator에 기본 Controller를 연결하세요.", MessageType.Warning);
            EditorGUILayout.HelpBox("기존 DefaultEffect.controller는 수정하지 않고 Override Controller 에셋을 새로 만듭니다.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("이 이펙트만 사용하는 AnimatorController와 재생 State를 새로 생성합니다.", MessageType.None);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("4. 저장 위치", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        outputFolder = EditorGUILayout.TextField("상위 폴더 (Assets/...)", outputFolder);
        if (GUILayout.Button("찾기", GUILayout.Width(54)))
            PickOutputFolder();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("결과물은 [상위 폴더]/[이펙트 이름]/ 에 각각 .prefab, .anim, " +
                                ".overrideController 또는 .controller 파일로 저장됩니다.", MessageType.None);

        string error = ValidateInput(sprites, frames, validComponents, baseController, originalClips);
        if (!string.IsNullOrEmpty(error))
            EditorGUILayout.HelpBox(error, MessageType.Warning);

        EditorGUILayout.Space(12);
        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(error)))
        {
            if (GUILayout.Button("프리팹 + 애니메이션 + 컨트롤러 한 번에 생성", GUILayout.Height(36)))
            {
                try
                {
                    CreateVfx(sprites, frames, selectedAnimator, selectedRenderer, baseController, originalClips);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorUtility.DisplayDialog("생성 실패", exception.Message + "\n자세한 내용은 Console에서 확인하세요.", "확인");
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool DrawComponentPickers(out Animator animator, out SpriteRenderer renderer)
    {
        animator = null;
        renderer = null;
        if (sourcePrefab == null)
            return false;

        Animator[] animators = sourcePrefab.GetComponentsInChildren<Animator>(true);
        if (animators.Length > 0)
        {
            animatorIndex = Mathf.Clamp(animatorIndex, 0, animators.Length - 1);
            string[] labels = animators.Select(a => ReadablePath(sourcePrefab.transform, a.transform)).ToArray();
            animatorIndex = EditorGUILayout.Popup("Animator 위치", animatorIndex, labels);
            animator = animators[animatorIndex];
        }
        else
        {
            EditorGUILayout.HelpBox("원본에 Animator가 없어 복제본의 루트에 Animator를 추가합니다.", MessageType.Info);
        }

        Transform animatorRoot = animator == null ? sourcePrefab.transform : animator.transform;
        SpriteRenderer[] renderers = sourcePrefab.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.transform == animatorRoot || r.transform.IsChildOf(animatorRoot))
            .ToArray();
        if (renderers.Length == 0)
        {
            EditorGUILayout.HelpBox("선택한 Animator 아래에 SpriteRenderer가 없습니다.", MessageType.Error);
            return false;
        }

        rendererIndex = Mathf.Clamp(rendererIndex, 0, renderers.Length - 1);
        string[] rendererLabels = renderers.Select(r => ReadablePath(sourcePrefab.transform, r.transform)).ToArray();
        rendererIndex = EditorGUILayout.Popup("애니메이션할 SpriteRenderer", rendererIndex, rendererLabels);
        renderer = renderers[rendererIndex];
        return true;
    }

    private string ValidateInput(Sprite[] sprites, int frames, bool validComponents,
                                 AnimatorController controller, AnimationClip[] originalClips)
    {
        if (sourcePrefab == null) return "복사할 Prefab을 지정하세요.";
        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            return "Project에 저장된 .prefab 에셋만 원본으로 지정할 수 있습니다.";
        if (!validComponents) return "Animator 아래에서 SpriteRenderer를 찾을 수 없습니다.";
        if (spriteSheet == null || sprites.Length == 0) return "슬라이스된 스프라이트가 포함된 텍스처를 지정하세요.";
        if (frames < 1 || frames > 4096) return "프레임 수는 1 ~ 4096 사이여야 합니다.";
        if (!IsValidDuration(clipDuration)) return "재생 시간은 0초보다 크고 유한한 값이어야 합니다.";
        if (clipDuration / frames < 0.000001f) return "총 길이가 프레임 수에 비해 너무 짧습니다.";
        if (string.IsNullOrWhiteSpace(effectName)) return "생성할 이펙트 이름을 입력하세요.";
        if (string.IsNullOrEmpty(SafeFileName(effectName))) return "파일명으로 사용할 수 없는 이름입니다.";
        if (!IsSafeAssetsFolderPath(outputFolder))
            return "저장 상위 폴더를 Assets 또는 Assets 하위 경로로 지정하세요.";
        if (controllerMode == ControllerMode.OverrideSharedController &&
            (controller == null || originalClips.Length == 0))
            return "Override 방식에는 AnimationClip이 들어 있는 기본 AnimatorController가 필요합니다.";
        return null;
    }

    private void CreateVfx(Sprite[] sprites, int frameCount,
                           Animator sourceAnimator, SpriteRenderer sourceRenderer,
                           AnimatorController baseController, AnimationClip[] originalClips)
    {
        string cleanName = SafeFileName(effectName);
        string parentFolder = outputFolder.Replace('\\', '/').TrimEnd('/');
        EnsureAssetsFolder(parentFolder);
        string folder = MakeUniqueSubfolder(parentFolder, cleanName);
        string clipPath = folder + "/" + cleanName + ".anim";
        string controllerPath = folder + "/" + cleanName +
            (controllerMode == ControllerMode.OverrideSharedController ? "_Override.overrideController" : "_Controller.controller");
        string prefabPath = folder + "/" + cleanName + ".prefab";
        List<string> created = new List<string>();
        bool succeeded = false;

        try
        {
            AnimationClip clip = CreateSpriteClip(sprites, frameCount, sourceAnimator, sourceRenderer);
            AssetDatabase.CreateAsset(clip, clipPath);
            created.Add(clipPath);

            RuntimeAnimatorController generatedController;
            if (controllerMode == ControllerMode.OverrideSharedController)
            {
                AnimationClip originalClip = originalClips[overrideClipIndex];
                AnimatorOverrideController overrideController = CreateOverride(
                    baseController,
                    sourceAnimator != null ? sourceAnimator.runtimeAnimatorController as AnimatorOverrideController : null,
                    originalClip, clip);
                overrideController.name = cleanName + "_Override";
                AssetDatabase.CreateAsset(overrideController, controllerPath);
                created.Add(controllerPath);
                generatedController = overrideController;
            }
            else
            {
                AnimatorController newController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                if (newController == null) throw new InvalidOperationException("AnimatorController 생성에 실패했습니다.");
                created.Add(controllerPath);
                generatedController = newController;
                AnimatorStateMachine stateMachine = newController.layers[0].stateMachine;
                AnimatorState state = stateMachine.AddState("Play");
                state.motion = clip;
                stateMachine.defaultState = state;
                EditorUtility.SetDirty(newController);
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
            if (!AssetDatabase.CopyAsset(sourcePath, prefabPath))
                throw new InvalidOperationException("원본 Prefab 에셋 복사에 실패했습니다: " + sourcePath);
            created.Add(prefabPath);

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                Animator targetAnimator;
                if (sourceAnimator == null)
                {
                    targetAnimator = prefabRoot.GetComponent<Animator>();
                    if (targetAnimator == null)
                        targetAnimator = prefabRoot.AddComponent<Animator>();
                }
                else
                {
                    Animator[] animators = prefabRoot.GetComponentsInChildren<Animator>(true);
                    if (animatorIndex >= animators.Length)
                        throw new InvalidOperationException("복제된 Prefab의 Animator 구성이 변경됐습니다.");
                    targetAnimator = animators[animatorIndex];
                }

                SpriteRenderer[] renderers = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => r.transform == targetAnimator.transform ||
                                r.transform.IsChildOf(targetAnimator.transform)).ToArray();
                if (rendererIndex >= renderers.Length)
                    throw new InvalidOperationException("복제된 Prefab의 SpriteRenderer 구성이 변경됐습니다.");

                Sprite[] playback = reverseOrder ? sprites.Reverse().ToArray() : sprites;
                renderers[rendererIndex].sprite = playback[0];
                targetAnimator.runtimeAnimatorController = generatedController;

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                if (saved == null)
                    throw new InvalidOperationException("복제 Prefab의 저장에 실패했습니다.");
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            succeeded = true;
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = result;
            EditorGUIUtility.PingObject(result);
            Debug.Log(string.Format("[Sprite VFX Generator] 생성 완료: {0}\nClip: {1}\nController: {2}", prefabPath, clipPath, controllerPath), result);
            EditorUtility.DisplayDialog("생성 완료", "Prefab, AnimationClip, Controller를 생성했어요.\n" + folder, "확인");
        }
        finally
        {
            if (!succeeded)
            {
                for (int i = created.Count - 1; i >= 0; --i)
                    AssetDatabase.DeleteAsset(created[i]);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private AnimationClip CreateSpriteClip(Sprite[] orderedSprites, int frameCount,
                                           Animator sourceAnimator, SpriteRenderer sourceRenderer)
    {
        Sprite[] playback = reverseOrder ? orderedSprites.Reverse().ToArray() : orderedSprites;
        Transform animatorRoot = sourceAnimator == null ? sourcePrefab.transform : sourceAnimator.transform;
        string rendererPath = RelativePath(animatorRoot, sourceRenderer.transform);

        AnimationClip clip = new AnimationClip();
        clip.name = SafeFileName(effectName);
        clip.frameRate = Mathf.Clamp(Mathf.CeilToInt(frameCount / clipDuration), 1, 240);

        // Exactly N frames spread over [0, duration), keeping the final sprite
        // through t=duration so clip.length and the VFX pool lifetime match.
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount + 1];
        for (int i = 0; i < frameCount; i++)
        {
            int spriteIndex = frameCount == 1
                ? 0
                : Mathf.RoundToInt(i * (playback.Length - 1f) / (frameCount - 1f));
            keys[i] = new ObjectReferenceKeyframe
            {
                time = (float)((double)i * clipDuration / frameCount),
                value = playback[spriteIndex]
            };
        }
        keys[frameCount] = new ObjectReferenceKeyframe
        {
            time = clipDuration,
            value = keys[frameCount - 1].value
        };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve(rendererPath, typeof(SpriteRenderer), "m_Sprite"), keys);

        // Inspector 'Loop Time' is stored separately from wrapMode.
        SerializedObject serialized = new SerializedObject(clip);
        SerializedProperty loopTime = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = loop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return clip;
    }

    private static AnimatorOverrideController CreateOverride(AnimatorController baseController,
        AnimatorOverrideController existingOverride, AnimationClip original, AnimationClip generated)
    {
        var result = new AnimatorOverrideController(baseController);
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        result.GetOverrides(pairs);

        // Preserve unrelated overrides if the source prefab used this same base.
        Dictionary<AnimationClip, AnimationClip> inherited = null;
        if (existingOverride != null && FindBaseController(existingOverride) == baseController)
        {
            var oldPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            existingOverride.GetOverrides(oldPairs);
            inherited = new Dictionary<AnimationClip, AnimationClip>();
            foreach (KeyValuePair<AnimationClip, AnimationClip> pair in oldPairs)
                if (pair.Key != null && pair.Value != null) inherited[pair.Key] = pair.Value;
        }

        bool replaced = false;
        for (int i = 0; i < pairs.Count; i++)
        {
            AnimationClip key = pairs[i].Key;
            AnimationClip value = pairs[i].Value;
            if (inherited != null && inherited.TryGetValue(key, out AnimationClip saved))
                value = saved;
            if (key == original)
            {
                value = generated;
                replaced = true;
            }
            pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, value);
        }
        if (!replaced)
            throw new InvalidOperationException("공용 Controller에서 선택한 원본 클립을 찾을 수 없습니다.");
        result.ApplyOverrides(pairs);
        return result;
    }

    private static AnimatorController FindBaseController(RuntimeAnimatorController source)
    {
        for (int i = 0; i < 8 && source != null; i++)
        {
            if (source is AnimatorController controller) return controller;
            if (source is AnimatorOverrideController overrideController)
            {
                source = overrideController.runtimeAnimatorController;
                continue;
            }
            break;
        }
        return null;
    }

    private static AnimationClip[] GetDistinctClips(AnimatorController controller)
    {
        return controller.animationClips.Where(c => c != null).Distinct().ToArray();
    }

    private static Sprite[] LoadSlicedSprites(Texture2D texture)
    {
        if (texture == null) return Array.Empty<Sprite>();
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path)) return Array.Empty<Sprite>();
        Sprite[] found = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        // Across the sheet: top row -> bottom row, each row left -> right.
        Array.Sort(found, (a, b) =>
        {
            int byTop = b.rect.yMin.CompareTo(a.rect.yMin);
            if (byTop != 0) return byTop;
            int byLeft = a.rect.xMin.CompareTo(b.rect.xMin);
            return byLeft != 0 ? byLeft : string.CompareOrdinal(a.name, b.name);
        });
        return found;
    }

    private static string MakeUniqueSubfolder(string parent, string name)
    {
        string candidate = name;
        int suffix = 2;
        while (AssetDatabase.IsValidFolder(parent + "/" + candidate) ||
               AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(parent + "/" + candidate) != null)
            candidate = name + "_" + suffix++;
        string guid = AssetDatabase.CreateFolder(parent, candidate);
        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException("출력 폴더를 만들지 못했습니다: " + parent + "/" + candidate);
        return parent + "/" + candidate;
    }

    private void PickOutputFolder()
    {
        string absolute = EditorUtility.OpenFolderPanel("생성 위치 선택", Application.dataPath, "");
        if (string.IsNullOrEmpty(absolute)) return;
        string assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');
        absolute = absolute.Replace('\\', '/').TrimEnd('/');
        if (string.Equals(absolute, assets, StringComparison.OrdinalIgnoreCase))
            outputFolder = "Assets";
        else if (absolute.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
            outputFolder = "Assets" + absolute.Substring(assets.Length);
        else
            EditorUtility.DisplayDialog("폴더 선택 오류", "Unity 프로젝트의 Assets 폴더 내부를 선택하세요.", "확인");
    }

    private static bool IsSafeAssetsFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        path = path.Replace('\\', '/').TrimEnd('/');
        if (path == "Assets") return true;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return false;
        string[] segments = path.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            string part = segments[i];
            if (string.IsNullOrWhiteSpace(part) || part == "." || part == ".." ||
                SafeFileName(part) != part)
                return false;
        }
        return true;
    }

    private static void EnsureAssetsFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        if (!IsSafeAssetsFolderPath(path))
            throw new ArgumentException("Assets 폴더 아래의 올바른 경로만 지정할 수 있습니다: " + path);
        int separator = path.LastIndexOf('/');
        if (separator <= 0)
            throw new ArgumentException("Assets 폴더 경로가 올바르지 않습니다: " + path);
        string parent = path.Substring(0, separator);
        EnsureAssetsFolder(parent);
        string guid = AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException("상위 출력 폴더 생성에 실패했습니다: " + path);
    }

    private static string SafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        char[] forbidden = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).Distinct().ToArray();
        string cleaned = new string(name.Trim().Select(c => forbidden.Contains(c) || char.IsControl(c) ? '_' : c).ToArray());
        return cleaned.Trim(' ', '.');
    }

    private static bool IsValidDuration(float time)
    {
        return time > 0f && !float.IsNaN(time) && !float.IsInfinity(time);
    }

    private static string ReadablePath(Transform root, Transform target)
    {
        string path = RelativePath(root, target);
        return string.IsNullOrEmpty(path) ? "(Root)" : path;
    }

    private static string RelativePath(Transform ancestor, Transform target)
    {
        if (target == ancestor) return "";
        var parts = new List<string>();
        Transform current = target;
        while (current != null && current != ancestor)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        if (current != ancestor)
            throw new ArgumentException("SpriteRenderer must be inside the selected Animator hierarchy.");
        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }
}
#endif
