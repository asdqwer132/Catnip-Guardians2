using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬/Inspector에서 실제로 수정한 계층만 추적합니다. 전역 오브젝트 검색은 하지 않습니다.
[InitializeOnLoad]
public static class SkillTreeLayoutRecorder
{
    private static readonly HashSet<SkillTreeNodePlacer> known = new HashSet<SkillTreeNodePlacer>();
    private static bool recording;
    static SkillTreeLayoutRecorder()
    {
        Undo.postprocessModifications += OnModified;
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorApplication.update += RememberSelection;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        PrefabStage.prefabSaving += OnPrefabSaving;
    }
    public static void RememberSelection()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || recording) return;
        known.RemoveWhere(placer => placer == null);
        foreach (var selected in Selection.transforms)
        {
            var placer = selected.GetComponentInParent<SkillTreeNodePlacer>(true);
            Remember(placer);
        }
    }
    private static void Remember(SkillTreeNodePlacer placer)
    {
        if (placer == null || !placer.autoRememberPositions || placer.IsApplyingLayout) return;
        known.Add(placer); recording = true;
        try { placer.CaptureNodePositions(); }
        finally { recording = false; }
    }
    private static UndoPropertyModification[] OnModified(UndoPropertyModification[] modifications)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || recording) return modifications;
        var placers = new HashSet<SkillTreeNodePlacer>();
        foreach (var modification in modifications)
            if (modification.currentValue.target is RectTransform rect)
            {
                var placer = rect.GetComponentInParent<SkillTreeNodePlacer>(true);
                if (placer != null) placers.Add(placer);
            }
        foreach (var placer in placers) Remember(placer);
        return modifications;
    }
    private static void OnUndoRedo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        recording = true;
        try
        {
            known.RemoveWhere(placer => placer == null);
            foreach (var placer in known) if (placer.autoRememberPositions) placer.ApplySavedLayout(false);
        }
        finally { recording = false; }
    }
    private static void OnSceneSaving(Scene scene, string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var placer in root.GetComponentsInChildren<SkillTreeNodePlacer>(true)) Remember(placer);
    }
    private static void OnPrefabSaving(GameObject root)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var placer in root.GetComponentsInChildren<SkillTreeNodePlacer>(true)) Remember(placer);
    }
}
