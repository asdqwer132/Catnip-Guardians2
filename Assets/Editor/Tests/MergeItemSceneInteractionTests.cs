using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public class MergeItemSceneInteractionTests
{
    private readonly List<Object> owned = new List<Object>();
    private MergegameManager board;
    private MergeItem definition;
    private GameObject Host(string name, bool active = true)
    {
        var value = new GameObject(name); value.SetActive(active); owned.Add(value); return value;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
        definition = Asset<MergeItem>(); definition.item = Asset<ItemData>(); definition.radius = .1f;
        definition.item.name = "Selection material";
        var prefab = Host("Piece prefab", false); prefab.AddComponent<MergeItemObject>();
        prefab.GetComponent<Rigidbody2D>().gravityScale = 0;
        prefab.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem); definition.prefab = prefab;
        var host = Host("Board", false); host.transform.position = new Vector3(30000, 30000, 0);
        var pool = host.AddComponent<ObjectPoolManager>(); pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = host.transform;
        board = host.AddComponent<MergegameManager>(); board.objectPool = pool;
        var point = Host("Inlet"); point.transform.SetParent(host.transform, false); board.spawnPoint = point.transform;
        board.dropInterval = board.dropOffset = board.resultMergeDelay = 0; board.prewarmCount = 0; board.logActions = false;
        var toggleHost = Host("Material toggle"); toggleHost.AddComponent<RectTransform>();
        var ui = toggleHost.AddComponent<MergeUI>(); ui.toggle = toggleHost.AddComponent<Toggle>(); ui.toggle.isOn = true;
        board.mergeMaterials.Add(new MergeItemMaterial(definition, ui, 20, 0)); host.SetActive(true);
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        yield return new ExitPlayMode();
    }
    private MergeItemObject Drop(Vector2 local)
    {
        board.spawnPoint.localPosition = local;
        Assert.That(board.TryDropCurrent(), Is.True);
        Physics2D.SyncTransforms();
        return board.ActiveItems[board.ActiveItems.Count - 1];
    }
    private MergeItemSelection Selection()
    {
        var value = board.gameObject.AddComponent<MergeItemSelection>(); value.board = board;
        value.maximumDragWidth = value.maximumDragHeight = 10; value.clickCooldown = value.dragCooldown = 0;
        value.logSelections = false; return value;
    }
    private MergeDropZone Zone()
    {
        var host = Host("Drop zone"); host.transform.SetParent(board.transform, false);
        var zone = host.AddComponent<MergeDropZone>(); board.dropZone = zone;
        var line = Host("Drop Limit Line"); line.transform.SetParent(host.transform, false);
        zone.lineSprite = line.AddComponent<SpriteRenderer>();
        zone.lineSprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/AdventureUI/Map/Line.png");
        zone.lineSprite.drawMode = SpriteDrawMode.Tiled; zone.lineSprite.size = new Vector2(4, .04f);
        return zone;
    }

    private static IEnumerator WaitForGameTime(float seconds)
    {
        // EditMode에서 Play로 진입한 검사는 요청한 대기 시간 대신 실제 투입 시계의 경과를 확인합니다.
        float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 3f;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until), "The game clock did not advance.");
    }

    [UnityTest]
    public IEnumerator CountdownReadsTheRealFeederAndPauseDoesNotConsumeMaterials()
    {
        board.dropInterval = 1; board.SetAutomaticDrop(true);
        var host = Host("Authored timer"); host.AddComponent<RectTransform>();
        var text = host.AddComponent<TextMeshProUGUI>(); var timer = host.AddComponent<MergeDropTimerUI>();
        timer.board = board; timer.timeText = text;
        float before = board.DropCountdown;
        yield return new WaitForSeconds(.06f);
        timer.Refresh();
        Assert.That(board.DropCountdown, Is.LessThan(before));
        Assert.That(board.NextDropItem, Is.SameAs(definition.item));
        Assert.That(text.text, Does.EndWith("s"));
        board.SetFeeding(false);
        yield return new WaitForSeconds(.03f);
        Assert.That(board.DropCountdown, Is.EqualTo(1f).Within(.001f));
        Assert.That(board.mergeMaterials[0].count, Is.EqualTo(20));
    }

    [UnityTest]
    public IEnumerator MaterialToggleTransitionsStopStartAndPreserveTheTimerAsRequested()
    {
        board.dropInterval = 10; board.SetAutomaticDrop(true);
        var firstToggle = board.mergeMaterials[0].mergeUI.toggle;
        firstToggle.isOn = false;
        yield return WaitForGameTime(.06f);
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f), "No selected toggle must keep the full initial time.");
        Assert.That(board.ActiveItems, Is.Empty);
        firstToggle.isOn = true;
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f));
        yield return WaitForGameTime(.06f);
        float running = board.DropCountdown;
        Assert.That(running, Is.LessThan(9.99f));

        var host = Host("Additional material toggle"); host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeUI>(); ui.toggle = host.AddComponent<Toggle>(); ui.toggle.isOn = false;
        board.mergeMaterials.Add(new MergeItemMaterial(definition, ui, 20, 0)); board.RefreshUI();
        ui.toggle.isOn = true;
        Assert.That(board.DropCountdown, Is.EqualTo(running).Within(.001f), "Adding a selection must not reset the running timer.");
        firstToggle.isOn = false;
        Assert.That(board.DropCountdown, Is.EqualTo(running).Within(.001f), "One remaining selection must keep the timer.");
        ui.toggle.isOn = false;
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f));
        yield return WaitForGameTime(.04f);
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f));
        ui.toggle.isOn = true;
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f));
    }

    [UnityTest]
    public IEnumerator DeselectingAndReselectingWithinOneFrameStillRestartsTheTimer()
    {
        board.dropInterval = 10; board.SetAutomaticDrop(true);
        yield return WaitForGameTime(.06f);
        Assert.That(board.DropCountdown, Is.LessThan(9.99f));
        var toggle = board.mergeMaterials[0].mergeUI.toggle;
        toggle.isOn = false; toggle.isOn = true;
        Assert.That(board.DropCountdown, Is.EqualTo(10).Within(.001f), "The zero-selection transition must not depend on Update polling.");
    }

    [Test]
    public void DragDoesNotPublishItemsUntilConfirmationAndCancellationKeepsTheLastConfirmedSnapshot()
    {
        var first = Drop(new Vector2(-.5f, -2)); var second = Drop(new Vector2(.5f, -2));
        var selection = Selection(); Vector2 start = first.Position - Vector2.one * .25f;
        Assert.That(selection.TryBeginSelection(start), Is.True);
        selection.UpdateSelection(second.Position + Vector2.one * .25f);
        Assert.That(selection.SelectedItems, Is.Empty);
        Assert.That(first.IsLive && second.IsLive, Is.True, "Preview must not return pooled objects.");
        Assert.That(selection.CompleteSelection(second.Position + Vector2.one * .25f), Is.EqualTo(2));
        Assert.That(board.ActiveItems, Is.Empty);
        Assert.That(selection.SelectedItems.Select(i => i.itemData), Is.All.SameAs(definition.item));
        Assert.That(selection.CompleteSelection(second.Position), Is.Zero, "The same mouse-up must not consume twice.");
        selection.CancelSelection(); Assert.That(selection.SelectedItems.Count, Is.EqualTo(2));
    }

    [Test]
    public void OnlyConfirmedSelectionsShowIconsAndTheAuthoredPrefabIsReused()
    {
        var piece = Drop(new Vector2(0, -2)); var selection = Selection();
        var template = Host("Icon prefab", false); template.AddComponent<RectTransform>();
        var icon = template.AddComponent<MergeSelectedItemIcon>(); icon.icon = template.AddComponent<Image>();
        var host = Host("Authored selected area", false); host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeSelectedItemsUI>(); ui.selection = selection; ui.content = host.transform; ui.iconPrefab = icon;
        host.SetActive(true);
        selection.TryBeginSelection(piece.Position);
        Assert.That(host.transform.childCount, Is.Zero);
        selection.CancelSelection(); Assert.That(host.transform.childCount, Is.Zero); Assert.That(piece.IsLive, Is.True);
        selection.TryBeginSelection(piece.Position);
        selection.CompleteSelection(piece.Position);
        Assert.That(host.transform.childCount, Is.EqualTo(1)); var reused = host.transform.GetChild(0);
        ui.Hide(); Assert.That(reused.gameObject.activeSelf, Is.False);
        piece = Drop(new Vector2(0, -2));
        selection.TryBeginSelection(piece.Position); selection.CompleteSelection(piece.Position);
        Assert.That(host.transform.childCount, Is.EqualTo(1)); Assert.That(reused.gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator ConfirmedIconsExpireInRealTimeAndRefreshCannotShowAnExpiredSelectionAgain()
    {
        var piece = Drop(new Vector2(0, -2)); var selection = Selection();
        var template = Host("Icon prefab", false); template.AddComponent<RectTransform>();
        var icon = template.AddComponent<MergeSelectedItemIcon>(); icon.icon = template.AddComponent<Image>();
        var host = Host("Selected area", false); host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeSelectedItemsUI>();
        ui.selection = selection; ui.content = host.transform; ui.iconPrefab = icon; ui.displayDuration = .04f;
        host.SetActive(true);
        selection.TryBeginSelection(piece.Position); selection.CompleteSelection(piece.Position);
        Assert.That(host.transform.GetChild(0).gameObject.activeSelf, Is.True);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.08f);
        ui.Refresh(); Assert.That(host.transform.GetChild(0).gameObject.activeSelf, Is.False);
        host.SetActive(false); host.SetActive(true);
        Assert.That(host.transform.GetChild(0).gameObject.activeSelf, Is.False);
        Assert.That(selection.SelectedItems.Count, Is.EqualTo(1), "Expiry hides presentation without consuming or sending another item.");
    }

    [UnityTest]
    public IEnumerator IdenticalConfirmedSelectionsRestartTheDisplayTimeAndCallTheEffectOnlyOnceEach()
    {
        var piece = Drop(new Vector2(0, -2)); var selection = Selection();
        var template = Host("Icon prefab", false); template.AddComponent<RectTransform>();
        var icon = template.AddComponent<MergeSelectedItemIcon>(); icon.icon = template.AddComponent<Image>();
        var host = Host("Selected area", false); host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeSelectedItemsUI>();
        ui.selection = selection; ui.content = host.transform; ui.iconPrefab = icon; ui.displayDuration = .3f;
        ui.selectionEffect = Host("Authored effects area").AddComponent<MergeSelectedItemsEffect>();
        host.SetActive(true);
        int calls = 0;
        Application.LogCallback capture = (message, stack, type) => { if (message.StartsWith("[MergeSelectedItemsEffect]")) calls++; };
        Application.logMessageReceived += capture;
        try
        {
            selection.TryBeginSelection(piece.Position); selection.UpdateSelection(piece.Position);
            Assert.That(calls, Is.Zero); Assert.That(host.transform.childCount, Is.Zero);
            selection.CompleteSelection(piece.Position);
            Assert.That(calls, Is.EqualTo(1));
            ui.Refresh(); selection.CancelSelection();
            yield return new WaitForSecondsRealtime(.2f);
            piece = Drop(new Vector2(0, -2));
            selection.TryBeginSelection(piece.Position); selection.CompleteSelection(piece.Position);
            Assert.That(calls, Is.EqualTo(2)); Assert.That(host.transform.childCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.2f);
            ui.Refresh(); Assert.That(host.transform.GetChild(0).gameObject.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(.15f);
            ui.Refresh(); Assert.That(host.transform.GetChild(0).gameObject.activeSelf, Is.False);
            Assert.That(calls, Is.EqualTo(2));
        }
        finally { Application.logMessageReceived -= capture; }
    }

    [Test]
    public void CrossingTheLineAnywhereBlocksTheInletWithoutSpendingStockAndClearsWhenBelow()
    {
        var piece = Drop(new Vector2(0, .45f)); var zone = Zone();
        zone.lineSprite.transform.localPosition = Vector3.up * .5f;
        board.spawnPoint.localPosition = new Vector2(1, 2);
        int stock = board.mergeMaterials[0].count;
        Assert.That(board.TryDropCurrent(), Is.False); Assert.That(board.mergeMaterials[0].count, Is.EqualTo(stock));
        piece.body.position = board.transform.TransformPoint(Vector3.zero); Physics2D.SyncTransforms();
        Assert.That(zone.IsBlocked(board), Is.False);
        Assert.That(board.TryDropCurrent(), Is.True);
        Assert.That(board.mergeMaterials[0].count, Is.EqualTo(stock - 1));
    }

    [Test]
    public void ItemsAboveTheLineBlockUntilRemovedButItemsOutsideTheLineSpanDoNot()
    {
        var piece = Drop(new Vector2(0, 5)); var zone = Zone();
        Assert.That(zone.IsBlocked(board), Is.True);
        piece.body.position = board.transform.TransformPoint(new Vector3(5, 5)); Physics2D.SyncTransforms();
        Assert.That(zone.IsBlocked(board), Is.False);
        piece.body.position = board.transform.TransformPoint(new Vector3(0, 5)); Physics2D.SyncTransforms();
        piece.ReturnToPool(); Assert.That(zone.IsBlocked(board), Is.False);
    }

    [Test]
    public void MovingAndScalingTheBoxLineObjectChangesBothItsWorldLineAndTheDropLimit()
    {
        var piece = Drop(new Vector2(0, .3f)); var zone = Zone();
        var child = zone.lineSprite.gameObject;
        child.transform.localPosition = Vector3.up; zone.RefreshLine();
        Assert.That(zone.IsBlocked(board), Is.False);
        child.transform.localPosition = Vector3.up * .2f; zone.RefreshLine();
        Assert.That(zone.IsBlocked(board), Is.True);
        Assert.That(zone.lineSprite.localBounds.size.x, Is.EqualTo(4).Within(.001f));
        piece.body.position = board.transform.TransformPoint(new Vector3(1.5f, 1, 0)); Physics2D.SyncTransforms();
        child.transform.localScale = new Vector3(.5f, 1, 1);
        Assert.That(zone.IsBlocked(board), Is.False);
        zone.lineSprite.size = new Vector2(8, .04f);
        Assert.That(zone.IsBlocked(board), Is.True, "Resizing a tiled sprite must resize the actual limit too.");
        child.transform.localScale = new Vector3(2, 1, 1);
        Assert.That(zone.IsBlocked(board), Is.True);
        zone.showWorldLine = false; zone.RefreshLine();
        Assert.That(zone.lineSprite.enabled, Is.False); Assert.That(zone.IsBlocked(board), Is.True);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void InletDragClampsEnabledAxesInTheScaledRotatedBoxAndKeepsTheGrabOffset(bool x, bool y)
    {
        var zone = Zone(); zone.transform.localScale = new Vector3(2, .5f, 1);
        zone.transform.localRotation = Quaternion.Euler(0, 0, 20);
        zone.movementArea = new Rect(-2, -1, 4, 2);
        board.spawnPoint.position = zone.transform.TransformPoint(new Vector3(.2f, .3f, 0));
        var dragger = board.gameObject.AddComponent<MergeSpawnPointDragger>(); dragger.board = board; dragger.moveX = x; dragger.moveY = y;
        Vector3 pointer = zone.transform.TransformPoint(new Vector3(.3f, .3f, 0));
        Assert.That(dragger.TryBeginDrag(pointer), Is.True);
        dragger.DragTo(pointer);
        Assert.That(zone.transform.InverseTransformPoint(board.spawnPoint.position).x, Is.EqualTo(.2f).Within(.02f), "Picking up the handle must not snap it to the pointer.");
        dragger.DragTo(zone.transform.TransformPoint(new Vector3(100, 100, 0)));
        Vector3 local = zone.transform.InverseTransformPoint(board.spawnPoint.position);
        Assert.That(local.x, Is.EqualTo(x ? 2 : .2f).Within(.02f)); Assert.That(local.y, Is.EqualTo(y ? 1 : .3f).Within(.02f));
    }

    [Test]
    public void InletDraggingCancelsAnItemGestureAndCannotAlsoStartItemSelection()
    {
        Drop(new Vector2(0, -2)); var selection = Selection(); var zone = Zone();
        board.spawnPoint.position = zone.transform.TransformPoint(new Vector3(0, 2, 0));
        var dragger = board.gameObject.AddComponent<MergeSpawnPointDragger>(); dragger.board = board; dragger.selection = selection;
        selection.spawnPointDragger = dragger;
        selection.TryBeginSelection(board.transform.TransformPoint(new Vector3(0, -2, 0)));
        Assert.That(dragger.TryBeginDrag(board.spawnPoint.position), Is.True);
        Assert.That(selection.IsSelecting, Is.False); Assert.That(selection.SelectedItems, Is.Empty);
        Assert.That(selection.TryBeginSelection(board.spawnPoint.position), Is.False);
        dragger.EndDrag();
        Assert.That(selection.TryBeginSelection(board.transform.TransformPoint(new Vector3(0, -2, 0))), Is.True);
    }

    [Test]
    [Category("RequiresFocusedGameView")]
    public void MouseInputDragsTheInletBeyondTheViewportWithoutSelectingOrRemovingAnItem()
    {
        if (!Application.isFocused)
            Assert.Ignore("Real pointer callbacks require a focused Game View; batchmode has no application focus. Axis limits and selection exclusion are tested separately.");
        var piece = Drop(new Vector2(0, -2)); var selection = Selection(); var zone = Zone();
        zone.movementArea = new Rect(-2, -1, 4, 2); board.spawnPoint.localPosition = Vector3.zero;
        var camera = Host("Pointer camera").AddComponent<Camera>();
        camera.transform.position = board.transform.position + Vector3.back * 10;
        camera.orthographic = true; camera.orthographicSize = 5; camera.pixelRect = new Rect(0, 0, 800, 800);
        selection.inputCamera = camera;
        var dragger = board.gameObject.AddComponent<MergeSpawnPointDragger>();
        dragger.board = board; dragger.inputCamera = camera; dragger.selection = selection;
        selection.spawnPointDragger = dragger;
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            Vector2 start = camera.WorldToScreenPoint(board.spawnPoint.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }); InputSystem.Update();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left)); InputSystem.Update();
            Assert.That(dragger.IsDragging, Is.True, $"focus={Application.isFocused}, screen={start}, mouse={mouse.position.ReadValue()}, left={mouse.leftButton.isPressed}, hit={dragger.IsHandleAtScreen(start)}, viewport={camera.pixelRect}");
            Assert.That(selection.IsSelecting, Is.False);
            Vector2 outside = camera.WorldToScreenPoint(board.transform.position + new Vector3(100, 100, 0));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = outside }.WithButton(MouseButton.Left)); InputSystem.Update();
            Assert.That(dragger.IsDragging, Is.True);
            Assert.That(board.spawnPoint.localPosition.x, Is.EqualTo(2).Within(.01f));
            Assert.That(board.spawnPoint.localPosition.y, Is.EqualTo(0).Within(.01f));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = outside }); InputSystem.Update();
            Assert.That(dragger.IsDragging, Is.False); Assert.That(piece.IsLive, Is.True);
            Assert.That(selection.LastRemovedCount, Is.Zero);
        }
        finally { InputSystem.RemoveDevice(mouse); }
    }
}

public class MergeItemSceneAuthoringTests
{
    [Test]
    public void ItemSceneConnectsAuthoredTimerSelectedPrefabAndEveryBoxWithOnlyOneSelectionInput()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(r => r.GetComponentsInChildren<MergeBoxSceneController>(true)).Single();
        var timer = roots.SelectMany(r => r.GetComponentsInChildren<MergeDropTimerUI>(true)).Single();
        Assert.That(timer.boxes, Is.SameAs(controller)); Assert.That(timer.timeText, Is.Not.Null); Assert.That(timer.nextItemIcon, Is.Not.Null);
        var selectionUI = roots.SelectMany(r => r.GetComponentsInChildren<MergeSelectedItemsUI>(true)).Single();
        Assert.That(selectionUI.selection, Is.SameAs(controller.selection)); Assert.That(selectionUI.content, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(selectionUI.iconPrefab), Is.EqualTo("Assets/Prefab/Merge/Selected Item Icon.prefab"));
        Assert.That(selectionUI.displayDuration, Is.EqualTo(2f));
        Assert.That(selectionUI.selectionEffect, Is.Not.Null);
        Assert.That(selectionUI.selectionEffect.transform.parent, Is.SameAs(selectionUI.content));
        Assert.That(selectionUI.selectionEffect.GetComponent<LayoutElement>().ignoreLayout, Is.True);
        Assert.That(selectionUI.selectionEffect.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        Assert.That(controller.selection.spawnPointDragger, Is.Not.Null);
        Assert.That(controller.selection.spawnPointDragger.inputCamera, Is.SameAs(controller.selection.inputCamera));
        Assert.That(controller.selection.dragAllowedArea.Find("Drop Limit Line"), Is.Null);
        foreach (var entry in controller.boxes)
        {
            var bindings = entry.boxRoot.GetComponentInChildren<MergeBoxBindings>(true);
            Assert.That(bindings.dropZone, Is.Not.Null); Assert.That(bindings.dropZone.lineSprite != null, Is.True);
            Assert.That(bindings.dropZone.spawnMarker, Is.Not.Null); Assert.That(entry.board.dropZone, Is.SameAs(bindings.dropZone));
            Assert.That(bindings.dropZone.showWorldLine, Is.True);
            Assert.That(bindings.dropZone.lineSprite.transform.parent, Is.SameAs(bindings.transform));
            Assert.That(bindings.dropZone.lineSprite.name, Is.EqualTo("Drop Limit Line"));
            Assert.That(bindings.dropZone.lineSprite, Is.SameAs(bindings.transform.Find("Drop Limit Line").GetComponent<SpriteRenderer>()));
            Assert.That(bindings.dropZone.GetComponentsInChildren<LineRenderer>(true), Is.Empty);
            Assert.That(bindings.dropZone.lineSprite.GetComponent<Canvas>(), Is.Null);
            Assert.That(bindings.dropZone.lineSprite.sprite, Is.Not.Null);
            Assert.That(bindings.dropZone.lineSprite.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(bindings.dropZone.lineSprite.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        }
        Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<MergeItemSelection>(true)).Count(s => s.enabled), Is.EqualTo(1));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
