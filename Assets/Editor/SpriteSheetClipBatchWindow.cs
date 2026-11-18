using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum SpriteClipTimingMode { FrameInterval, TotalDuration }

// 시트 하나당 클립 하나. 모든 슬라이스를 사용하며 프리팹/컨트롤러는 생성하지 않습니다.
public sealed class SpriteSheetClipBatchWindow : EditorWindow
{
    [SerializeField] private string rootFolder = "Assets/Resources/Effects";
    [SerializeField] private string outputFolder = "Assets/Animation/VisualEffect/Generated";
    [SerializeField] private SpriteClipTimingMode timingMode;
    [SerializeField] private float seconds = .1f;
    [SerializeField] private bool loop;
    [SerializeField] private bool overwriteExisting;
    private string result;

    [MenuItem("Tools/Utility/Animation/Sprite Sheet Clips")]
    private static void Open() => GetWindow<SpriteSheetClipBatchWindow>("Sprite Sheet Clips");

    private void OnGUI()
    {
        rootFolder = DrawFolder("Sprite Sheet Root", rootFolder);
        outputFolder = DrawFolder("Output Folder", outputFolder);
        timingMode = (SpriteClipTimingMode)EditorGUILayout.EnumPopup("Timing Mode", timingMode);
        seconds = EditorGUILayout.FloatField(timingMode == SpriteClipTimingMode.FrameInterval ? "Seconds Per Frame" : "Total Seconds", seconds);
        loop = EditorGUILayout.Toggle("Loop", loop);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Clips", overwriteExisting);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("루트 폴더의 하위 폴더까지 검색합니다. 시트별 모든 Sprite를 이름의 숫자 순서로 사용하며, 출력 폴더에도 하위 폴더 구조를 유지합니다.", MessageType.None);
        bool valid = AssetDatabase.IsValidFolder(rootFolder) && IsAssetsPath(outputFolder) && ValidSeconds(seconds);
        using (new EditorGUI.DisabledScope(!valid))
            if (GUILayout.Button("Generate Animation Clips")) Generate();
        if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.Info);
    }

    private static string DrawFolder(string label, string path)
    {
        path = EditorGUILayout.TextField(label, path).Replace('\\','/').TrimEnd('/');
        var selected = (DefaultAsset)EditorGUILayout.ObjectField("", AssetDatabase.LoadAssetAtPath<DefaultAsset>(path), typeof(DefaultAsset), false);
        string selectedPath = AssetDatabase.GetAssetPath(selected);
        return selected != null && AssetDatabase.IsValidFolder(selectedPath) ? selectedPath : path;
    }
    private static bool IsAssetsPath(string path) => !string.IsNullOrWhiteSpace(path) &&
        (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)) &&
        !path.Split('/').Any(p => p == ".." || p == "." || p.Length == 0) && path.IndexOfAny(new[]{':','*','?','"','<','>','|'}) < 0;
    private static bool ValidSeconds(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    private void Generate()
    {
        int written = 0, skipped = 0, failed = 0;
        var sheets = AssetDatabase.FindAssets("t:Texture2D", new[]{rootFolder})
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        try
        {
            foreach (var path in sheets)
            {
                EditorUtility.DisplayProgressBar("Sprite Sheet Clips",path,(float)(written+skipped+failed)/Mathf.Max(1,sheets.Length));
                try
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                    if (sprites.Length == 0) { skipped++; continue; }
                    string relative = path.Substring(rootFolder.Length).TrimStart('/');
                    string destination = outputFolder + "/" + Path.ChangeExtension(relative,"anim").Replace('\\','/');
                    var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination);
                    if (existing != null && (!overwriteExisting || !(existing is AnimationClip))) { skipped++; continue; }
                    var clip = BuildClip(sprites, timingMode, seconds, loop);
                    clip.name = Path.GetFileNameWithoutExtension(path);
                    try
                    {
                        EnsureFolder(Path.GetDirectoryName(destination).Replace('\\','/'));
                        if (existing is AnimationClip current)
                        {
                            Undo.RecordObject(current,"Update sprite clip");
                            EditorUtility.CopySerialized(clip,current);
                            EditorUtility.SetDirty(current);
                        }
                        else { AssetDatabase.CreateAsset(clip,destination); clip = null; }
                        written++;
                    }
                    finally { if (clip != null) DestroyImmediate(clip); }
                }
                catch (Exception error) { failed++; Debug.LogError($"[Sprite Sheet Clips] {path}: {error.Message}"); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); AssetDatabase.SaveAssets(); }
        result = $"생성·갱신 {written}개 / 건너뜀 {skipped}개 / 실패 {failed}개";
        Debug.Log("[Sprite Sheet Clips] " + result);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\','/');
        EnsureFolder(parent);
        string guid = AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
        if (string.IsNullOrEmpty(guid)) throw new IOException("폴더 생성 실패: " + folder);
    }

    public static AnimationClip BuildClip(Sprite[] sprites, SpriteClipTimingMode mode, float seconds, bool loop)
    {
        if (sprites == null || sprites.Length == 0 || sprites.Any(s=>s==null)) throw new ArgumentException("분할된 Sprite가 필요합니다.");
        if (!ValidSeconds(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        var frames = sprites.OrderBy(s=>s.name, System.Collections.Generic.Comparer<string>.Create(EditorUtility.NaturalCompare)).ToArray();
        float duration = mode == SpriteClipTimingMode.FrameInterval ? seconds * frames.Length : seconds;
        if (!ValidSeconds(duration)) throw new ArgumentOutOfRangeException(nameof(seconds));
        float interval = duration / frames.Length;
        if (!ValidSeconds(interval)) throw new ArgumentOutOfRangeException(nameof(seconds));
        float frameRate = 1f / interval;
        if (!ValidSeconds(frameRate)) throw new ArgumentOutOfRangeException(nameof(seconds));
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = (float)((double)i * duration / frames.Length), value = frames[i] };
        // Unity의 Sprite 곡선은 마지막 키 뒤에도 1/frameRate초를 유지합니다.
        // 종료 지점에 중복 키를 넣으면 그만큼 클립 길이가 추가되므로 N개만 기록합니다.
        var clip = new AnimationClip { frameRate = frameRate, wrapMode = loop ? WrapMode.Loop : WrapMode.Once };
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        return clip;
    }
}
