using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MergeBoxVisualLayoutTests
{
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
