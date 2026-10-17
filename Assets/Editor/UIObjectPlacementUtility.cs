#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Catnip.EditorTools
{
    public readonly struct UIObjectPlacementMove
    {
        public readonly Transform target;
        public readonly Vector3 originalPosition, position, fromPivot, toPivot;
        public UIObjectPlacementMove(Transform target, Vector3 pivot, Vector3 destination)
        {
            this.target = target;
            originalPosition = target.position;
            fromPivot = pivot; toPivot = destination;
            position = originalPosition + destination - pivot;
        }
    }

    public static class UIObjectPlacementUtility
    {
        public static bool TryBuildPlan(RectTransform reference, Camera uiCamera, Camera worldCamera,
            IList<Transform> targets, UIObjectPlacementSettings settings,
            out List<UIObjectPlacementMove> moves, out Vector3[] outline, out string error)
        {
            moves = new List<UIObjectPlacementMove>(); outline = null; error = null;
            if (reference == null || EditorUtility.IsPersistent(reference)) return Fail("씬의 기준 UI RectTransform을 연결하세요.", out error);
            if (!UIObjectPlacementLayout.Validate(reference.rect, settings, out error)) return false;
            if (!TryUICamera(reference, settings.mapping, ref uiCamera, worldCamera, out error)) return false;
            var unique = new List<Transform>();
            if (targets != null) foreach (var target in targets)
            {
                if (target == null || unique.Contains(target)) continue;
                if (EditorUtility.IsPersistent(target) || !target.gameObject.scene.IsValid() ||
                    (target.gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    return Fail(target.name + ": 씬에서 편집할 수 있는 오브젝트를 넣으세요. 프리팹 에셋은 먼저 씬에 배치하세요.", out error);
                if (reference == target || reference.IsChildOf(target))
                    return Fail("기준 UI 또는 그 부모는 배치 대상에 넣을 수 없습니다.", out error);
                foreach (var other in unique)
                    if (target.IsChildOf(other) || other.IsChildOf(target))
                        return Fail("부모·자식을 동시에 이동하면 결과가 달라집니다. 한쪽만 대상으로 선택하세요.", out error);
                unique.Add(target);
            }
            if (unique.Count == 0) return Fail("배치할 씬 오브젝트를 대상 목록에 추가하세요.", out error);
            for (int i = 0; i < unique.Count; i++)
            {
                Transform target = unique[i];
                if (!TryPivot(target, settings, out var pivot)) return Fail(target.name + ": 선택한 피벗에 사용할 활성 Renderer / Collider2D / RectTransform이 없습니다.", out error);
                Vector2 local = UIObjectPlacementLayout.Position(reference.rect, reference.pivot, i, unique.Count, settings);
                float z = settings.keepTargetZ ? pivot.z : settings.worldZ;
                if (!TryMap(reference, local, z, settings.mapping, uiCamera, worldCamera, out var destination))
                    return Fail("UI 위치를 지정한 월드 Z 평면에 투영할 수 없습니다. 카메라 방향·깊이를 확인하세요.", out error);
                destination += (Vector3)settings.worldOffset;
                if (settings.snapToWorldGrid) destination = UIObjectPlacementLayout.Snap(destination, settings.snapStep);
                if (!settings.moveX) destination.x = pivot.x;
                if (!settings.moveY) destination.y = pivot.y;
                var move = new UIObjectPlacementMove(target, pivot, destination);
                if (!UIObjectPlacementLayout.Finite(move.position)) return Fail("배치 결과가 유효하지 않습니다. 좌표·크기를 확인하세요.", out error);
                moves.Add(move);
            }
            Rect area = UIObjectPlacementLayout.ContentRect(reference.rect, settings);
            var corners = new[] { area.min, new Vector2(area.xMin, area.yMax), area.max, new Vector2(area.xMax, area.yMin) };
            outline = new Vector3[4];
            for (int i = 0; i < corners.Length; i++)
                if (!TryMap(reference, corners[i], moves[0].toPivot.z, settings.mapping, uiCamera, worldCamera, out outline[i]))
                    return Fail("기준 UI의 범위를 월드 평면에 표시할 수 없습니다.", out error);
            return true;
        }

        private static bool TryUICamera(RectTransform reference, UIPlacementMapping mapping,
            ref Camera uiCamera, Camera worldCamera, out string error)
        {
            error = null;
            if (mapping == UIPlacementMapping.RectWorldCoordinates) return true;
            if (worldCamera == null) return Fail("화면 기준 배치에는 월드 카메라가 필요합니다.", out error);
            var canvas = reference.GetComponentInParent<Canvas>();
            if (canvas == null) return Fail("기준 UI를 Canvas 아래에 두거나 Rect World Coordinates를 선택하세요.", out error);
            canvas = canvas.rootCanvas;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) uiCamera = null;
            else
            {
                if (uiCamera == null) uiCamera = canvas.worldCamera;
                if (uiCamera == null) return Fail("Canvas의 Render Camera 또는 도구의 UI 카메라를 연결하세요.", out error);
            }
            if ((canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.targetDisplay != worldCamera.targetDisplay) ||
                (uiCamera != null && uiCamera.targetDisplay != worldCamera.targetDisplay))
                return Fail("UI와 월드 카메라는 같은 Display를 사용해야 합니다.", out error);
            if ((uiCamera != null ? uiCamera.targetTexture : null) != worldCamera.targetTexture)
                return Fail("UI와 월드 카메라의 Target Texture가 다릅니다. 같은 출력 화면을 사용하세요.", out error);
            return true;
        }

        private static bool TryMap(RectTransform reference, Vector2 local, float z, UIPlacementMapping mode,
            Camera uiCamera, Camera worldCamera, out Vector3 point)
        {
            point = reference.TransformPoint(local);
            if (mode == UIPlacementMapping.RectWorldCoordinates) point.z = z;
            else
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, point);
                Ray ray = worldCamera.ScreenPointToRay(screen);
                var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
                if (!plane.Raycast(ray, out float distance)) return false;
                point = ray.GetPoint(distance);
            }
            return UIObjectPlacementLayout.Finite(point);
        }

        private static bool TryPivot(Transform target, UIObjectPlacementSettings settings, out Vector3 pivot)
        {
            pivot = target.position;
            if (settings.targetPivot == UIPlacementTargetPivot.TransformOrigin) return true;
            Bounds bounds = default;
            bool hasBounds = false;
            if (settings.targetPivot == UIPlacementTargetPivot.RectTransformBounds)
            {
                if (!(target is RectTransform rect)) return false;
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners) Encapsulate(ref bounds, ref hasBounds, new Bounds(corner, Vector3.zero));
            }
            else if (settings.targetPivot == UIPlacementTargetPivot.RendererBounds)
            {
                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                        Encapsulate(ref bounds, ref hasBounds, renderer.bounds);
            }
            else
            {
                foreach (var collider in target.GetComponentsInChildren<Collider2D>(true))
                    if (collider.enabled && collider.gameObject.activeInHierarchy)
                        Encapsulate(ref bounds, ref hasBounds, collider.bounds);
            }
            if (!hasBounds) return false;
            Vector2 anchor = UIObjectPlacementLayout.Anchor(settings.objectAnchor, settings.customObjectAnchor);
            pivot = new Vector3(bounds.min.x + bounds.size.x * anchor.x, bounds.min.y + bounds.size.y * anchor.y, pivot.z);
            return UIObjectPlacementLayout.Finite(pivot);
        }

        private static void Encapsulate(ref Bounds bounds, ref bool initialized, Bounds next)
        {
            if (!initialized) { bounds = next; initialized = true; }
            else bounds.Encapsulate(next);
        }
        private static bool Fail(string message, out string error) { error = message; return false; }

        public static void Apply(IReadOnlyList<UIObjectPlacementMove> moves)
        {
            // 미리보기 이후 대상 위치가 바뀌었다면 오래된 계획을 적용하지 않습니다.
            foreach (var move in moves)
                if (move.target == null || move.target.position != move.originalPosition)
                    throw new System.InvalidOperationException("대상이 변경되었습니다. 배치를 다시 계산하세요.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("UI 기준 오브젝트 배치");
            try
            {
                foreach (var move in moves)
                {
                    Undo.RecordObject(move.target, "UI 기준 오브젝트 배치");
                    move.target.position = move.position;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(move.target);
                }
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }
    }
}
#endif
