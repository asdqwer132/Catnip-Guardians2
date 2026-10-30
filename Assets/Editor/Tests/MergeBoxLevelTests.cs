using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public abstract class MergeBoxLevelTestFixture
{
    private readonly List<Object> owned = new List<Object>();
    protected GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name); host.SetActive(active); owned.Add(host); return host;
    }
    protected T Asset<T>() where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    protected MergeBoxBindings Box(Transform root, string name)
    {
        var host = Host(name); host.transform.SetParent(root, false);
        var bindings = host.AddComponent<MergeBoxBindings>();
        var point = Host("Inlet"); point.transform.SetParent(host.transform, false); point.transform.localPosition = Vector3.up * 2;
        bindings.spawnPoint = point.transform; bindings.dropZone = host.AddComponent<MergeDropZone>();
        var line = Host("Drop limit"); line.transform.SetParent(host.transform, false); line.transform.localPosition = Vector3.up * 1.5f;
        bindings.dropZone.lineSprite = line.AddComponent<SpriteRenderer>();
        bindings.dropZone.lineSprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/AdventureUI/Map/Line.png");
        bindings.dropZone.lineSprite.drawMode = SpriteDrawMode.Tiled; bindings.dropZone.lineSprite.size = new Vector2(4, .125f);
        var wall = Host("Floor"); wall.transform.SetParent(host.transform, false); wall.transform.localPosition = Vector3.down * 2;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(4, .1f);
        return bindings;
    }
    protected MergeBoxLevelManager Manager(ItemSeries series, params int[] thresholds)
    {
        var host = Host("Series " + series, false); var manager = host.AddComponent<MergeBoxLevelManager>();
        manager.levels = thresholds.Select((level, i) => new MergeBoxLevel { minimumLevel = level, box = Box(host.transform, "Variant " + i) }).ToArray();
        var boardHost = Host("Board " + series, false); var board = boardHost.AddComponent<MergegameManager>();
        board.itemSeries = series; board.logActions = false; board.prewarmCount = 0;
        manager.BindBoard(board); host.SetActive(true); return manager;
    }
    [SetUp] public void SetUp()
    {
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    [TearDown] public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

}

public class MergeBoxLevelTests : MergeBoxLevelTestFixture
{
    [Test]
    public void UnsortedThresholdsSelectOnlyTheHighestUnlockedBoxAndItsOwnBindings()
    {
        var manager = Manager(ItemSeries.Weapon, 5, 1, 3);
        Assert.That(manager.CurrentBox, Is.SameAs(manager.levels[1].box));
        Assert.That(manager.TrySetLevel(4), Is.True);
        Assert.That(manager.CurrentLevel, Is.EqualTo(4)); Assert.That(manager.ActiveBoxLevel, Is.EqualTo(3));
        Assert.That(manager.CurrentBox, Is.SameAs(manager.levels[2].box));
        Assert.That(manager.Board.spawnPoint, Is.SameAs(manager.CurrentBox.spawnPoint));
        Assert.That(manager.Board.dropZone, Is.SameAs(manager.CurrentBox.dropZone));
        foreach (var entry in manager.levels)
            Assert.That(entry.box.GetComponentInChildren<Collider2D>(true).gameObject.activeInHierarchy, Is.EqualTo(entry.box == manager.CurrentBox));
        Assert.That(manager.TrySetLevel(100), Is.True); Assert.That(manager.ActiveBoxLevel, Is.EqualTo(5));
        Assert.That(manager.TrySetLevel(0), Is.True); Assert.That(manager.CurrentLevel, Is.EqualTo(1));
    }
    [Test]
    public void UpgradeUsesTheNextConfiguredThresholdAndDoesNothingAtTheLastBox()
    {
        var manager = Manager(ItemSeries.Food, 1, 4, 10);
        int changes = 0; manager.BoxChanged += _ => changes++;
        Assert.That(manager.TryUpgrade(), Is.True); Assert.That(manager.CurrentLevel, Is.EqualTo(4));
        manager.SetLevel(7); Assert.That(changes, Is.EqualTo(1), "A level change within the same variant must not interrupt selection.");
        Assert.That(manager.TryUpgrade(), Is.True); Assert.That(manager.CurrentLevel, Is.EqualTo(10));
        Assert.That(manager.TryUpgrade(), Is.False); Assert.That(changes, Is.EqualTo(2));
        Assert.That(manager.CurrentBox, Is.SameAs(manager.levels[2].box));
    }
    [Test]
    public void EightSeriesKeepIndependentLevelsRegardlessOfDropdownOrder()
    {
        var managers = Enumerable.Range(1, 8).Select(i => Manager((ItemSeries)i, 1, 2, 3)).Reverse().ToArray();
        var controller = Host("Controller", false).AddComponent<MergeBoxSceneController>();
        controller.boxes = managers.Select(m => new MergeBoxEntry { board = m.Board, boxRoot = m.transform, levelManager = m }).ToArray();
        Assert.That(controller.SetBoxLevel(ItemSeries.Mineral, 3), Is.True);
        foreach (var manager in managers)
            Assert.That(manager.CurrentLevel, Is.EqualTo(manager.Board.itemSeries == ItemSeries.Mineral ? 3 : 1));
        controller.SelectBox(Array.FindIndex(controller.boxes, entry => entry.board.itemSeries == ItemSeries.Food));
        controller.UpgradeCurrentBox(); Assert.That(controller.GetBoxManager(ItemSeries.Food).CurrentLevel, Is.EqualTo(2));
        controller.SetCurrentBoxLevel(1); Assert.That(controller.GetBoxManager(ItemSeries.Food).CurrentLevel, Is.EqualTo(1));
        Assert.That(controller.GetBoxManager(ItemSeries.Mineral).CurrentLevel, Is.EqualTo(3));
        Assert.That(controller.SetBoxLevel(ItemSeries.None, 2), Is.False);
    }
    [Test]
    public void RebindingAfterSelectingAHigherLevelUsesThatVariantsInletAndLimit()
    {
        var manager = Manager(ItemSeries.Magic, 1, 2);
        manager.BindBoard(null); manager.SetLevel(2);
        var replacement = Host("Replacement board", false).AddComponent<MergegameManager>();
        manager.BindBoard(replacement);
        Assert.That(replacement.spawnPoint, Is.SameAs(manager.levels[1].box.spawnPoint));
        Assert.That(replacement.dropZone, Is.SameAs(manager.levels[1].box.dropZone));
        Assert.That(manager.levels[0].box.gameObject.activeSelf, Is.False);
    }
    [Test]
    public void InvalidForeignOrNestedBoxesDoNotDisableAnotherSeriesOrChangeTheCurrentState()
    {
        var a = Manager(ItemSeries.Weapon, 1, 2); var b = Manager(ItemSeries.Food, 1, 2);
        var original = a.CurrentBox; var originalPoint = a.Board.spawnPoint;
        a.levels[1].box = b.levels[1].box;
        Assert.That(a.TrySetLevel(2), Is.False); Assert.That(a.CurrentBox, Is.SameAs(original));
        Assert.That(a.Board.spawnPoint, Is.SameAs(originalPoint)); Assert.That(b.CurrentBox.gameObject.activeSelf, Is.True);
        a.levels[1].box = Box(original.transform, "Nested variant");
        Assert.That(a.TrySetLevel(2), Is.False); Assert.That(original.gameObject.activeSelf, Is.True);
        a.levels = Array.Empty<MergeBoxLevel>(); Assert.That(a.TryUpgrade(), Is.False); Assert.That(a.TrySetLevel(2), Is.False);
    }
    [Test]
    public void EveryAuthoredSeriesPrefabAndSceneEntryHasItsOwnLevelManager()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single);
        var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MergeBoxSceneController>(true)).Single();
        Assert.That(controller.boxes.Length, Is.EqualTo(8));
        var unique = new HashSet<MergeBoxLevelManager>(); var series = new HashSet<ItemSeries>();
        foreach (var entry in controller.boxes)
        {
            Assert.That(entry.levelManager != null, Is.True); Assert.That(unique.Add(entry.levelManager), Is.True);
            Assert.That(series.Add(entry.board.itemSeries), Is.True);
            Assert.That(entry.levelManager.transform, Is.SameAs(entry.boxRoot));
            Assert.That(entry.levelManager.levels.Length, Is.GreaterThan(0));
            var box = entry.levelManager.levels[0].box;
            Assert.That(box.transform.IsChildOf(entry.boxRoot), Is.True);
            entry.levelManager.BindBoard(entry.board);
            Assert.That(entry.board.spawnPoint, Is.SameAs(box.spawnPoint)); Assert.That(entry.board.dropZone, Is.SameAs(box.dropZone));
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(entry.boxRoot.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab.GetComponent<MergeBoxLevelManager>().levels[0].box != null, Is.True);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}

public class MergeBoxLevelPlayTests : MergeBoxLevelTestFixture
{
    [UnitySetUp] public IEnumerator Enter()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }
    [UnityTearDown] public IEnumerator Exit() { yield return new ExitPlayMode(); }

    [UnityTest]
    public IEnumerator UpgradePreservesPooledItemsStockAndTimerAndCancelsOldBoxGestures()
    {
        Time.timeScale = 1;
        var manager = Manager(ItemSeries.Weapon, 1, 5);
        manager.transform.position = new Vector3(30000, 30000, 0);
        var board = manager.Board; board.transform.position = manager.transform.position;
        var pool = board.gameObject.AddComponent<ObjectPoolManager>(); pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = board.transform; board.objectPool = pool;
        var item = Asset<MergeItem>(); item.item = Asset<ItemData>(); item.radius = .1f;
        var prefab = Host("Piece prefab", false); prefab.AddComponent<MergeItemObject>();
        prefab.GetComponent<Rigidbody2D>().gravityScale = 0; prefab.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem); item.prefab = prefab;
        var uiHost = Host("Material", false); uiHost.AddComponent<RectTransform>();
        var ui = uiHost.AddComponent<MergeUI>(); ui.toggle = uiHost.AddComponent<Toggle>(); ui.toggle.isOn = true;
        var material = new MergeItemMaterial(item, ui, 20, 3); board.mergeMaterials.Add(material);
        board.dropInterval = 10; board.dropOffset = 0; board.automaticDrop = true; board.gameObject.SetActive(true);
        var selection = board.gameObject.AddComponent<MergeItemSelection>(); selection.board = board; selection.logSelections = false;
        var dragger = board.gameObject.AddComponent<MergeSpawnPointDragger>(); dragger.board = board; dragger.selection = selection; selection.spawnPointDragger = dragger;
        var controllerHost = Host("Controller", false); var controller = controllerHost.AddComponent<MergeBoxSceneController>();
        controller.selection = selection; controller.boxes = new[] { new MergeBoxEntry { boxRoot = manager.transform, board = board, levelManager = manager } };
        controllerHost.SetActive(true);
        Assert.That(board.TryDropCurrent(), Is.True);
        var piece = board.ActiveItems.Single(); piece.body.position = manager.CurrentBox.transform.position; piece.transform.position = piece.body.position;
        piece.Velocity = new Vector2(1, 2); int life = piece.LifeId;
        var second = manager.levels[1].box; second.transform.localPosition = new Vector3(5, 2, 0); second.transform.localScale = Vector3.one * 1.5f;
        var expectedPosition = piece.transform.position; var expectedVelocity = piece.Velocity;
        var authoredBoxPosition = second.transform.localPosition; var authoredBoxScale = second.transform.localScale;
        int stock = material.count, bonus = material.defaultCount; float countdown = board.DropCountdown;
        Assert.That(dragger.TryBeginDrag(board.spawnPoint.position), Is.True);
        Assert.That(controller.UpgradeBox(ItemSeries.Weapon), Is.True);
        Assert.That(dragger.IsDragging, Is.False);
        Assert.That(board.spawnPoint, Is.SameAs(second.spawnPoint)); Assert.That(board.dropZone, Is.SameAs(second.dropZone));
        Assert.That(second.transform.localPosition, Is.EqualTo(authoredBoxPosition)); Assert.That(second.transform.localScale, Is.EqualTo(authoredBoxScale));
        Assert.That(board.ActiveItems.Single(), Is.SameAs(piece)); Assert.That(piece.IsLive, Is.True); Assert.That(piece.LifeId, Is.EqualTo(life));
        Assert.That(Vector3.Distance(piece.transform.position, expectedPosition), Is.LessThan(.01f));
        Assert.That(Vector2.Distance(piece.Velocity, expectedVelocity), Is.LessThan(.001f));
        Assert.That(material.count, Is.EqualTo(stock)); Assert.That(material.defaultCount, Is.EqualTo(bonus));
        Assert.That(board.DropCountdown, Is.EqualTo(countdown).Within(.001f)); Assert.That(board.isFeeding, Is.True);
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True);
        controller.SetCurrentBoxLevel(1); Assert.That(selection.IsSelecting, Is.False); Assert.That(piece.IsLive, Is.True);
        // 씬 컨트롤러가 없어도 이전 투입구의 드래그가 새 투입구를 움직이지 않는지 확인합니다.
        controller.enabled = false;
        Assert.That(dragger.TryBeginDrag(board.spawnPoint.position), Is.True);
        manager.SetLevel(5); Vector3 point = board.spawnPoint.position;
        dragger.DragTo(point + Vector3.right); Assert.That(dragger.IsDragging, Is.False); Assert.That(board.spawnPoint.position, Is.EqualTo(point));
        yield return null;
    }
}
