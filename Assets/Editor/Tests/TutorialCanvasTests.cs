using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class TutorialCanvasTests
{
    private List<Object> owned;
    private string savedHistory;
    private bool hadHistory;
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.SetActive(active); owned.Add(go);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    private TutorialCanvasController Controller(string scope, params string[] ids)
    {
        var go = Host(scope, active: false);
        var canvas = go.AddComponent<Canvas>();
        var controller = go.AddComponent<TutorialCanvasController>(); controller.tutorialCanvas = canvas; controller.completionScope = scope;
        controller.previousButton = Host("Previous", go.transform).AddComponent<Button>();
        controller.nextButton = Host("Next", go.transform).AddComponent<Button>();
        controller.skipButton = Host("Skip", go.transform).AddComponent<Button>();
        var entries = new List<TutorialPanelSequence>();
        foreach (string id in ids)
        {
            var root = Host(id, go.transform);
            entries.Add(new TutorialPanelSequence { id = id, panelRoot = root,
                pages = new[] { Host("Page 1", root.transform), Host("Page 2", root.transform) } });
        }
        controller.tutorials = entries.ToArray(); go.SetActive(true); return controller;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        hadHistory = PlayerPrefs.HasKey(TutorialCompletionHistory.SaveKey);
        savedHistory = PlayerPrefs.GetString(TutorialCompletionHistory.SaveKey);
        PlayerPrefs.DeleteKey(TutorialCompletionHistory.SaveKey);
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        if (hadHistory) PlayerPrefs.SetString(TutorialCompletionHistory.SaveKey, savedHistory);
        else PlayerPrefs.DeleteKey(TutorialCompletionHistory.SaveKey);
        PlayerPrefs.Save();
        yield return new ExitPlayMode();
    }

    [Test]
    public void AuthoredButtonsNavigatePagesAndCompleteOnlyAfterTheLastPage()
    {
        var controller = Controller("upgrade", "shop");
        Assert.That(controller.tutorialCanvas.enabled, Is.False);
        Assert.That(controller.TryShowTutorial("shop"), Is.True);
        Assert.That(controller.previousButton.interactable, Is.False);
        Assert.That(controller.tutorials[0].pages[0].activeSelf, Is.True);
        controller.nextButton.onClick.Invoke();
        Assert.That(controller.CurrentPageIndex, Is.EqualTo(1));
        Assert.That(controller.tutorials[0].pages[0].activeSelf, Is.False);
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "shop"), Is.False);
        controller.previousButton.onClick.Invoke(); Assert.That(controller.CurrentPageIndex, Is.Zero);
        controller.nextButton.onClick.Invoke(); controller.nextButton.onClick.Invoke();
        Assert.That(controller.IsShowing, Is.False);
        Assert.That(controller.tutorialCanvas.enabled, Is.False);
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "shop"), Is.True);
        Assert.That(controller.TryShowTutorial("shop"), Is.False);
        Assert.That(controller.gameObject.activeInHierarchy, Is.True, "Hiding a canvas never disables its own controller.");
    }

    [Test]
    public void SimultaneousRequestsQueueOnceAndSkippedTutorialsHaveIndependentCompletionOptions()
    {
        var controller = Controller("general", "enemy", "drop");
        controller.tutorials[0].skipCountsAsCompleted = false;
        Assert.That(controller.TryShowTutorial("enemy"), Is.True);
        Assert.That(controller.TryShowTutorial("drop"), Is.True);
        Assert.That(controller.TryShowTutorial("drop"), Is.False);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("enemy"));
        controller.skipButton.onClick.Invoke();
        Assert.That(TutorialCompletionHistory.IsCompleted("general", "enemy"), Is.False);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("drop"));
        Assert.That(controller.tutorials[0].panelRoot.activeSelf, Is.False);
        controller.skipButton.onClick.Invoke();
        Assert.That(TutorialCompletionHistory.IsCompleted("general", "drop"), Is.True);
        Assert.That(controller.IsShowing, Is.False);
    }

    [Test]
    public void SceneScopesAreIndependentAndDisablingDoesNotCompleteUnfinishedTutorials()
    {
        var upgrade = Controller("upgrade", "same"); var general = Controller("general", "same");
        upgrade.ShowTutorial("same"); upgrade.Skip();
        Assert.That(general.TryShowTutorial("same"), Is.True);
        general.gameObject.SetActive(false);
        Assert.That(TutorialCompletionHistory.IsCompleted("general", "same"), Is.False);
        general.gameObject.SetActive(true);
        Assert.That(general.TryShowTutorial("same"), Is.True);
        general.Cancel(); Assert.That(general.IsShowing, Is.False);
        TutorialCompletionHistory.ResetAll();
        Assert.That(upgrade.TryShowTutorial("same"), Is.True);
    }

    [Test]
    public void InvalidUiReferencesNeverConsumeOnceOnlyCompletion()
    {
        var controller = Controller("upgrade", "shop");
        controller.tutorials[0].pages[0] = null;
        Assert.That(controller.TryShowTutorial("shop"), Is.False);
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "shop"), Is.False);
        Assert.That(controller.TryShowTutorial("unknown"), Is.False);
    }

    [UnityTest]
    public IEnumerator ToggleTriggersOnlyOnSelectionAndDoesNotOpenOnInitialState()
    {
        var controller = Controller("upgrade", "shop");
        var host = Host("Shop toggle", active: false); var toggle = host.AddComponent<Toggle>(); toggle.isOn = true;
        var trigger = host.AddComponent<TutorialTrigger>(); trigger.controller = controller; trigger.tutorialId = "shop";
        trigger.mode = TutorialTriggerMode.ToggleSelected; trigger.sourceToggle = toggle;
        host.SetActive(true); yield return null;
        Assert.That(controller.IsShowing, Is.False);
        toggle.isOn = false; Assert.That(controller.IsShowing, Is.False);
        toggle.isOn = true; Assert.That(controller.CurrentTutorialId, Is.EqualTo("shop"));
        controller.Skip(); toggle.isOn = false; toggle.isOn = true;
        Assert.That(controller.IsShowing, Is.False);
    }

    [UnityTest]
    public IEnumerator ClickingInitiallySelectedRequiredToggleStillTriggersTutorial()
    {
        var controller = Controller("upgrade", "skill");
        var group = Host("Panel toggle group").AddComponent<ToggleGroup>(); group.allowSwitchOff = false;
        var host = Host("Skill toggle", group.transform, false);
        var toggle = host.AddComponent<Toggle>(); toggle.group = group; toggle.isOn = true;
        var trigger = host.AddComponent<TutorialTrigger>(); trigger.controller = controller; trigger.tutorialId = "skill";
        trigger.mode = TutorialTriggerMode.ToggleSelected; trigger.sourceToggle = toggle;
        host.SetActive(true);
        trigger.TriggerWhenSelected(true);
        Assert.That(controller.IsShowing, Is.False, "Initialization is not a user selection.");
        yield return null;
        Assert.That(controller.IsShowing, Is.False);
        toggle.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
        Assert.That(toggle.isOn, Is.True);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("skill"));
    }

    [Test]
    public void GeneralReceivesEarlierGameplayEventAndFiltersItsSourceScene()
    {
        TutorialSignals.Raise(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), TutorialSignals.EnemySpawned);
        var controller = Controller("general", "first"); controller.enabled = false;
        controller.tutorials[0].triggerEvent = TutorialSignals.EnemySpawned;
        controller.tutorials[0].sourceScene = "Another scene";
        controller.enabled = true; Assert.That(controller.IsShowing, Is.False);
        controller.enabled = false;
        controller.tutorials[0].sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        controller.enabled = true;
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("first"));
    }

    [Test]
    public void OnlyAnActiveRegisteredEnemyPublishesTheFirstEnemyEvent()
    {
        var controller = Controller("general", "enemy"); controller.tutorials[0].triggerEvent = TutorialSignals.EnemySpawned;
        var manager = Host("Enemies").AddComponent<EnemyManager>();
        var enemyHost = Host("Enemy", active: false); var enemy = enemyHost.AddComponent<Enemy>();
        manager.RegisterEnemy(enemy); Assert.That(controller.IsShowing, Is.False);
        enemyHost.SetActive(true); manager.RegisterEnemy(enemy);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("enemy"));
        controller.tutorials[0].skipCountsAsCompleted = false;
        controller.Skip(); manager.RegisterEnemy(enemy);
        Assert.That(controller.IsShowing, Is.True, "An unrecorded skip may be triggered by the next actual event.");
        controller.tutorials[0].skipCountsAsCompleted = true;
        controller.Skip(); manager.RegisterEnemy(enemy); Assert.That(controller.IsShowing, Is.False);
    }

    [Test]
    public void OnlySuccessfulItemDropsPublishTheFirstDropEvent()
    {
        var controller = Controller("general", "drop"); controller.tutorials[0].triggerEvent = TutorialSignals.ItemDropped;
        var boardHost = Host("Board", active: false); boardHost.transform.position = Vector3.one * 30000;
        var board = boardHost.AddComponent<MergegameManager>(); board.prewarmCount = 0; board.logActions = false;
        var pool = boardHost.AddComponent<ObjectPoolManager>(); pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = boardHost.transform;
        board.objectPool = pool; board.spawnPoint = boardHost.transform; board.dropInterval = board.dropOffset = 0;
        var item = Asset<MergeItem>(); item.item = Asset<ItemData>(); item.radius = .1f;
        var prefab = Host("Piece template", active: false); prefab.AddComponent<MergeItemObject>().GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem); item.prefab = prefab;
        var slotHost = Host("Material"); var ui = slotHost.AddComponent<MergeUI>(); ui.toggle = slotHost.AddComponent<Toggle>(); ui.toggle.isOn = true;
        board.mergeMaterials.Add(new MergeItemMaterial(item, ui, 2, 0));
        boardHost.SetActive(true);
        board.isFeeding = false;
        Assert.That(board.TryDropCurrent(), Is.False); Assert.That(controller.IsShowing, Is.False);
        board.isFeeding = true;
        Assert.That(board.TryDropCurrent(), Is.True); Assert.That(controller.CurrentTutorialId, Is.EqualTo("drop"));
        controller.Skip();
        board.isFeeding = false;
        Assert.That(board.TryDropCurrent(), Is.False);
        Assert.That(controller.IsShowing, Is.False);
    }
}

public sealed class TutorialSceneWiringTests
{
    [TearDown]
    public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void UpgradePanelTogglesUseTheirOwnSceneControllerAndUniqueTutorialIds()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(root => root.GetComponentsInChildren<TutorialCanvasController>(true)).Single();
        var triggers = roots.SelectMany(root => root.GetComponentsInChildren<TutorialTrigger>(true)).ToArray();
        Assert.That(controller.completionScope, Is.EqualTo("upgrade"));
        Assert.That(triggers.Length, Is.EqualTo(5));
        Assert.That(controller.tutorials.Select(tutorial => tutorial.id).Distinct().Count(), Is.EqualTo(5));
        foreach (var trigger in triggers)
        {
            Assert.That(trigger.controller, Is.SameAs(controller));
            Assert.That(trigger.mode, Is.EqualTo(TutorialTriggerMode.ToggleSelected));
            Assert.That(trigger.sourceToggle, Is.SameAs(trigger.GetComponent<Toggle>()));
            Assert.That(controller.tutorials.Any(tutorial => tutorial.id == trigger.tutorialId), Is.True);
        }
        // The user will supply the new UI; no placeholder Canvas/pages are generated.
        Assert.That(controller.tutorialCanvas, Is.Null);
    }

    [Test]
    public void GeneralGameplayTriggersMatchItsConfiguredChildScenes()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameGeneralScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(root => root.GetComponentsInChildren<TutorialCanvasController>(true)).Single();
        var general = roots.SelectMany(root => root.GetComponentsInChildren<GameGeneralSceneController>(true)).Single();
        Assert.That(controller.completionScope, Is.EqualTo("general"));
        Assert.That(controller.tutorials.Single(tutorial => tutorial.triggerEvent == TutorialSignals.EnemySpawned).sourceScene, Is.EqualTo(general.battleScene));
        Assert.That(controller.tutorials.Single(tutorial => tutorial.triggerEvent == TutorialSignals.ItemDropped).sourceScene, Is.EqualTo(general.itemScene));
        Assert.That(controller.tutorialCanvas, Is.Null);
    }
}
