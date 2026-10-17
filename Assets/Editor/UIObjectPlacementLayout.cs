using System;
using UnityEngine;

namespace Catnip.EditorTools
{
    public enum UIPlacementPattern { Point, Horizontal, Vertical, Grid, Ellipse }
    public enum UIPlacementSpacing { Fixed, FitReference }
    public enum UIPlacementAnchor
    {
        BottomLeft, BottomCenter, BottomRight, MiddleLeft, Center, MiddleRight,
        TopLeft, TopCenter, TopRight, Custom
    }
    public enum UIPlacementMapping { ScreenToWorld, RectWorldCoordinates }
    public enum UIPlacementTargetPivot { TransformOrigin, RendererBounds, Collider2DBounds, RectTransformBounds }

    [Serializable]
    public sealed class UIObjectPlacementSettings
    {
        public UIPlacementPattern pattern;
        public UIPlacementSpacing spacingMode = UIPlacementSpacing.FitReference;
        public UIPlacementAnchor referenceAnchor = UIPlacementAnchor.Center;
        public Vector2 customReferenceAnchor = new Vector2(.5f, .5f);
        public bool useReferencePivot;
        public UIPlacementAnchor groupAnchor = UIPlacementAnchor.Center;
        public Vector2 customGroupAnchor = new Vector2(.5f, .5f);
        public Vector2 spacing = new Vector2(100f, 100f);
        public int columns = 3;
        public Vector2 ellipseRadius = new Vector2(100f, 100f);
        public float startAngle = 90f;
        public bool clockwise = true;
        public bool reverseOrder;
        public Vector2 paddingMin, paddingMax, uiOffset, worldOffset;
        public UIPlacementMapping mapping;
        public UIPlacementTargetPivot targetPivot;
        public UIPlacementAnchor objectAnchor = UIPlacementAnchor.Center;
        public Vector2 customObjectAnchor = new Vector2(.5f, .5f);
        public bool keepTargetZ;
        public bool moveX = true, moveY = true;
        public float worldZ;
        public bool snapToWorldGrid;
        public Vector2 snapStep = new Vector2(.1f, .1f);
    }

    // 카메라/오브젝트를 수정하지 않는 배치 계산입니다. 간격·여백은 RectTransform 로컬 단위입니다.
    public static class UIObjectPlacementLayout
    {
        public static Vector2 Anchor(UIPlacementAnchor anchor, Vector2 custom)
        {
            if (anchor == UIPlacementAnchor.Custom) return custom;
            int index = (int)anchor;
            return new Vector2(index % 3 * .5f, index / 3 * .5f);
        }

        public static Rect ContentRect(Rect rect, UIObjectPlacementSettings settings)
        {
            Vector2 min = rect.min + settings.paddingMin;
            Vector2 max = rect.max - settings.paddingMax;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static bool Validate(Rect rect, UIObjectPlacementSettings s, out string error)
        {
            error = null;
            if (s == null) error = "배치 설정이 없습니다.";
            else if (!Finite(rect.min) || !Finite(rect.max) || rect.width <= 0f || rect.height <= 0f)
                error = "기준 UI의 가로·세로 크기는 0보다 커야 합니다.";
            else if (!Finite(s.paddingMin) || !Finite(s.paddingMax) || s.paddingMin.x < 0f || s.paddingMin.y < 0f ||
                     s.paddingMax.x < 0f || s.paddingMax.y < 0f || s.paddingMin.x + s.paddingMax.x >= rect.width ||
                     s.paddingMin.y + s.paddingMax.y >= rect.height)
                error = "여백은 0 이상이며 UI 크기보다 작아야 합니다.";
            else if (!Finite(s.customReferenceAnchor) || !Finite(s.customGroupAnchor) || !Finite(s.customObjectAnchor) ||
                     !Finite(s.uiOffset) || !Finite(s.worldOffset) || !Finite(s.worldZ) || !Finite(s.startAngle))
                error = "피벗·오프셋·깊이·각도에는 유한한 숫자를 입력하세요.";
            else if (!Finite(s.spacing) || s.spacing.x < 0f || s.spacing.y < 0f ||
                     !Finite(s.ellipseRadius) || s.ellipseRadius.x < 0f || s.ellipseRadius.y < 0f || s.columns < 1)
                error = "간격·반지름은 0 이상, 격자 열 개수는 1 이상이어야 합니다.";
            else if (s.snapToWorldGrid && (!Finite(s.snapStep) || s.snapStep.x <= 0f || s.snapStep.y <= 0f))
                error = "월드 스냅 간격은 가로·세로 모두 0보다 커야 합니다.";
            return error == null;
        }

        public static Vector2 Position(Rect rect, Vector2 referencePivot, int index, int count, UIObjectPlacementSettings s)
        {
            if (count < 1 || index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            Rect area = ContentRect(rect, s);
            Vector2 anchor = Anchor(s.referenceAnchor, s.customReferenceAnchor);
            Vector2 origin = s.useReferencePivot ? rect.min + Vector2.Scale(rect.size, referencePivot) :
                area.min + Vector2.Scale(area.size, anchor);
            int slot = s.reverseOrder ? count - 1 - index : index;
            Vector2 position = origin;
            if (s.pattern == UIPlacementPattern.Ellipse)
            {
                Vector2 radius = s.spacingMode == UIPlacementSpacing.FitReference ? area.size * .5f : s.ellipseRadius;
                float angle = (s.startAngle + (s.clockwise ? -1f : 1f) * 360f * slot / count) * Mathf.Deg2Rad;
                position += new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
            }
            else if (s.pattern != UIPlacementPattern.Point)
            {
                int columns = s.pattern == UIPlacementPattern.Horizontal ? count :
                    s.pattern == UIPlacementPattern.Vertical ? 1 : Mathf.Min(count, s.columns);
                int rows = (count - 1) / columns + 1;
                int column = slot % columns, row = rows - 1 - slot / columns;
                if (s.spacingMode == UIPlacementSpacing.FitReference)
                    position = new Vector2(columns > 1 ? area.xMin + area.width * column / (columns - 1) : origin.x,
                        rows > 1 ? area.yMin + area.height * row / (rows - 1) : origin.y);
                else
                {
                    Vector2 groupSize = new Vector2((columns - 1) * s.spacing.x, (rows - 1) * s.spacing.y);
                    position += new Vector2(column * s.spacing.x, row * s.spacing.y) -
                        Vector2.Scale(groupSize, Anchor(s.groupAnchor, s.customGroupAnchor));
                }
            }
            return position + s.uiOffset;
        }

        public static Vector3 Snap(Vector3 point, Vector2 step)
            => new Vector3(Mathf.Round(point.x / step.x) * step.x, Mathf.Round(point.y / step.y) * step.y, point.z);
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}
