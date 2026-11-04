using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MergeBoxVisualLayoutTests
{
    private static readonly MergeBoxSpriteRole[] Roles = { MergeBoxSpriteRole.LeftWall, MergeBoxSpriteRole.RightWall,
        MergeBoxSpriteRole.BottomWall, MergeBoxSpriteRole.BackFrame };

    private static MergeBoxBindings CreateBox(GameObject root, Sprite sprite, Vector2 size)
    {
        var host = new GameObject("Box"); host.transform.SetParent(root.transform, false);
        var box = host.AddComponent<MergeBoxBindings>();
        box.visuals = new MergeBoxSpritePart[4];
        for (int i = 0; i < 4; i++)
        {
            var target = new GameObject(Roles[i].ToString()); target.transform.SetParent(host.transform, false);
            var renderer = target.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = i < 2 ? new Vector2(.8f, size.y) : i == 2 ? new Vector2(size.x, .8f) : size;
            target.AddComponent<BoxCollider2D>();
            box.visuals[i] = new MergeBoxSpritePart { role = Roles[i], target = target };
        }
        var spawn = new GameObject("Spawn").transform; spawn.SetParent(host.transform, false); spawn.localPosition = Vector3.up * size.y / 2;
        box.spawnPoint = spawn;
        box.dropZone = host.AddComponent<MergeDropZone>();
        var line = new GameObject("Deadline"); line.transform.SetParent(host.transform, false);
        box.dropZone.lineSprite = line.AddComponent<SpriteRenderer>(); box.dropZone.lineSprite.sprite = sprite;
        box.dropZone.lineSprite.drawMode = SpriteDrawMode.Sliced; box.dropZone.lineSprite.size = new Vector2(size.x, .1f);
        box.CaptureVisualLayout(size);
        return box;
    }

    [Test]
    public void SharedRoleSettingsApplyAcrossBoxesWhileDropPositionsRemainIndividual()
    {
        var root = new GameObject("Series"); var texture = new Texture2D(16, 16);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
        try
        {
            var first = CreateBox(root, sprite, new Vector2(4, 6));
            var second = CreateBox(root, sprite, new Vector2(6, 8));
            var control = root.AddComponent<MergeBoxVisualController>();
            control.boxes = new[] { new MergeBoxVisualSize { box = first, width = 4, height = 6, spawnOffset = Vector2.up },
                new MergeBoxVisualSize { box = second, width = 6, height = 8, deadlineOffset = Vector2.down } };
            control.parts[0].positionOffset = new Vector2(.3f, -.4f);
            control.parts[0].colliderPadding = new MergeBoxColliderPadding { left = .1f, right = .2f, top = .3f, bottom = .4f };
            control.backgroundPadding = new MergeBoxBackgroundPadding { left = .2f, right = .4f };
            for (int repeat = 0; repeat < 4; repeat++)
            {
                control.ApplySizes();
                foreach (var box in new[] { first, second })
                {
                    var left = box.visuals[0]; var right = box.visuals[1];
                    Assert.That(Vector2.Distance(left.renderer.transform.localPosition, new Vector2(.3f, -.4f)), Is.LessThan(.001f));
                    Assert.That(Vector2.Distance(left.wallCollider.size, left.renderer.size - new Vector2(.3f, .7f)), Is.LessThan(.001f));
                    Assert.That(Vector2.Distance(left.wallCollider.offset, new Vector2(-.05f, .05f)), Is.LessThan(.001f));
                    Assert.That(right.renderer.transform.localPosition, Is.EqualTo(Vector3.zero));
                }
            }
            Assert.That(first.spawnPoint.localPosition.y, Is.EqualTo(4));
            Assert.That(second.spawnPoint.localPosition.y, Is.EqualTo(4));
            Assert.That(first.dropZone.lineSprite.transform.localPosition.y, Is.Zero);
            Assert.That(second.dropZone.lineSprite.transform.localPosition.y, Is.EqualTo(-1));
            control.parts[0].positionOffset = Vector2.zero; control.parts[0].colliderPadding = default;
            control.backgroundPadding = default; control.ApplySizes();
            Assert.That(first.visuals[0].renderer.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(first.visuals[0].wallCollider.size, Is.EqualTo(first.visuals[0].renderer.size));
            Assert.That(first.visuals[3].renderer.size, Is.EqualTo(new Vector2(4, 6)));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

    [Test]
    public void ColliderFollowsDirectVisualChangesAndPaddingClampsAtSmallSizes()
    {
        var root = new GameObject("Series"); var texture = new Texture2D(16, 16);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.zero, 16);
        try
        {
            root.transform.localScale = new Vector3(2, 3, 1);
            var box = CreateBox(root, sprite, new Vector2(4, 6));
            var control = root.AddComponent<MergeBoxVisualController>();
            control.boxes = new[] { new MergeBoxVisualSize { box = box, width = 4, height = 6 } };
            control.ApplySizes();
            var left = box.visuals[0]; left.target.transform.localPosition = new Vector3(1, 2, 0);
            left.target.transform.localScale = new Vector3(.5f, 2, 1);
            typeof(MergeBoxVisualController).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(control, null);
            Assert.That(left.wallCollider.size, Is.EqualTo((Vector2)left.renderer.localBounds.size));
            Assert.That(left.wallCollider.offset, Is.EqualTo((Vector2)left.renderer.localBounds.center));
            Assert.That(left.target.transform.localPosition, Is.EqualTo(new Vector3(1, 2, 0)));
            control.parts[0].colliderPadding = new MergeBoxColliderPadding { left = 100, right = 100, top = 100, bottom = 100 };
            control.ApplySizes();
            Assert.That(left.wallCollider.size.x, Is.GreaterThan(0)); Assert.That(left.wallCollider.size.y, Is.GreaterThan(0));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

    [Test]
    public void SeriesPrefabsUseSingleObjectBindingsAndRestoreAfterResize()
    {
        int verified = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Merge/Box" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/Prefab/Merge/Box/Box (")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var control = root.GetComponent<MergeBoxVisualController>(); Assert.That(control, Is.Not.Null, path);
                control.ApplySizes();
                foreach (var entry in control.boxes)
                {
                    foreach (var part in entry.box.visuals)
                    {
                        Assert.That(part.target, Is.Not.Null, path);
                        Assert.That(part.renderer.gameObject, Is.SameAs(part.target));
                        if (part.role == MergeBoxSpriteRole.LeftWall || part.role == MergeBoxSpriteRole.RightWall || part.role == MergeBoxSpriteRole.BottomWall)
                            Assert.That(part.wallCollider, Is.Not.Null, path);
                        if (part.wallCollider != null) Assert.That(part.wallCollider.gameObject, Is.SameAs(part.target));
                    }
                    float width = entry.width, height = entry.height;
                    Vector3 spawn = entry.box.spawnPoint.position;
                    entry.width += 2; entry.height += 3; control.ApplySizes(); control.ApplySizes();
                    entry.width = width; entry.height = height; control.ApplySizes();
                    Assert.That(Vector3.Distance(entry.box.spawnPoint.position, spawn), Is.LessThan(.001f));
                }
                verified++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Assert.That(verified, Is.EqualTo(8));
    }
}
