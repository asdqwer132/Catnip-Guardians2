using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// 풀/컴포넌트의 실제 상태를 검사합니다. 접촉 알림은 직접 전달하며 실제 낙하는 Play에서 확인합니다.
public sealed class MergeGameTests
{
    private GameObject host;
    private MergegameManager manager;
    private ObjectPoolManager pool, previousDefault;
    private MergeItem a, b, result;
    private float previousTimeScale;
    private readonly List<Object> assets = new List<Object>();
    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); assets.Add(value); return value; }
    private MergeItem Definition(string name, float radius)
    {
        var item = Asset<ItemData>(); item.name = name;
        var texture = new Texture2D(2, 4); assets.Add(texture);
        item.icon = Sprite.Create(texture, new Rect(0f, 0f, 2f, 4f), new Vector2(.5f, .5f), 1f); assets.Add(item.icon);
        var definition = Asset<MergeItem>(); definition.item = item; definition.radius = radius; return definition;
    }
    [SetUp]
    public void Setup()
    {
        previousDefault = ObjectPoolManager.instance;
        previousTimeScale = Time.timeScale; Time.timeScale = 1f;
        host = new GameObject("Merge game test"); host.SetActive(false);
        host.transform.position = new Vector3(45000f, 45000f, 0f);
        pool = host.AddComponent<ObjectPoolManager>(); pool.registerAsDefaultPool = false;
        pool.poolParent = host.transform;
        var board = new GameObject("Manager"); board.transform.SetParent(host.transform, false);
        manager = board.AddComponent<MergegameManager>(); manager.acceptKeyboardInput = false; manager.logActions = false;
        manager.objectPool = pool; manager.dropInterval = 0f; manager.resultMergeDelay = 0f; manager.prewarmCount = 0;
        pool.mergeItemActiveParent = manager.transform; pool.mergeItemPoolParent = host.transform;
        var point = new GameObject("Spawn point"); point.transform.SetParent(manager.transform, false);
        manager.spawnPoint = point.transform;
        var prefab = new GameObject("Common merge item"); prefab.SetActive(false); prefab.transform.SetParent(host.transform, false);
        var piece = prefab.AddComponent<MergeItemObject>();
        piece.circleCollider = prefab.GetComponent<CircleCollider2D>(); piece.body = prefab.GetComponent<Rigidbody2D>();
        prefab.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem);
        var visual = new GameObject("Visual"); visual.transform.SetParent(prefab.transform, false);
        piece.spriteRenderer = visual.AddComponent<SpriteRenderer>(); piece.visualSize = visual; manager.itemPrefab = prefab;
        a = Definition("A", .2f); b = Definition("B", .25f); result = Definition("Result", .4f);
        manager.mergeItems = new List<MergeItem> { a, b, result };
        manager.mergeMaterials = new List<MergeItemMaterial> {
            new MergeItemMaterial { MergeItem = a, count = 3 }, new MergeItemMaterial { MergeItem = b, count = 3 } };
        var recipe = Asset<ItemRecipeData>(); recipe.resultItem = result.item;
        recipe.materials = new[] { new RecipeMaterial { itemData = a.item, amount = 1 }, new RecipeMaterial { itemData = b.item, amount = 1 } };
        manager.mergeRecipe = new List<MergeSeries> { new MergeSeries { itemRecipeDatas = new List<ItemRecipeData> { recipe } } };
        host.SetActive(true);
        // EditMode에서 Awake 호출 여부에 의존하지 않고 로컬 풀 설정을 초기화합니다.
        typeof(ObjectPoolManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
    }
    [TearDown]
    public void Cleanup()
    {
        MergeSpawnProbe.callback = null;
        if (manager != null) manager.ClearItems();
        if (host != null) Object.DestroyImmediate(host);
        for (int i = assets.Count - 1; i >= 0; i--) if (assets[i] != null) Object.DestroyImmediate(assets[i]);
        assets.Clear(); ObjectPoolManager.instance = previousDefault; Time.timeScale = previousTimeScale;
    }
    private MergeItemObject Drop(int materialIndex, float x)
    {
        Assert.That(manager.SelectMaterial(materialIndex), Is.True);
        manager.spawnPoint.localPosition = new Vector3(x, 0f, 0f);
        Assert.That(manager.TryDropCurrent(), Is.True);
        return manager.ActiveItems[manager.ActiveItems.Count - 1];
    }
    [Test]
    public void DropConsumesOneMaterialAndSetsThePooledSpriteRadiusAndVisualSize()
    {
        manager.gameObject.layer = 31;
        var piece = Drop(0, -1f);
        Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(2));
        Assert.That(piece.item, Is.SameAs(a.item)); Assert.That(piece.circleCollider.radius, Is.EqualTo(.2f));
        Assert.That(piece.spriteRenderer.sprite, Is.SameAs(a.item.icon));
        Assert.That(piece.spriteRenderer.transform.localScale.x, Is.EqualTo(.05f).Within(.0001f));
        Assert.That(piece.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(piece.body.simulated, Is.True); Assert.That(piece.circleCollider.isTrigger, Is.False);
        Assert.That(piece.GetComponent<PooledObject>().OwnerPool, Is.SameAs(pool));
        Assert.That(piece.gameObject.scene, Is.EqualTo(manager.gameObject.scene));
        foreach (var child in piece.GetComponentsInChildren<Transform>(true)) Assert.That(child.gameObject.layer, Is.EqualTo(31));
        Assert.That(ObjectPoolManager.instance, Is.SameAs(previousDefault));
    }
    [Test]
    public void BlockedDropsAndMaximumActiveCountDoNotConsumeMaterials()
    {
        Drop(0, 0f); int left = manager.mergeMaterials[0].count;
        Assert.That(manager.TryDropCurrent(), Is.False); Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(left));
        manager.maximumActiveItems = 1; manager.spawnPoint.localPosition = Vector3.right;
        Assert.That(manager.TryDropCurrent(), Is.False); Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(left));
    }
    [Test]
    public void SuccessfulDropStartsTheCooldownButAFailedDropDoesNotSpendMoreStock()
    {
        manager.dropInterval = 100f; Drop(0, -1f);
        manager.spawnPoint.localPosition = Vector3.right;
        Assert.That(manager.TryDropCurrent(), Is.False); Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(2));
    }
    [Test]
    public void DisabledMergeComponentDoesNotConsumeStockOrLeaveAnActiveRental()
    {
        manager.itemPrefab.GetComponent<MergeItemObject>().enabled = false;
        manager.SelectMaterial(0);
        Assert.That(manager.TryDropCurrent(), Is.False);
        Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(3));
        Assert.That(manager.ActiveItems.Count, Is.Zero);
    }
    [Test]
    public void RegisteredMixedContactConsumesOnePairDespiteReverseAndSharedThirdContacts()
    {
        var first = Drop(0, -1f); var second = Drop(1, 0f); var third = Drop(1, 1f);
        manager.QueueContact(first, second); manager.QueueContact(second, first); manager.QueueContact(first, third);
        manager.ProcessContacts();
        Assert.That(manager.ActiveItems.Count, Is.EqualTo(2)); Assert.That(third.IsLive, Is.True);
        int results = 0; foreach (var piece in manager.ActiveItems) if (piece.item == result.item) results++;
        Assert.That(results, Is.EqualTo(1));
        Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(2)); Assert.That(manager.mergeMaterials[1].count, Is.EqualTo(1));
    }
    [Test]
    public void UnregisteredEqualItemsAndMissingResultDefinitionLeaveBothPieces()
    {
        var first = Drop(0, -1f); var same = Drop(0, 0f);
        manager.QueueContact(first, same); manager.ProcessContacts(); Assert.That(manager.ActiveItems.Count, Is.EqualTo(2));
        manager.ClearItems(); first = Drop(0, -1f); var second = Drop(1, 0f);
        manager.mergeItems.Remove(result);
        manager.QueueContact(first, second); manager.ProcessContacts();
        Assert.That(manager.ActiveItems.Count, Is.EqualTo(2)); Assert.That(first.IsLive && second.IsLive, Is.True);
    }
    [Test]
    public void ReturnedPieceIsReusedAndQueuedContactFromItsPreviousLifeIsIgnored()
    {
        var first = Drop(0, -1f); var second = Drop(1, 1f);
        manager.QueueContact(first, second); int oldLife = first.LifeId; first.ReturnToPool();
        var reused = Drop(0, -1f);
        Assert.That(reused, Is.SameAs(first)); Assert.That(reused.LifeId, Is.Not.EqualTo(oldLife));
        manager.ProcessContacts(); Assert.That(manager.ActiveItems.Count, Is.EqualTo(2));
        Assert.That(reused.item, Is.SameAs(a.item));
        manager.ClearItems(); Assert.That(reused.body.simulated, Is.False); Assert.That(reused.item, Is.Null);
    }
    [Test]
    public void SpawnCallbackCannotSpendTheSameLastMaterialAndCancellationRefundsTheReservation()
    {
        manager.mergeMaterials[0].count = 1;
        manager.itemPrefab.AddComponent<MergeSpawnProbe>();
        int attempts = 0;
        MergeSpawnProbe.callback = () => { attempts++; Assert.That(manager.TryDropCurrent(), Is.False); };
        Drop(0, -1f); Assert.That(attempts, Is.EqualTo(1)); Assert.That(manager.mergeMaterials[0].count, Is.Zero);
        manager.ClearItems(); manager.mergeMaterials[0].count = 1;
        MergeSpawnProbe.callback = () => manager.ClearItems();
        manager.SelectMaterial(0);
        Assert.That(manager.TryDropCurrent(), Is.False); Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(1));
        Assert.That(manager.ActiveItems.Count, Is.Zero);
    }
    [Test]
    public void SelectionUsesTheSharedStockEntryAndSkipsDepletedMaterials()
    {
        Assert.That(manager.SelectMaterial(0), Is.True); Assert.That(manager.currentMaterial, Is.SameAs(manager.mergeMaterials[0]));
        Assert.That(manager.CurrentMaterialName, Is.EqualTo("A")); // 언어 데이터가 없는 단독 테스트도 안전합니다.
        Assert.That(manager.SelectNextMaterial(), Is.True); Assert.That(manager.currentMaterial, Is.SameAs(manager.mergeMaterials[1]));
        manager.mergeMaterials[0].count = 0;
        manager.SelectNextMaterial(); Assert.That(manager.currentMaterial, Is.SameAs(manager.mergeMaterials[1]));
        manager.mergeMaterials[1].count = 0;
        Assert.That(manager.SelectNextMaterial(), Is.False); Assert.That(manager.CurrentMaterialCount, Is.Zero);
    }

    private MergeItemSelection Selection()
    {
        var selection = manager.gameObject.AddComponent<MergeItemSelection>();
        selection.board = manager; selection.logSelections = false;
        selection.dragCooldown = selection.clickCooldown = 0f;
        return selection;
    }

    private RectTransform AllowedArea(MergeItemSelection selection, Vector2 center)
    {
        var camera = new GameObject("Selection camera").AddComponent<Camera>(); camera.transform.SetParent(host.transform, false);
        camera.transform.position = new Vector3(center.x, center.y, -10f);
        camera.orthographic = true; camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f); camera.aspect = 1f;
        selection.inputCamera = camera;
        var area = new GameObject("Allowed area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(host.transform, false); area.position = center; area.sizeDelta = new Vector2(2f, 2f);
        selection.dragAllowedArea = area;
        return area;
    }
    [Test]
    public void LeavingAllowedAreaCancelsBeforeClampingAndReenteringDoesNotDelete()
    {
        var piece = Drop(0, -1f); var selection = Selection(); AllowedArea(selection, piece.Position);
        selection.maximumDragWidth = selection.maximumDragHeight = .1f;
        selection.dragCooldown = selection.clickCooldown = 100f;
        Assert.That(selection.TryBeginSelection(piece.Position + Vector2.right * 3f), Is.False);
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True);
        selection.UpdateSelection(piece.Position + Vector2.right * 3f); // 표시 사각형은 .1이지만 실제 포인터는 밖입니다.
        Assert.That(selection.IsSelecting, Is.False);
        selection.UpdateSelection(piece.Position);
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero);
        Assert.That(piece.IsLive, Is.True); Assert.That(selection.CooldownRemaining, Is.Zero);
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True); // 새 클릭은 즉시 가능합니다.
    }
    [Test]
    public void ReleasingOutsideTheAreaWithoutAnUpdateCannotFallThroughToClickDeletion()
    {
        var piece = Drop(0, -1f); var selection = Selection(); AllowedArea(selection, piece.Position);
        Physics2D.SyncTransforms();
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True);
        Assert.That(selection.CompleteSelection(piece.Position + Vector2.right * 3f), Is.Zero);
        Assert.That(piece.IsLive, Is.True); Assert.That(selection.IsSelecting, Is.False);
        Assert.That(selection.CooldownRemaining, Is.Zero);
    }
    [Test]
    public void DisablingAnAssignedAreaCancelsAnActiveSelection()
    {
        var piece = Drop(0, -1f); var selection = Selection();
        var area = AllowedArea(selection, piece.Position);
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True);
        area.gameObject.SetActive(false);
        selection.UpdateSelection(piece.Position);
        Assert.That(selection.IsSelecting, Is.False);
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero); Assert.That(piece.IsLive, Is.True);
    }
    [Test]
    public void AreaUsesItsCanvasCameraAndAnInsideDragStillReturnsTheItemToThePool()
    {
        var piece = Drop(0, -1f); var selection = Selection();
        var area = AllowedArea(selection, piece.Position);
        var canvas = area.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        area.position = piece.Position; area.localScale = Vector3.one; area.sizeDelta = new Vector2(4f, 4f);
        var uiCamera = new GameObject("Area UI camera").AddComponent<Camera>(); uiCamera.transform.SetParent(host.transform, false);
        uiCamera.transform.position = selection.inputCamera.transform.position;
        uiCamera.orthographic = true; uiCamera.orthographicSize = 10f;
        uiCamera.pixelRect = selection.inputCamera.pixelRect; uiCamera.aspect = 1f;
        canvas.worldCamera = uiCamera;
        Assert.That(selection.IsInsideDragArea(piece.Position + Vector2.right * .5f), Is.True);
        Assert.That(selection.IsInsideDragArea(piece.Position + Vector2.right * 1.5f), Is.False);
        Physics2D.SyncTransforms();
        Assert.That(selection.TryBeginSelection(piece.Position - Vector2.one * .5f), Is.True);
        Assert.That(selection.CompleteSelection(piece.Position + Vector2.one * .5f), Is.EqualTo(1));
        Assert.That(piece.IsLive, Is.False);
    }
    [Test]
    public void DragRemovesOnlyIntersectingLivePiecesAndReturnsThemWithoutRefundingStock()
    {
        var first = Drop(0, -1f); var edge = Drop(1, 0f); var outside = Drop(1, 1f);
        var selection = Selection(); Physics2D.SyncTransforms();
        // B의 중심은 바깥이지만 반지름 .25가 사각형 끝 -.1에 걸쳐 선택됩니다.
        Assert.That(selection.TryBeginSelection((Vector2)host.transform.position + new Vector2(-1.5f, -.3f)), Is.True);
        Assert.That(selection.CompleteSelection((Vector2)host.transform.position + new Vector2(-.1f, .3f)), Is.EqualTo(2));
        Assert.That(first.IsLive || edge.IsLive, Is.False); Assert.That(outside.IsLive, Is.True);
        Assert.That(manager.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(manager.mergeMaterials[0].count, Is.EqualTo(2)); Assert.That(manager.mergeMaterials[1].count, Is.EqualTo(1));
        Assert.That(selection.LastMessage, Does.Contain("B"));
        var reused = Drop(0, -1f);
        Assert.That(reused == first || reused == edge, Is.True); // Destroy 대신 기존 풀 재사용
    }
    [Test]
    public void ClickRemovesOnePieceAndItsCooldownAlsoBlocksStartingADrag()
    {
        var piece = Drop(0, -1f); var selection = Selection(); selection.clickCooldown = 100f;
        Physics2D.SyncTransforms();
        Assert.That(selection.TryBeginSelection(piece.Position), Is.True);
        Assert.That(selection.CompleteSelection(piece.Position), Is.EqualTo(1));
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero); // 같은 mouse-up 중복 처리 금지
        Assert.That(selection.TryBeginSelection(piece.Position), Is.False);
        Assert.That(selection.CooldownRemaining, Is.GreaterThan(0f));
    }
    [Test]
    public void CancelDuringDragDoesNotDeleteOnReleaseOrSpendCooldown()
    {
        var piece = Drop(0, -1f); var selection = Selection(); selection.dragCooldown = 100f;
        Vector2 start = piece.Position - Vector2.one;
        Assert.That(selection.TryBeginSelection(start), Is.True);
        selection.UpdateSelection(piece.Position + Vector2.one);
        Assert.That(selection.IsDragging, Is.True);
        selection.CancelSelection(); // X 키 콜백과 같은 경로
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero);
        Assert.That(piece.IsLive, Is.True); Assert.That(selection.CooldownRemaining, Is.Zero);
        Assert.That(selection.TryBeginSelection(start), Is.True);
    }
    [Test]
    public void DragClampsWidthAndHeightIndependentlyAndDoesNotBecomeAClickAfterReturningToStart()
    {
        var piece = Drop(0, -1f); var selection = Selection();
        selection.maximumDragWidth = .5f; selection.maximumDragHeight = .25f;
        Vector2 start = piece.Position - new Vector2(1f, .1f);
        Assert.That(selection.TryBeginSelection(start), Is.True);
        selection.UpdateSelection(start + Vector2.right * 10f);
        Assert.That(selection.SelectionBounds.width, Is.EqualTo(.5f).Within(.01f));
        selection.UpdateSelection(start + new Vector2(10f, 10f));
        Assert.That(selection.SelectionBounds.width, Is.EqualTo(.5f).Within(.01f));
        Assert.That(selection.SelectionBounds.height, Is.EqualTo(.25f).Within(.01f));
        Rect maximum = selection.SelectionBounds;
        selection.UpdateSelection(start + new Vector2(100f, 11f));
        Assert.That(selection.SelectionBounds, Is.EqualTo(maximum));
        selection.UpdateSelection(start);
        Assert.That(selection.IsDragging, Is.True); // 왕복 드래그는 클릭이 아닙니다.
        Assert.That(selection.CompleteSelection(start), Is.Zero); Assert.That(piece.IsLive, Is.True);
        Assert.That(selection.CooldownRemaining, Is.Zero);
    }
    [Test]
    public void SelectedOldRentalCannotConsumeItsReplacementFromAQueuedMerge()
    {
        var first = Drop(0, -1f); var second = Drop(1, 1f); int life = first.LifeId;
        manager.QueueContact(first, second);
        var selection = Selection(); Physics2D.SyncTransforms();
        selection.TryBeginSelection(first.Position);
        Assert.That(selection.CompleteSelection(first.Position), Is.EqualTo(1));
        var reused = Drop(0, -1f); Assert.That(reused, Is.SameAs(first));
        Assert.That(manager.TryRemoveItem(reused, life), Is.False);
        manager.ProcessContacts();
        Assert.That(manager.ActiveItems.Count, Is.EqualTo(2)); Assert.That(reused.IsLive && second.IsLive, Is.True);
    }
    [Test]
    public void SelectionOfOneBoardDoesNotIncludeOtherBoardsOrOperateWhilePaused()
    {
        var piece = Drop(0, -1f);
        var selection = Selection();
        var other = new GameObject("Other board"); other.transform.SetParent(host.transform, false);
        selection.board = other.AddComponent<MergegameManager>(); selection.board.acceptKeyboardInput = false;
        Physics2D.SyncTransforms();
        selection.TryBeginSelection(piece.Position);
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero); Assert.That(piece.IsLive, Is.True);
        selection.board = manager; selection.TryBeginSelection(piece.Position);
        Time.timeScale = 0f;
        Assert.That(selection.CompleteSelection(piece.Position), Is.Zero); Assert.That(piece.IsLive, Is.True);
        Assert.That(selection.IsSelecting, Is.False);
    }
}

public sealed class MergeSpawnProbe : MonoBehaviour, IPoolable
{
    public static System.Action callback;
    public void OnSpawnedFromPool() => callback?.Invoke();
    public void OnReturnedToPool() { }
}
