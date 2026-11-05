#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Catnip.EditorTools
{
    public enum UILineEndpoint { Center, Pivot, FacingEdge }
    public enum UILinePrefabAxis { Horizontal, Vertical }

    [Serializable]
    public sealed class UILinePrefabSettings
    {
        public UILineEndpoint endpoint = UILineEndpoint.Center;
        public UILinePrefabAxis prefabAxis = UILinePrefabAxis.Horizontal;
        public Vector2 startOffset, endOffset;
        [Min(0f)] public float startGap, endGap;
        public bool overrideThickness;
        [Min(0.01f)] public float thickness = 10f;
        public bool sendToBack = true;
    }

    public sealed class UILinePrefabWindow : EditorWindow
    {
        [SerializeField] private RectTransform from, to, lineParent;
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private UILinePrefabSettings settings = new UILinePrefabSettings();
        [SerializeField] private RectTransform createdLine;
        private Vector2 scroll;
        private string result;

        [MenuItem("Tools/UI/UI Line Prefab")]
        private static void Open()
        {
            var window = GetWindow<UILinePrefabWindow>("UI Line Prefab");
            window.minSize = new Vector2(360f, 440f);
            window.Show();
        }

        private void OnGUI()
        {
            if (settings == null) settings = new UILinePrefabSettings();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            from = (RectTransform)EditorGUILayout.ObjectField("From UI", from, typeof(RectTransform), true);
            to = (RectTransform)EditorGUILayout.ObjectField("To UI", to, typeof(RectTransform), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selected UI Pair"))
                {
                    var selected = Selection.transforms;
                    if (selected.Length == 2 && selected[0] is RectTransform first && selected[1] is RectTransform second)
                    { from = first; to = second; }
                    else result = "RectTransform이 있는 UI 오브젝트 두 개를 선택하세요.";
                }
                if (GUILayout.Button("Swap")) { var old = from; from = to; to = old; }
            }
            linePrefab = (GameObject)EditorGUILayout.ObjectField("Line Prefab", linePrefab, typeof(GameObject), false);
            lineParent = (RectTransform)EditorGUILayout.ObjectField("Line Parent", lineParent, typeof(RectTransform), true);

            EditorGUILayout.Space();
            settings.endpoint = (UILineEndpoint)EditorGUILayout.EnumPopup("Connection Point", settings.endpoint);
            settings.prefabAxis = (UILinePrefabAxis)EditorGUILayout.EnumPopup("Prefab Length Axis", settings.prefabAxis);
            settings.startOffset = EditorGUILayout.Vector2Field("Start Offset", settings.startOffset);
            settings.endOffset = EditorGUILayout.Vector2Field("End Offset", settings.endOffset);
            settings.startGap = Mathf.Max(0f, EditorGUILayout.FloatField("Start Gap", settings.startGap));
            settings.endGap = Mathf.Max(0f, EditorGUILayout.FloatField("End Gap", settings.endGap));
            settings.overrideThickness = EditorGUILayout.Toggle("Override Thickness", settings.overrideThickness);
            if (settings.overrideThickness)
                settings.thickness = Mathf.Max(0.01f, EditorGUILayout.FloatField("Thickness", settings.thickness));
            settings.sendToBack = EditorGUILayout.Toggle("Send To Back", settings.sendToBack);
            if (EditorGUI.EndChangeCheck()) result = null;

            EditorGUILayout.Space();
            bool valid = UILinePrefabUtility.TryGetEndpoints(from, to, lineParent, linePrefab, settings, out _, out _, out string error);
            if (!valid) EditorGUILayout.HelpBox(error, MessageType.Info);
            using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Create Line"))
                    Run(() => createdLine = UILinePrefabUtility.Create(from, to, lineParent, linePrefab, settings));
                createdLine = (RectTransform)EditorGUILayout.ObjectField("Created Line", createdLine, typeof(RectTransform), true);
                using (new EditorGUI.DisabledScope(createdLine == null))
                    if (GUILayout.Button("Update Line"))
                        Run(() => UILinePrefabUtility.Update(createdLine, from, to, lineParent, linePrefab, settings));
            }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void Run(Action action)
        {
            try { Canvas.ForceUpdateCanvases(); action(); result = "라인 배치 완료. Ctrl/Cmd+Z로 되돌릴 수 있습니다."; }
            catch (Exception exception) { result = exception.Message; Debug.LogException(exception); }
        }
    }

    public static class UILinePrefabUtility
    {
        public static bool TryGetEndpoints(RectTransform from, RectTransform to, RectTransform parent,
            GameObject prefab, UILinePrefabSettings settings, out Vector2 start, out Vector2 end, out string error)
        {
            start = end = default;
            error = null;
            if (!Editable(from) || !Editable(to) || !Editable(parent))
                return Fail("From UI, To UI, Line Parent에 씬 또는 Prefab Mode의 UI를 연결하세요.", out error);
            if (from == to) return Fail("서로 다른 UI 두 개를 연결하세요.", out error);
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas == null || from.GetComponentInParent<Canvas>()?.rootCanvas != canvas.rootCanvas ||
                to.GetComponentInParent<Canvas>()?.rootCanvas != canvas.rootCanvas ||
                from.gameObject.scene != parent.gameObject.scene || to.gameObject.scene != parent.gameObject.scene)
                return Fail("두 UI와 Line Parent를 같은 씬의 같은 루트 Canvas 안에서 지정하세요.", out error);
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.transform.parent != null ||
                prefab.GetComponent<RectTransform>() == null)
                return Fail("루트에 RectTransform이 있는 라인 프리팹을 연결하세요.", out error);
            if (settings == null || !Finite(settings.startOffset) || !Finite(settings.endOffset) ||
                !Finite(settings.startGap) || !Finite(settings.endGap) ||
                (settings.overrideThickness && (!Finite(settings.thickness) || settings.thickness <= 0f)))
                return Fail("라인 간격, 오프셋, 두께에 유효한 값을 입력하세요.", out error);
            var prefabRect = prefab.GetComponent<RectTransform>();
            float thickness = settings.overrideThickness ? settings.thickness :
                (settings.prefabAxis == UILinePrefabAxis.Horizontal ? prefabRect.rect.height : prefabRect.rect.width);
            if (!Finite(thickness) || thickness <= 0f)
                return Fail("프리팹 두께가 0 이하입니다. Override Thickness로 두께를 지정하세요.", out error);

            Vector3 scale = parent.lossyScale;
            if (Mathf.Abs(scale.x) < 0.00001f || Mathf.Abs(scale.y) < 0.00001f)
                return Fail("Line Parent의 가로·세로 Scale은 0일 수 없습니다.", out error);
            start = Point(from, to, parent, settings.endpoint) + settings.startOffset;
            end = Point(to, from, parent, settings.endpoint) + settings.endOffset;
            Vector2 direction = end - start;
            float length = direction.magnitude;
            float startGap = Mathf.Max(0f, settings.startGap), endGap = Mathf.Max(0f, settings.endGap);
            if (!Finite(start) || !Finite(end) || !Finite(length) || length <= startGap + endGap + 0.001f)
                return Fail("연결점이 겹치거나 간격보다 가깝습니다. 연결점과 Gap을 조절하세요.", out error);
            start += direction / length * startGap;
            end -= direction / length * endGap;
            return true;
        }

        public static RectTransform Create(RectTransform from, RectTransform to, RectTransform parent,
            GameObject prefab, UILinePrefabSettings settings)
        {
            Validate(from, to, parent, prefab, settings, out _, out _);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create UI Line");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                Undo.RegisterCreatedObjectUndo(instance, "Create UI Line");
                Undo.RecordObject(instance, "Name UI Line");
                instance.name = $"{prefab.name} ({from.name} - {to.name})";
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                var rect = instance.GetComponent<RectTransform>();
                Apply(rect, from, to, parent, prefab, settings);
                Undo.CollapseUndoOperations(group);
                return rect;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            finally { Undo.IncrementCurrentGroup(); }
        }

        public static void Update(RectTransform line, RectTransform from, RectTransform to, RectTransform parent,
            GameObject prefab, UILinePrefabSettings settings)
        {
            Validate(from, to, parent, prefab, settings, out _, out _);
            if (!Editable(line) || line.parent != parent || line == from || line == to ||
                from.IsChildOf(line) || to.IsChildOf(line))
                throw new ArgumentException("Created Line에 Line Parent 바로 아래의 라인 오브젝트를 연결하세요.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update UI Line");
            try { Apply(line, from, to, parent, prefab, settings); Undo.CollapseUndoOperations(group); }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            finally { Undo.IncrementCurrentGroup(); }
        }

        private static void Validate(RectTransform from, RectTransform to, RectTransform parent,
            GameObject prefab, UILinePrefabSettings settings, out Vector2 start, out Vector2 end)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 배치하세요.");
            if (!TryGetEndpoints(from, to, parent, prefab, settings, out start, out end, out string error))
                throw new ArgumentException(error);
        }

        private static void Apply(RectTransform line, RectTransform from, RectTransform to, RectTransform parent,
            GameObject prefab, UILinePrefabSettings settings)
        {
            Undo.RecordObject(line, "Place UI Line");
            // Exclude the line before sizing it. Clearing a layout group's old
            // driven properties afterwards would otherwise restore the prefab pose.
            var layout = line.GetComponent<LayoutElement>();
            if (layout == null) layout = Undo.AddComponent<LayoutElement>(line.gameObject);
            Undo.RecordObject(layout, "Ignore Line In Layout");
            layout.ignoreLayout = true;
            layout.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(layout);
            if (parent.TryGetComponent<LayoutGroup>(out var parentLayout) && parentLayout.isActiveAndEnabled)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            Validate(from, to, parent, prefab, settings, out var start, out var end);
            // Adding a component flushes pending Undo.RecordObject snapshots.
            Undo.RecordObject(line, "Place UI Line");

            bool horizontal = settings.prefabAxis == UILinePrefabAxis.Horizontal;
            var prefabRect = prefab.GetComponent<RectTransform>();
            float thickness = settings.overrideThickness ? settings.thickness :
                (horizontal ? prefabRect.rect.height : prefabRect.rect.width);
            Vector2 delta = end - start;
            line.anchorMin = line.anchorMax = parent.pivot;
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition3D = (start + end) * 0.5f;
            line.localScale = Vector3.one;
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - (horizontal ? 0f : 90f));
            line.sizeDelta = horizontal ? new Vector2(delta.magnitude, thickness) : new Vector2(thickness, delta.magnitude);
            if (settings.sendToBack && line.GetSiblingIndex() != 0)
                Undo.SetSiblingIndex(line, 0, "Send UI Line To Back");
            PrefabUtility.RecordPrefabInstancePropertyModifications(line);

            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
        }

        private static Vector2 Point(RectTransform source, RectTransform other, RectTransform parent, UILineEndpoint mode)
        {
            Vector3 local = mode == UILineEndpoint.Pivot ? Vector3.zero : (Vector3)source.rect.center;
            if (mode == UILineEndpoint.FacingEdge)
            {
                Vector3 target = source.InverseTransformPoint(other.TransformPoint(other.rect.center));
                Vector2 direction = target - local;
                float x = Mathf.Abs(direction.x) > 0.00001f ? source.rect.width * 0.5f / Mathf.Abs(direction.x) : float.PositiveInfinity;
                float y = Mathf.Abs(direction.y) > 0.00001f ? source.rect.height * 0.5f / Mathf.Abs(direction.y) : float.PositiveInfinity;
                float amount = Mathf.Min(x, y);
                if (Finite(amount)) local += (Vector3)(direction * Mathf.Clamp01(amount));
            }
            return parent.InverseTransformPoint(source.TransformPoint(local));
        }

        private static bool Editable(RectTransform rect) => rect != null && !EditorUtility.IsPersistent(rect) &&
            rect.gameObject.scene.IsValid() && (rect.gameObject.hideFlags & HideFlags.NotEditable) == 0;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static bool Fail(string message, out string error) { error = message; return false; }
    }
}
#endif
