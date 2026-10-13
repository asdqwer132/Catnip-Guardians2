using UnityEngine;

public static partial class Runner
{
    private static void RunPrefabVisualChecks()
    {
        Check("visual entrance interpolates scale and alpha without changing settings", () => {
            var enter = new PrefabVisualEntrance { duration = 2f, startScale = new Vector3(0.5f,0.5f,1f), startAlpha = 0.2f };
            var frame = PrefabVisualAnimationMath.Evaluate(enter,null,null,1f,float.PositiveInfinity);
            Eq(frame.scaleMultiplier.x,0.75f); Eq(frame.scaleMultiplier.z,1f); Eq(frame.alphaMultiplier,0.6f);
            Eq(enter.startScale.x,0.5f); Eq(enter.startAlpha,0.2f);
        });
        Check("visual exit follows remaining runtime lifetime", () => {
            var exit = new PrefabVisualExit { duration = 1f, endScale = new Vector3(0.5f,0.5f,1f) };
            var halfway = PrefabVisualAnimationMath.Evaluate(null,null,exit,10f,0.5f);
            Eq(halfway.scaleMultiplier.x,0.75f); Eq(halfway.alphaMultiplier,0.5f);
            var end = PrefabVisualAnimationMath.Evaluate(null,null,exit,10f,0f);
            Eq(end.scaleMultiplier.x,0.5f); Eq(end.alphaMultiplier,0f);
        });
        Check("short prefab lifetime composes overlapping entry and exit safely", () => {
            var enter = new PrefabVisualEntrance { duration = 1f, startScale = new Vector3(0.5f,0.5f,1f) };
            var exit = new PrefabVisualExit { duration = 1f, endScale = new Vector3(0.5f,0.5f,1f) };
            var frame = PrefabVisualAnimationMath.Evaluate(enter,null,exit,0.5f,0.5f);
            Eq(frame.scaleMultiplier.x,0.5625f); Eq(frame.alphaMultiplier,0.25f);
        });
        Check("manual exit can finish an otherwise indefinite visual", () => {
            var exit = new PrefabVisualExit { duration = 1f };
            var frame = PrefabVisualAnimationMath.Evaluate(null,null,exit,9f,float.PositiveInfinity,1f);
            Eq(frame.alphaMultiplier,0f);
        });
        Check("loop transforms are deterministic offsets and never compound previous frames", () => {
            var loop = new PrefabVisualLoop { rotate = true, rotationDegreesPerSecond = 90f,
                pulse = true, pulseAmplitude = 0.1f, pulseFrequency = 1f,
                sway = true, swayAmplitude = new Vector2(0.1f,0.2f), swayFrequency = 1f };
            var first = PrefabVisualAnimationMath.Evaluate(null,loop,null,0.25f,10f);
            var again = PrefabVisualAnimationMath.Evaluate(null,loop,null,0.25f,10f);
            Eq(first.rotationDegrees,22.5f); Eq(first.scaleMultiplier.x,1.1f); Eq(first.scaleMultiplier.z,1f);
            Eq(first.offset.x,0.1f); Eq(first.scaleMultiplier.x,again.scaleMultiplier.x); Eq(first.offset.x,again.offset.x);
        });
        Check("disabled visual channels keep the baseline frame", () => {
            var frame = PrefabVisualAnimationMath.Evaluate(new PrefabVisualEntrance { enabled = false },
                new PrefabVisualLoop(),new PrefabVisualExit { enabled = false },1f,0f);
            Eq(frame.scaleMultiplier.x,1f); Eq(frame.alphaMultiplier,1f); Eq(frame.rotationDegrees,0f); Eq(frame.offset.sqrMagnitude,0f);
        });
        Check("zero duration entrance and explicit exit complete without division by zero", () => {
            var frame = PrefabVisualAnimationMath.Evaluate(new PrefabVisualEntrance { duration = 0f },null,
                new PrefabVisualExit { duration = 0f },0f,float.PositiveInfinity,0f);
            Eq(frame.alphaMultiplier,0f); True(!float.IsNaN(frame.scaleMultiplier.x));
        });
        Check("invalid visual values sanitize on the runtime frame without mutating configuration", () => {
            var entrance = new PrefabVisualEntrance { duration = float.NaN, startScale = new Vector3(float.NaN,-2f,float.PositiveInfinity), startAlpha = float.NaN };
            var frame = PrefabVisualAnimationMath.Evaluate(entrance,null,null,0f,float.PositiveInfinity);
            Eq(frame.scaleMultiplier.x,1f); Eq(frame.scaleMultiplier.y,0f); Eq(frame.scaleMultiplier.z,1f); Eq(frame.alphaMultiplier,1f);
            True(float.IsNaN(entrance.duration)); Eq(entrance.startScale.y,-2f);
        });
        Check("extended lifetime restores the ongoing visual before its natural fade", () => {
            var exit = new PrefabVisualExit { duration = 1f };
            Eq(PrefabVisualAnimationMath.Evaluate(null,null,exit,10f,0.5f).alphaMultiplier,0.5f);
            Eq(PrefabVisualAnimationMath.Evaluate(null,null,exit,10f,2f).alphaMultiplier,1f);
        });
        Check("circular gizmo closes its world XY outline", () => {
            var range = PrefabVisualRange.Circle(new Vector3(2f,3f,5f),2f,"Circle",PrefabRangeKind.Damage);
            var start = range.ArcPoint(0f); var end = range.ArcPoint(1f);
            True(Vector3.Distance(start,end)<0.00001f); Eq(start.z,5f);
            var right = range.ArcPoint(0.25f); Eq(right.x,4f); Eq(right.y,3f);
        });
        Check("sector gizmo arcs use the same forward direction and half angle", () => {
            var range = PrefabVisualRange.Circle(Vector3.zero,3f,"Sector",PrefabRangeKind.Damage);
            range.shape = PrefabRangeShape.Sector; range.forward = Vector3.right; range.angle = 90f;
            Eq(range.ArcPoint(0.5f).x,3f); Eq(range.ArcPoint(0.5f).y,0f);
            Eq(range.ArcPoint(0f).y,-3f/Mathf.Sqrt(2f)); Eq(range.ArcPoint(1f).y,3f/Mathf.Sqrt(2f));
        });
    }
}
