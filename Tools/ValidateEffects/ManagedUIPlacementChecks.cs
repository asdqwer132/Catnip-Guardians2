using Catnip.EditorTools;
using UnityEngine;

public static partial class Runner
{
    private static void RunUIPlacementChecks()
    {
        var rect = new Rect(-100f, -50f, 200f, 100f);
        var pivot = new Vector2(.5f, .5f);
        Check("UI placement supports nine anchors and custom normalized points", () => {
            var settings = new UIObjectPlacementSettings();
            for (int i = 0; i < 9; i++) {
                settings.referenceAnchor = (UIPlacementAnchor)i;
                Vector2 point = UIObjectPlacementLayout.Position(rect, pivot, 0, 1, settings);
                Eq(point.x, -100f + i % 3 * 100f); Eq(point.y, -50f + i / 3 * 50f);
            }
            settings.referenceAnchor = UIPlacementAnchor.Custom; settings.customReferenceAnchor = new Vector2(.25f, .8f);
            var custom = UIObjectPlacementLayout.Position(rect, pivot, 0, 1, settings); Eq(custom.x, -50f); Eq(custom.y, 30f);
        });
        Check("UI reference actual pivot stays fixed when padding changes and offsets apply once", () => {
            var settings = new UIObjectPlacementSettings { useReferencePivot = true, paddingMin = new Vector2(10f, 20f),
                paddingMax = new Vector2(30f, 10f), uiOffset = new Vector2(3f, -4f) };
            var point = UIObjectPlacementLayout.Position(rect, pivot, 0, 1, settings); Eq(point.x, 3f); Eq(point.y, -4f);
            settings.useReferencePivot = false; settings.referenceAnchor = UIPlacementAnchor.BottomLeft;
            point = UIObjectPlacementLayout.Position(rect, pivot, 0, 1, settings); Eq(point.x, -87f); Eq(point.y, -34f);
        });
        Check("UI fit spacing spans the padded rectangle with singleton axes anchored safely", () => {
            var settings = new UIObjectPlacementSettings { pattern = UIPlacementPattern.Horizontal,
                paddingMin = new Vector2(10f, 20f), paddingMax = new Vector2(30f, 10f) };
            for (int i = 0; i < 3; i++) {
                var point = UIObjectPlacementLayout.Position(rect, pivot, i, 3, settings); Eq(point.x, -90f + 80f * i); Eq(point.y, 5f);
            }
            settings.pattern = UIPlacementPattern.Vertical;
            for (int i = 0; i < 3; i++) {
                var point = UIObjectPlacementLayout.Position(rect, pivot, i, 3, settings); Eq(point.x, -10f); Eq(point.y, 40f - 35f * i);
            }
            var singleton = UIObjectPlacementLayout.Position(rect, pivot, 0, 1, settings); Eq(singleton.x, -10f); Eq(singleton.y, 5f);
        });
        Check("UI fixed grid uses group pivot and supports incomplete rows and reversed order", () => {
            var settings = new UIObjectPlacementSettings { pattern = UIPlacementPattern.Grid, columns = 3,
                spacingMode = UIPlacementSpacing.Fixed, spacing = new Vector2(10f, 20f) };
            var first = UIObjectPlacementLayout.Position(rect, pivot, 0, 5, settings); Eq(first.x, -10f); Eq(first.y, 10f);
            var last = UIObjectPlacementLayout.Position(rect, pivot, 4, 5, settings); Eq(last.x, 0f); Eq(last.y, -10f);
            settings.reverseOrder = true;
            var reversed = UIObjectPlacementLayout.Position(rect, pivot, 0, 5, settings); Eq(reversed.x, last.x); Eq(reversed.y, last.y);
            settings.reverseOrder = false; settings.groupAnchor = UIPlacementAnchor.TopLeft;
            first = UIObjectPlacementLayout.Position(rect, pivot, 0, 5, settings); Eq(first.x, 0f); Eq(first.y, 0f);
        });
        Check("UI fit grid covers all columns and rows without requiring a full final row", () => {
            var settings = new UIObjectPlacementSettings { pattern = UIPlacementPattern.Grid, columns = 3 };
            var first = UIObjectPlacementLayout.Position(rect, pivot, 0, 5, settings); Eq(first.x, -100f); Eq(first.y, 50f);
            var last = UIObjectPlacementLayout.Position(rect, pivot, 4, 5, settings); Eq(last.x, 0f); Eq(last.y, -50f);
        });
        Check("UI ellipse respects separate radii direction and start angle without duplicating endpoints", () => {
            var settings = new UIObjectPlacementSettings { pattern = UIPlacementPattern.Ellipse };
            var small = new Rect(-1f, -2f, 2f, 4f);
            var top = UIObjectPlacementLayout.Position(small, pivot, 0, 4, settings); Eq(top.x, 0f); Eq(top.y, 2f);
            var right = UIObjectPlacementLayout.Position(small, pivot, 1, 4, settings); Eq(right.x, 1f); Eq(right.y, 0f);
            settings.clockwise = false;
            var left = UIObjectPlacementLayout.Position(small, pivot, 1, 4, settings); Eq(left.x, -1f); Eq(left.y, 0f);
            settings.spacingMode = UIPlacementSpacing.Fixed; settings.ellipseRadius = new Vector2(3f, 4f);
            top = UIObjectPlacementLayout.Position(small, pivot, 0, 1, settings); Eq(top.y, 4f);
        });
        Check("UI layout validation rejects invalid padding dimensions spacing and snap steps", () => {
            var settings = new UIObjectPlacementSettings(); True(UIObjectPlacementLayout.Validate(rect, settings, out _));
            settings.paddingMin.x = 200f; True(!UIObjectPlacementLayout.Validate(rect, settings, out _)); settings.paddingMin.x = 0f;
            settings.worldOffset.x = float.NaN; True(!UIObjectPlacementLayout.Validate(rect, settings, out _)); settings.worldOffset.x = 0f;
            settings.columns = 0; True(!UIObjectPlacementLayout.Validate(rect, settings, out _)); settings.columns = 3;
            settings.snapToWorldGrid = true; settings.snapStep.x = 0f; True(!UIObjectPlacementLayout.Validate(rect, settings, out _));
            True(!UIObjectPlacementLayout.Validate(new Rect(0f, 0f, 0f, 1f), new UIObjectPlacementSettings(), out _));
        });
        Check("UI world snapping rounds each axis independently and preserves depth", () => {
            var point = UIObjectPlacementLayout.Snap(new Vector3(1.24f, -1.13f, 7f), new Vector2(.5f, .25f));
            Eq(point.x, 1f); Eq(point.y, -1.25f); Eq(point.z, 7f);
        });
    }
}
