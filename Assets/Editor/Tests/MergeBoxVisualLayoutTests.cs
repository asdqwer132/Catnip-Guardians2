using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MergeBoxVisualLayoutTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void PerFrameOffsetsAndColliderInsetsDoNotAccumulate(int hierarchy)
    {
        var root = new GameObject("Series root");
        var boxHost = new GameObject("Box"); boxHost.transform.SetParent(root.transform, false);
        var visual = new GameObject("Wall"); visual.transform.SetParent(boxHost.transform, false);
        var colliderHost = hierarchy == 0 ? visual : new GameObject("Collider");
        if (hierarchy != 0) colliderHost.transform.SetParent(boxHost.transform, false);
        if (hierarchy == 2) visual.transform.SetParent(colliderHost.transform, false);
        if (hierarchy == 3) colliderHost.transform.SetParent(visual.transform, false);
        var texture = new Texture2D(16, 16);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
        try
        {
            root.transform.localScale = new Vector3(2, 3, 1);
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(4, 6);
            if (hierarchy == 4) colliderHost.transform.localScale = new Vector3(2, .5f, 1);
            if (hierarchy != 0) colliderHost.transform.localPosition = new Vector3(-2, 3, 0);
            // Stale collider settings must be replaced by the Visual geometry.
            var collider = colliderHost.AddComponent<BoxCollider2D>(); collider.size = new Vector2(9, 13);
            collider.offset = new Vector2(.1f, .2f); collider.autoTiling = true;
            visual.transform.position = boxHost.transform.position;
            var box = boxHost.AddComponent<MergeBoxBindings>();
            box.visuals = new[] { new MergeBoxSpritePart { role = MergeBoxSpriteRole.SideWall, renderer = renderer, wallCollider = collider } };
            box.CaptureVisualLayout(new Vector2(4, 6));
            var control = root.AddComponent<MergeBoxVisualController>();
            var entry = new MergeBoxVisualSize { box = box, width = 4, height = 6 };
            control.boxes = new[] { entry }; control.ApplySizes();
            Assert.That(entry.parts.Length, Is.EqualTo(1));
            entry.parts[0].positionOffset = new Vector2(.3f, -.4f);
            entry.parts[0].colliderPadding = new MergeBoxColliderPadding { left = .5f, right = .2f, top = .4f, bottom = .1f };
            for (int i = 0; i < 5; i++)
            {
                control.ApplySizes();
                Assert.That(Vector2.Distance(collider.size, hierarchy == 4 ? new Vector2(1.65f, 11) : new Vector2(3.3f, 5.5f)), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(collider.offset, hierarchy == 4 ? new Vector2(.075f, -.3f) : new Vector2(.15f, -.15f)), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(box.transform.InverseTransformPoint(visual.transform.position), new Vector2(.3f, -.4f)), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(box.transform.InverseTransformPoint(colliderHost.transform.position), new Vector2(.3f, -.4f)), Is.LessThan(.001f));
                Assert.That(renderer.size, Is.EqualTo(new Vector2(4, 6)));
                Assert.That(collider.autoTiling, Is.False);
            }
            visual.transform.position += box.transform.TransformVector(new Vector3(.2f, .1f, 0));
            Vector3 manuallyMoved = visual.transform.position;
            typeof(MergeBoxVisualController).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(control, null);
            Assert.That(Vector3.Distance(collider.transform.position, manuallyMoved), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(visual.transform.position, manuallyMoved), Is.LessThan(.001f));
            entry.parts[0].colliderPadding = default; entry.parts[0].positionOffset = Vector2.zero;
            control.ApplySizes();
            Assert.That(collider.offset.magnitude, Is.LessThan(.001f));
            Assert.That(Vector2.Distance(collider.size, hierarchy == 4 ? new Vector2(2, 12) : new Vector2(4, 6)), Is.LessThan(.001f));
            Assert.That(visual.transform.localPosition, Is.EqualTo(Vector3.zero));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

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
            var collider = visual.AddComponent<BoxCollider2D>();
            var box = boxHost.AddComponent<MergeBoxBindings>();
            box.visuals = new[] { new MergeBoxSpritePart { role = MergeBoxSpriteRole.BackFrame, renderer = renderer, wallCollider = collider } };
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
                Assert.That(Vector2.Distance(collider.size, renderer.localBounds.size), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(collider.offset, renderer.localBounds.center), Is.LessThan(.001f));
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
