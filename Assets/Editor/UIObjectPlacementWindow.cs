#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Catnip.EditorTools
{
    public sealed class UIObjectPlacementWindow : EditorWindow
    {
        [SerializeField] private RectTransform referenceUI;
        [SerializeField] private Camera uiCamera, worldCamera;
        [SerializeField] private List<Transform> targets = new List<Transform>();
        [SerializeField] private UIObjectPlacementSettings settings = new UIObjectPlacementSettings();
        [SerializeField] private bool preview = true;
        private SerializedObject windowObject;
        private Vector2 scroll;
        private string result;

        [MenuItem("Catnip/Tools/UI Object Placement")]
        private static void Open()
        {
            var window = GetWindow<UIObjectPlacementWindow>("UI 기준 배치");
            window.minSize = new Vector2(390f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            if (settings == null) settings = new UIObjectPlacementSettings();
            windowObject = new SerializedObject(this);
            SceneView.duringSceneGui += DrawPreview;
        }
        private void OnDisable() => SceneView.duringSceneGui -= DrawPreview;
        private void OnSelectionChange() => Repaint();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.HelpBox("직접 만든 UI를 기준으로 기존 씬 오브젝트의 위치를 배치합니다. " +
                "미리보기는 씬을 수정하지 않으며 [배치 적용] 후 Ctrl/Cmd+Z로 한 번에 되돌릴 수 있습니다.", MessageType.Info);
            Heading("1. 기준 UI와 좌표 변환");
            var next = (RectTransform)EditorGUILayout.ObjectField("기준 UI", referenceUI, typeof(RectTransform), true);
            if (next != referenceUI)
            {
                referenceUI = next;
                var canvas = referenceUI != null ? referenceUI.GetComponentInParent<Canvas>() : null;
                if (worldCamera == null && canvas != null) worldCamera = canvas.rootCanvas.worldCamera;
            }
            settings.mapping = (UIPlacementMapping)EditorGUILayout.EnumPopup("좌표 변환 방식", settings.mapping);
            if (settings.mapping == UIPlacementMapping.ScreenToWorld)
            {
                uiCamera = (Camera)EditorGUILayout.ObjectField("UI 카메라 (비우면 Canvas)", uiCamera, typeof(Camera), true);
                worldCamera = (Camera)EditorGUILayout.ObjectField("월드 카메라", worldCamera, typeof(Camera), true);
                EditorGUILayout.HelpBox("UI의 화면 위치를 월드 카메라의 XY 평면으로 변환합니다. " +
                    "Overlay Canvas에서는 UI 카메라를 사용하지 않습니다.", MessageType.None);
            }

            Heading("2. 배치 대상 · 목록 순서대로 배치");
            windowObject.Update();
            EditorGUILayout.PropertyField(windowObject.FindProperty("targets"), new GUIContent("씬 오브젝트 목록"), true);
            windowObject.ApplyModifiedProperties();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 오브젝트 추가"))
            {
                foreach (var target in Selection.transforms)
                    if (target != null && target != referenceUI && !EditorUtility.IsPersistent(target) && !targets.Contains(target)) targets.Add(target);
                result = null; SceneView.RepaintAll();
            }
            if (GUILayout.Button("목록 비우기")) { targets.Clear(); result = null; SceneView.RepaintAll(); }
            EditorGUILayout.EndHorizontal();
            settings.reverseOrder = EditorGUILayout.Toggle("배치 순서 반대로", settings.reverseOrder);

            Heading("3. 배치 방식");
            settings.pattern = (UIPlacementPattern)EditorGUILayout.EnumPopup("패턴", settings.pattern);
            settings.useReferencePivot = EditorGUILayout.Toggle("기준 UI의 실제 피벗 사용", settings.useReferencePivot);
            if (!settings.useReferencePivot)
                DrawAnchor("UI 기준점", ref settings.referenceAnchor, ref settings.customReferenceAnchor);
            if (settings.pattern != UIPlacementPattern.Point)
            {
                settings.spacingMode = (UIPlacementSpacing)EditorGUILayout.EnumPopup("간격 방식", settings.spacingMode);
                if (settings.pattern == UIPlacementPattern.Grid) settings.columns = EditorGUILayout.IntField("격자 열 개수", settings.columns);
                if (settings.pattern == UIPlacementPattern.Ellipse)
                {
                    if (settings.spacingMode == UIPlacementSpacing.Fixed)
                        settings.ellipseRadius = EditorGUILayout.Vector2Field("타원 반지름 · 가로/세로", settings.ellipseRadius);
                    settings.startAngle = EditorGUILayout.FloatField("시작 각도", settings.startAngle);
                    settings.clockwise = EditorGUILayout.Toggle("시계 방향", settings.clockwise);
                }
                else if (settings.spacingMode == UIPlacementSpacing.Fixed)
                {
                    settings.spacing = EditorGUILayout.Vector2Field("간격 · 가로/세로", settings.spacing);
                    DrawAnchor("배치 그룹 피벗", ref settings.groupAnchor, ref settings.customGroupAnchor);
                }
            }
            else if (targets.Count > 1)
                EditorGUILayout.HelpBox("Point는 모든 대상을 같은 기준점에 정렬합니다. 펼쳐 놓으려면 Horizontal / Vertical / Grid / Ellipse를 선택하세요.", MessageType.None);
            settings.paddingMin = EditorGUILayout.Vector2Field("여백 · 왼쪽/아래", settings.paddingMin);
            settings.paddingMax = EditorGUILayout.Vector2Field("여백 · 오른쪽/위", settings.paddingMax);

            Heading("4. 대상 피벗 · 오프셋 · 깊이");
            settings.targetPivot = (UIPlacementTargetPivot)EditorGUILayout.EnumPopup("대상 피벗 계산", settings.targetPivot);
            if (settings.targetPivot != UIPlacementTargetPivot.TransformOrigin)
                DrawAnchor("대상 정렬점", ref settings.objectAnchor, ref settings.customObjectAnchor);
            settings.uiOffset = EditorGUILayout.Vector2Field("UI 오프셋 · UI 로컬 단위", settings.uiOffset);
            settings.worldOffset = EditorGUILayout.Vector2Field("월드 오프셋 · 월드 단위", settings.worldOffset);
            settings.keepTargetZ = EditorGUILayout.Toggle("대상의 기존 Z 유지", settings.keepTargetZ);
            if (!settings.keepTargetZ) settings.worldZ = EditorGUILayout.FloatField("배치할 월드 Z", settings.worldZ);
            settings.moveX = EditorGUILayout.Toggle("가로 위치 적용", settings.moveX);
            settings.moveY = EditorGUILayout.Toggle("세로 위치 적용", settings.moveY);
            settings.snapToWorldGrid = EditorGUILayout.Toggle("월드 격자에 스냅", settings.snapToWorldGrid);
            if (settings.snapToWorldGrid) settings.snapStep = EditorGUILayout.Vector2Field("스냅 간격 · 가로/세로", settings.snapStep);

            Heading("5. 확인 및 적용");
            preview = EditorGUILayout.Toggle("Scene 뷰 미리보기", preview);
            bool valid = Build(out var moves, out _, out string error);
            if (!valid) EditorGUILayout.HelpBox(error, MessageType.Info);
            if (EditorApplication.isPlaying) EditorGUILayout.HelpBox("Play를 종료한 뒤 배치하세요.", MessageType.Info);
            EditorGUI.BeginDisabledGroup(!valid || EditorApplication.isPlaying);
            if (GUILayout.Button("배치 적용"))
            {
                Canvas.ForceUpdateCanvases();
                if (Build(out moves, out _, out error))
                {
                    try { UIObjectPlacementUtility.Apply(moves); result = moves.Count + "개 오브젝트 배치 완료 · Ctrl/Cmd+Z로 되돌리기"; }
                    catch (System.Exception exception) { result = exception.Message; Debug.LogException(exception); }
                }
                else result = error;
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.None);
            if (EditorGUI.EndChangeCheck()) { result = null; SceneView.RepaintAll(); }
            EditorGUILayout.EndScrollView();
        }

        private bool Build(out List<UIObjectPlacementMove> moves, out Vector3[] outline, out string error)
            => UIObjectPlacementUtility.TryBuildPlan(referenceUI, uiCamera, worldCamera, targets, settings, out moves, out outline, out error);
        private static void Heading(string text) { EditorGUILayout.Space(10f); EditorGUILayout.LabelField(text, EditorStyles.boldLabel); }
        private static void DrawAnchor(string label, ref UIPlacementAnchor anchor, ref Vector2 custom)
        {
            anchor = (UIPlacementAnchor)EditorGUILayout.EnumPopup(label, anchor);
            if (anchor == UIPlacementAnchor.Custom) custom = EditorGUILayout.Vector2Field("직접 입력 · 0~1 기준점", custom);
        }

        private void DrawPreview(SceneView view)
        {
            if (!preview || EditorApplication.isPlaying || !Build(out var moves, out var outline, out _)) return;
            Color previous = Handles.color;
            try
            {
                Handles.color = Color.cyan;
                for (int i = 0; i < outline.Length; i++) Handles.DrawLine(outline[i], outline[(i + 1) % outline.Length]);
                Handles.color = Color.yellow;
                foreach (var move in moves)
                {
                    Handles.DrawDottedLine(move.fromPivot, move.toPivot, 4f);
                    Handles.DrawWireDisc(move.toPivot, Vector3.forward, HandleUtility.GetHandleSize(move.toPivot) * .04f);
                    Handles.Label(move.toPivot, move.target.name);
                }
            }
            finally { Handles.color = previous; }
        }
    }
}
#endif
