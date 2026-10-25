using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.InputSystem;

public class SessionItemThrowTests
{
    private List<Object> owned = new List<Object>();
    private List<ItemData> launched = new List<ItemData>();
    private List<Vector3> targets = new List<Vector3>();
    private List<ItemThrowMover> movers = new List<ItemThrowMover>();
    private GameSession session, previousSession;
    private GameInputManager previousInput;
    private GameEndManager previousEnd;
    private ItemRuntimeObjectManager runtimeObjects;
    private float previousTimeScale;
    private BattleItemSessionReceiver receiver;
    private BattleSessionItemThrower thrower;
    private ItemUsePositionProvider provider;
    private ItemEffectExecutor effects;
    private CombatEffectRecordingEffect record;
    private ItemData first, second;

    private GameObject Host(string name)
    {
        var value = new GameObject(name);
        owned.Add(value);
        return value;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>();
        owned.Add(value);
        return value;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        launched = new List<ItemData>();
        targets = new List<Vector3>();
        movers = new List<ItemThrowMover>();
        previousSession = GameSession.Instance;
        previousInput = GameInputManager.instance;
        previousEnd = GameEndManager.instance;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        GameInputManager.instance = null;
        GameEndManager.instance = null;

        var sessionHost = Host("Session");
        sessionHost.SetActive(false);
        session = sessionHost.AddComponent<GameSession>();
        GameSession.Instance = session;
        receiver = Host("Receiver").AddComponent<BattleItemSessionReceiver>();
        provider = Host("Position provider").AddComponent<ItemUsePositionProvider>();
        provider.selectPositionOnly = true;
        provider.clampMousePositionByPlayerRange = false;

        runtimeObjects = Host("Runtime items").AddComponent<ItemRuntimeObjectManager>();
        var buffs = Host("Buffs").AddComponent<BuffManager>();
        effects = Host("Effects").AddComponent<ItemEffectExecutor>();
        effects.buffManager = buffs;
        var executor = Host("Executor").AddComponent<ItemThrowExecutor>();
        executor.itemEffectExecutor = effects;
        executor.showTargetRange = false;
        var prefab = Host("Throw prefab").AddComponent<ItemThrowMover>();
        prefab.destroyOnArrive = false;
        executor.throwMoverPrefab = prefab;
        record = Asset<CombatEffectRecordingEffect>();
        first = Asset<ItemData>();
        second = Asset<ItemData>();
        first.effectDatas = second.effectDatas = new ItemEffectData[] { record };

        thrower = Host("Session thrower").AddComponent<BattleSessionItemThrower>();
        thrower.receiver = receiver;
        thrower.positionProvider = provider;
        thrower.throwExecutor = executor;
        thrower.clampScatteredTargetsToPlayerRange = false;
        thrower.spread.baseRadius = 0f;
        thrower.spread.scaleByItemCount = false;
        thrower.ItemThrown += (item, target) =>
        {
            launched.Add(item);
            targets.Add(target);
            var mover = UncapturedMover();
            Assert.That(mover, Is.Not.Null);
            Assert.That(mover.IsMoving, Is.True);
            movers.Add(mover);
            owned.Add(mover.gameObject);
        };
        thrower.Init();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        launched.Clear();
        targets.Clear();
        movers.Clear();
        GameSession.Instance = previousSession;
        GameInputManager.instance = previousInput;
        GameEndManager.instance = previousEnd;
        Time.timeScale = previousTimeScale;
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    private ItemThrowMover UncapturedMover()
    {
        var objects = (HashSet<GameObject>)typeof(ItemRuntimeObjectManager)
            .GetField("runtimeObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(runtimeObjects);
        foreach (var root in objects)
        {
            var mover = root.GetComponent<ItemThrowMover>();
            if (mover != null && !movers.Contains(mover))
                return mover;
        }
        return null;
    }

    private void Send(int firstCount, int secondCount = 0)
    {
        var data = new GameItemToBattleData();
        data.items.Add(new InventoryItem(first, firstCount));
        data.items.Add(new InventoryItem(second, secondCount));
        session.SendItemsToBattle(data);
    }

    [Test]
    public void SelectionOnlyClickSkipsTheBagAndTurningItOffKeepsLegacyThrowing()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            var camera = Host("Click camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(100f, 100f, -10f);
            provider.mainCamera = camera;
            var bag = Host("Bag").AddComponent<EquipmentBag>();
            bag.bagData = Asset<BagData>();
            bag.currentSlotCount = bag.maxSlotCount = 1;
            bag.equippedItems.Add(new InventoryItem(first, 1));
            first.cooldown = 0f;
            var bagUse = Host("Bag use").AddComponent<BagItemUseManager>();
            bagUse.bag = bag;
            bagUse.throwExecutor = thrower.throwExecutor;
            bagUse.Init();
            var selection = Host("Bag selection").AddComponent<BagSelectManager>();
            selection.bagUseManagers = new[] { bagUse };
            var use = Host("Item use").AddComponent<ItemUseManager>();
            use.owner = use.gameObject;
            use.positionProvider = provider;
            use.bagSelectManager = selection;

            use.UseCurrentBagItem();
            Assert.That(UncapturedMover(), Is.Null, "The click must not launch the bag's item.");
            provider.selectPositionOnly = false;
            use.UseCurrentBagItem();
            var mover = UncapturedMover();
            if (mover != null) owned.Add(mover.gameObject);
            Assert.That(mover, Is.Not.Null, "Legacy mode still launches the current bag item.");
            Assert.That(mover.IsMoving, Is.True);
        }
        finally { InputSystem.RemoveDevice(mouse); }
    }

    [Test]
    public void SimultaneousBatchWaitsForSelectionAndExecutesEveryItemOnlyOnArrival()
    {
        Send(2, 1);
        thrower.Tick(0f);
        Assert.That(launched, Is.Empty);
        Assert.That(thrower.PendingItemCount, Is.EqualTo(3));
        provider.SelectPosition(new Vector3(5f, 2f, 0f));
        Assert.That(record.calls, Is.Empty, "Selecting a position must not execute an item.");
        thrower.Tick(0f);
        CollectionAssert.AreEqual(new[] { first, first, second }, launched);
        Assert.That(thrower.PendingItemCount, Is.Zero);
        Assert.That(record.calls, Is.Empty);
        foreach (var mover in movers) mover.Tick(mover.MoveDuration);
        Assert.That(record.calls.Count, Is.EqualTo(3));
        foreach (var call in record.calls)
            Assert.That(call.targetPosition, Is.EqualTo(new Vector3(5f, 2f, 0f)));
        thrower.Tick(1f);
        Assert.That(launched.Count, Is.EqualTo(3));
    }

    [Test]
    public void SequentialBatchUsesWholeCountForSpreadAndKeepsItsInitialTarget()
    {
        thrower.mode = SessionItemThrowMode.Sequential;
        thrower.sequentialInterval = 0.5f;
        thrower.spread.distribution = SessionThrowSpreadDistribution.EvenArc;
        thrower.spread.baseRadius = 1f;
        thrower.spread.scaleByItemCount = true;
        thrower.spread.radiusPerAdditionalItem = 0.5f;
        thrower.spread.maximumRadius = 10f;
        provider.SelectPosition(new Vector3(5f, 0f, 0f));
        Send(2, 1);
        thrower.Tick(0f);
        provider.SelectPosition(new Vector3(20f, 20f, 0f));
        thrower.Tick(0.49f);
        Assert.That(launched.Count, Is.EqualTo(1));
        thrower.Tick(0.5f);
        thrower.Tick(1f);
        CollectionAssert.AreEqual(new[] { first, first, second }, launched);
        foreach (var target in targets)
            Assert.That(Vector3.Distance(target, new Vector3(5f, 0f, 0f)), Is.EqualTo(2f).Within(0.0001f));
    }

    [Test]
    public void FailedThrowKeepsItsQuantityUntilTheExecutorIsReady()
    {
        provider.SelectPosition(Vector3.right * 5f);
        thrower.throwExecutor.itemEffectExecutor = null;
        Send(1);
        thrower.Tick(0f);
        Assert.That(launched, Is.Empty);
        Assert.That(thrower.PendingItemCount, Is.EqualTo(1));
        thrower.throwExecutor.itemEffectExecutor = effects;
        thrower.Tick(1f);
        Assert.That(launched.Count, Is.EqualTo(1));
        Assert.That(thrower.PendingItemCount, Is.Zero);
    }

    [Test]
    public void DisabledThrowerGetsUnreadBatchesAndDoesNotRepeatConsumedBatches()
    {
        thrower.enabled = false;
        Send(2);
        Assert.That(thrower.PendingItemCount, Is.Zero);
        provider.SelectPosition(Vector3.right * 5f);
        thrower.enabled = true;
        thrower.Init();
        thrower.Tick(0f);
        Assert.That(launched.Count, Is.EqualTo(2));
        thrower.Init();
        thrower.Tick(1f);
        Assert.That(launched.Count, Is.EqualTo(2));
    }

    [Test]
    public void NewGameClearsOldPendingThrowsEvenIfTheThrowerWasDisabled()
    {
        Send(2);
        thrower.enabled = false;
        session.SaveUpgradeToGeneral(new UpgradeToGeneralData());
        thrower.enabled = true;
        thrower.Init();
        provider.SelectPosition(Vector3.right * 5f);
        thrower.Tick(0f);
        Assert.That(thrower.PendingItemCount, Is.Zero);
        Assert.That(launched, Is.Empty);
    }

    [Test]
    public void PositionSelectionRejectsOtherViewportsAndKeepsTheSelectedWorldPoint()
    {
        var camera = Host("Battle camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.pixelRect = new Rect(0f, 0f, 200f, 100f);
        provider.mainCamera = camera;
        Assert.That(provider.SelectScreenPosition(new Vector2(100f, 50f)), Is.True);
        Vector3 selected = provider.SelectedPosition;
        Assert.That(selected.sqrMagnitude, Is.LessThan(0.0001f));
        Assert.That(provider.SelectScreenPosition(new Vector2(300f, 50f)), Is.False);
        Assert.That(provider.TryGetTargetPosition(out var target), Is.True);
        Assert.That(target, Is.EqualTo(selected));
    }

    [Test]
    public void CountGrowthCanBeDisabledAndSpreadRespectsRadiusAndRotatedSector()
    {
        var spread = new SessionItemThrowSpread
        {
            baseRadius = 1f, radiusPerAdditionalItem = 0.5f,
            maximumRadius = 3f, spreadAngle = 60f, directionAngle = 90f
        };
        Assert.That(spread.ResolveRadius(1), Is.EqualTo(1f));
        Assert.That(spread.ResolveRadius(3), Is.EqualTo(2f));
        Assert.That(spread.ResolveRadius(100), Is.EqualTo(3f));
        spread.scaleByItemCount = false;
        Assert.That(spread.ResolveRadius(100), Is.EqualTo(1f));
        var previousRandom = Random.state;
        try
        {
            Random.InitState(823);
            for (int i = 0; i < 50; i++)
            {
                Vector3 target = spread.GetPosition(Vector3.zero, Vector3.right, i, 50, 2f);
                Assert.That(target.magnitude, Is.LessThanOrEqualTo(2.0001f));
                Assert.That(Vector3.Angle(target, Vector3.up), Is.LessThanOrEqualTo(30.0001f));
            }
        }
        finally { Random.state = previousRandom; }
    }
}
