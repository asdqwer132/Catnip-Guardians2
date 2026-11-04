using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MergeBoxVisualLayoutTests
{
    [Test]
    public void BackgroundPaddingRespectsPivotScaleAndRepeatedUpdates()
    {
        var root = new GameObject("Series root");
        var boxHost = new GameObject("Box"); boxHost.transform.SetParent(root.transform, false);
        var visual = new GameObject("Background"); visual.transform.SetParent(boxHost.transform, false);
        var texture = new Texture2D(16, 16);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.zero, 16);
        try
        {
            root.transform.localScale = new Vector3(2, 3, 1);
            visual.transform.localScale = new Vector3(.5f, 2, 1);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = new Vector2(12, 2);
            var box = boxHost.AddComponent<MergeBoxBindings>();
            box.visuals = new[] { new MergeBoxSpritePart { role = MergeBoxSpriteRole.BackFrame, renderer = renderer } };
            box.CaptureVisualLayout(new Vector2(6, 4));
            var control = root.AddComponent<MergeBoxVisualController>();
            var entry = new MergeBoxVisualSize { box = box, width = 6, height = 4,
                backgroundPadding = new MergeBoxBackgroundPadding { left = 1, right = .5f, bottom = .2f, top = .8f } };
            control.boxes = new[] { entry };
            for (int i = 0; i < 5; i++)
            {
                control.ApplySizes();
                Vector3 min = box.transform.InverseTransformPoint(renderer.transform.TransformPoint(renderer.localBounds.min));
                Vector3 max = box.transform.InverseTransformPoint(renderer.transform.TransformPoint(renderer.localBounds.max));
                Assert.That(Vector2.Distance(min, new Vector2(1, .2f)), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(max, new Vector2(5.5f, 3.2f)), Is.LessThan(.001f));
            }
            entry.backgroundPadding = new MergeBoxBackgroundPadding { left = 100, right = 100, top = 100, bottom = 100 };
            control.ApplySizes();
            Assert.That(renderer.size.x, Is.GreaterThan(0)); Assert.That(renderer.size.y, Is.GreaterThan(0));
            entry.backgroundPadding = default; control.ApplySizes();
            Assert.That(Vector2.Distance(renderer.size, new Vector2(12, 2)), Is.LessThan(.001f));
            Assert.That(visual.transform.localPosition.magnitude, Is.LessThan(.001f));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

    [Test]
    public void SeriesPrefabsKeepTheirLayoutAfterRepeatedResizeAndRestore()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets/Prefab/Merge/Box"});
        int verified = 0;
        foreach (var guid in paths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/Prefab/Merge/Box/Box (")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var control = root.GetComponent<MergeBoxVisualController>();
                Assert.That(control, Is.Not.Null, path);
                control.ApplySizes(); // Initialize the prefab's currently configured size before recording geometry.
                var input = root.GetComponent<WallSpriteInput>();
                Assert.That(input, Is.Not.Null, path);
                Assert.That(root.GetComponentsInChildren<WallSpriteInput>(true).Length, Is.EqualTo(1));
                foreach (var entry in control.boxes)
                {
                    var box = entry.box;
                    Assert.That(box.HasVisualLayout, Is.True);
                    var sizes = new Vector2[box.visuals.Length];
                    for (int i = 0; i < sizes.Length; i++) sizes[i] = box.visuals[i].renderer.size;
                    Vector3 spawn = box.spawnPoint.localPosition;
                    Vector2 line = box.dropZone.lineSprite.size;
                    Vector3 linePosition = box.dropZone.lineSprite.transform.localPosition;
                    Rect movement = box.dropZone.movementArea;
                    float width = entry.width, height = entry.height;
                    entry.width += 2; entry.height += 3;
                    control.ApplySizes();
                    for (int i = 0; i < sizes.Length; i++)
                    {
                        var part = box.visuals[i];
                        Vector2 expected = sizes[i] + (part.role == MergeBoxSpriteRole.SideWall ? new Vector2(0,3) :
                            part.role == MergeBoxSpriteRole.BottomWall ? new Vector2(2,0) : new Vector2(2,3)) / part.units;
                        Assert.That(Vector2.Distance(part.renderer.size, expected), Is.LessThan(.001f));
                    }
                    Vector3 enlargedSpawn = box.spawnPoint.position;
                    Vector2 enlargedLine = box.dropZone.lineSprite.size;
                    for (int repeat = 0; repeat < 5; repeat++) control.ApplySizes();
                    Assert.That(Vector3.Distance(enlargedSpawn, box.spawnPoint.position), Is.LessThan(.001f));
                    Assert.That(Vector2.Distance(enlargedLine, box.dropZone.lineSprite.size), Is.LessThan(.001f));
                    entry.width = width; entry.height = height; control.ApplySizes();
                    Assert.That(Vector3.Distance(spawn, box.spawnPoint.localPosition), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(linePosition, box.dropZone.lineSprite.transform.localPosition), Is.LessThan(.001f));
                    Assert.That(Vector2.Distance(line, box.dropZone.lineSprite.size), Is.LessThan(.001f));
                    Assert.That(box.dropZone.movementArea, Is.EqualTo(movement));
                }
                verified++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Assert.That(verified, Is.EqualTo(8));
    }
}
