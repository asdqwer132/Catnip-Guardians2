using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class SceneLanguagePreview
{
    private sealed class Preview
    {
        public string Original;
        public string Applied;
    }

    private static readonly Dictionary<Component, Preview> previews = new Dictionary<Component, Preview>();

    static SceneLanguagePreview()
    {
        ObjectChangeEvents.changesPublished += OnObjectsChanged;
        EditorApplication.projectChanged += QueueRefresh;
        EditorSceneManager.sceneOpened += (_, _) => QueueRefresh();
        EditorSceneManager.sceneSaving += (_, _) => Restore();
        EditorSceneManager.sceneSaved += _ => QueueRefresh();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Restore;
        EditorApplication.quitting += Restore;
        QueueRefresh();
    }

    private static void OnObjectsChanged(ref ObjectChangeEventStream stream) => QueueRefresh();

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode) Restore();
        if (change == PlayModeStateChange.EnteredEditMode) QueueRefresh();
    }

    private static void QueueRefresh()
    {
        EditorApplication.delayCall -= Refresh;
        EditorApplication.delayCall += Refresh;
    }

    private static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var visible = new HashSet<Component>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;

            var roots = scene.GetRootGameObjects();
            LanguageManager manager = null;
            SceneLanguageMediator mediator = null;
            foreach (var root in roots)
            {
                if (manager == null) manager = root.GetComponentInChildren<LanguageManager>(true);
                if (mediator == null) mediator = root.GetComponentInChildren<SceneLanguageMediator>(true);
            }
            if (manager == null || mediator == null || mediator.translations == null) continue;

            foreach (var root in roots)
            foreach (var marker in root.GetComponentsInChildren<LocalizedText>(true))
            {
                if (!marker.enabled || marker.source != LocalizedTextSource.TranslationKey) continue;
                var target = ResolveTarget(marker);
                if (target == null || !visible.Add(target)) continue;

                string current = Read(target);
                if (!previews.TryGetValue(target, out var preview))
                    previews.Add(target, preview = new Preview { Original = current });
                else if (current != preview.Applied)
                    preview.Original = current;

                string key = string.IsNullOrEmpty(marker.key) ? preview.Original : marker.key;
                if (!mediator.translations.TryGetText(key, manager.selectedLan, out var text) &&
                    !mediator.translations.TryGetText(key, language.english, out text))
                    text = preview.Original;
                preview.Applied = text;
                if (current != text) Write(target, text);
            }
        }

        foreach (var target in new List<Component>(previews.Keys))
        {
            if (target != null && visible.Contains(target)) continue;
            RestoreTarget(target, previews[target]);
            previews.Remove(target);
        }
        SceneView.RepaintAll();
    }

    private static Component ResolveTarget(LocalizedText marker)
    {
        if (marker.tmpText != null) return marker.tmpText;
        if (marker.legacyText != null) return marker.legacyText;
        var tmp = marker.GetComponent<TMP_Text>();
        return tmp != null ? (Component)tmp : marker.GetComponent<Text>();
    }

    private static string Read(Component target) => target is TMP_Text tmp ? tmp.text : ((Text)target).text;

    private static void Write(Component target, string text)
    {
        if (target is TMP_Text tmp) tmp.text = text;
        else ((Text)target).text = text;
    }

    private static void RestoreTarget(Component target, Preview preview)
    {
        if (target != null && Read(target) == preview.Applied && preview.Original != preview.Applied)
            Write(target, preview.Original);
    }

    private static void Restore()
    {
        foreach (var pair in previews) RestoreTarget(pair.Key, pair.Value);
        previews.Clear();
    }
}
