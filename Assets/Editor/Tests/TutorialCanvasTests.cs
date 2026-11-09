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
        TutorialPageView View(string name)
        {
            var root = Host(name, go.transform);
            var view = root.AddComponent<TutorialPageView>();
            view.previousButton = Host("Previous", root.transform).AddComponent<Button>();
            view.nextButton = Host("Next", root.transform).AddComponent<Button>();
            view.closeButton = Host("Skip", root.transform).AddComponent<Button>();
            return view;
        }
        controller.tutorialView = View("Tutorial");
        controller.reviewView = View("Review");
        var selection = Host("Selection template", controller.reviewView.transform, false).AddComponent<TutorialSelectionToggle>();
        selection.toggle = selection.gameObject.AddComponent<Toggle>();
        controller.selectionTemplate = selection; controller.selectionParent = controller.reviewView.transform;
        var entries = new List<TutorialPanelSequence>();
        foreach (string id in ids)
        {
            var data = Asset<TutorialData>(); data.dataId = id;
            data.pages = new[] { new TutorialPageData(), new TutorialPageData() };
            entries.Add(new TutorialPanelSequence { id = id, data = data });
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
        Assert.That(controller.tutorialView.previousButton.interactable, Is.False);
        Assert.That(controller.tutorialView.CurrentPage, Is.Zero);
        controller.tutorialView.nextButton.onClick.Invoke();
        Assert.That(controller.CurrentPageIndex, Is.EqualTo(1));
        Assert.That(controller.tutorialView.CurrentPage, Is.EqualTo(1));
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "shop"), Is.False);
        controller.tutorialView.previousButton.onClick.Invoke(); Assert.That(controller.CurrentPageIndex, Is.Zero);
        controller.tutorialView.nextButton.onClick.Invoke(); controller.tutorialView.nextButton.onClick.Invoke();
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
        controller.tutorialView.closeButton.onClick.Invoke();
        Assert.That(TutorialCompletionHistory.IsCompleted("general", "enemy"), Is.False);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("drop"));
        Assert.That(controller.tutorialView.CurrentData, Is.SameAs(controller.tutorials[1].data));
        controller.tutorialView.closeButton.onClick.Invoke();
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
        controller.tutorials[0].data.pages[0] = null;
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

    private static IEnumerator Until(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "Timed tutorial request did not become ready.");
    }
    private static IEnumerator Realtime(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < until) yield return null;
    }

    [UnityTest]
    public IEnumerator GeneralSequenceWaitsForAttackAndStartsEachDelayAfterNormalClose()
    {
        var controller = Controller("general", "navigation", "feed", "throw", "result");
        controller.tutorials[0].triggerEvent = TutorialSignals.EnemyAttackedPlant;
        for (int i = 0; i < controller.tutorials.Length; i++)
        {
            controller.tutorials[i].delaySeconds = .06f;
            if (i > 0) controller.tutorials[i].afterTutorialId = controller.tutorials[i - 1].id;
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        TutorialSignals.Raise(scene, TutorialSignals.EnemySpawned);
        TutorialSignals.Raise(scene, TutorialSignals.ItemDropped);
        Assert.That(controller.IsShowing, Is.False);
        float began = Time.realtimeSinceStartup;
        TutorialSignals.Raise(scene, TutorialSignals.EnemyAttackedPlant);
        TutorialSignals.Raise(scene, TutorialSignals.EnemyAttackedPlant);
        Assert.That(controller.IsShowing, Is.False);
        yield return Until(() => controller.CurrentTutorialId == "navigation");
        Assert.That(Time.realtimeSinceStartup - began, Is.GreaterThanOrEqualTo(.05f));
        yield return Realtime(.1f);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("navigation"), "Reading time never starts the next step's delay.");
        foreach (string next in new[] { "feed", "throw", "result" })
        {
            float closed = Time.realtimeSinceStartup;
            controller.Skip();
            Assert.That(controller.IsShowing, Is.False);
            yield return Until(() => controller.CurrentTutorialId == next);
            Assert.That(Time.realtimeSinceStartup - closed, Is.GreaterThanOrEqualTo(.05f));
        }
        controller.Skip(); yield return Realtime(.1f);
        Assert.That(controller.IsShowing, Is.False);
    }

    [UnityTest]
    public IEnumerator SceneArrivalAndPanelDelayUseRealtimeAndPanelWaitsForIntroduction()
    {
        var controller = Controller("upgrade", "intro", "shop");
        controller.tutorials[0].startOnSceneEnter = true;
        controller.tutorials[0].delaySeconds = .06f;
        controller.tutorials[1].delaySeconds = .02f;
        controller.tutorials[1].requiredTutorialIds = new[] { "intro" };
        try
        {
            Time.timeScale = 0f;
            Assert.That(controller.RequestTutorial("shop"), Is.True);
            Assert.That(controller.RequestTutorial("shop"), Is.False);
            yield return Until(() => controller.CurrentTutorialId == "intro");
            Assert.That(TutorialCompletionHistory.HasSeen("upgrade", "shop"), Is.False);
            controller.Skip();
            yield return Until(() => controller.CurrentTutorialId == "shop");
        }
        finally { Time.timeScale = 1f; }
    }

    [UnityTest]
    public IEnumerator ClosingPanelDuringDelayCancelsItsTutorialAndReopeningCanRequestIt()
    {
        var controller = Controller("upgrade", "shop"); controller.tutorials[0].delaySeconds = .08f;
        var host = Host("Shop toggle", active: false); var toggle = host.AddComponent<Toggle>(); toggle.isOn = false;
        var trigger = host.AddComponent<TutorialTrigger>(); trigger.controller = controller; trigger.tutorialId = "shop";
        trigger.mode = TutorialTriggerMode.ToggleSelected; trigger.sourceToggle = toggle;
        host.SetActive(true); yield return null;
        toggle.isOn = true; toggle.isOn = false;
        yield return Realtime(.12f);
        Assert.That(controller.IsShowing, Is.False);
        Assert.That(TutorialCompletionHistory.HasSeen("upgrade", "shop"), Is.False);
        toggle.isOn = true;
        yield return Until(() => controller.CurrentTutorialId == "shop");
    }

    [UnityTest]
    public IEnumerator ReviewDoesNotAdvanceSequenceAndDisablingCancelsPendingDelay()
    {
        var controller = Controller("general", "first", "second");
        controller.tutorials[1].afterTutorialId = "first"; controller.tutorials[1].delaySeconds = .06f;
        controller.ShowTutorial("first"); controller.Cancel();
        controller.OpenReview(); controller.Next(); controller.Next(); controller.CloseReview();
        yield return Realtime(.1f);
        Assert.That(controller.IsShowing, Is.False);
        Assert.That(TutorialCompletionHistory.HasSeen("general", "second"), Is.False);
        controller.ShowTutorial("first"); controller.Skip();
        controller.enabled = false;
        yield return Realtime(.1f);
        Assert.That(controller.IsShowing, Is.False);
        Assert.That(TutorialCompletionHistory.HasSeen("general", "second"), Is.False);
    }

    [UnityTest]
    public IEnumerator ReenteringSceneResumesTheFirstUnfinishedSequenceStep()
    {
        TutorialCompletionHistory.Complete("general", "first");
        var controller = Controller("general", "first", "second", "third");
        controller.tutorials[1].afterTutorialId = "first"; controller.tutorials[1].delaySeconds = .02f;
        controller.tutorials[2].afterTutorialId = "second"; controller.tutorials[2].delaySeconds = .02f;
        yield return Until(() => controller.CurrentTutorialId == "second");
        controller.Next(); controller.Next();
        yield return Until(() => controller.CurrentTutorialId == "third");
    }

    [Test]
    public void ReviewListsSeenTutorialsIncludingCancelledOnesWithoutCompletingThem()
    {
        var controller = Controller("upgrade", "seen", "hidden");
        controller.ShowTutorial("seen"); controller.Cancel();
        Assert.That(TutorialCompletionHistory.HasSeen("upgrade", "seen"), Is.True);
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "seen"), Is.False);
        controller.OpenReview();
        Assert.That(controller.IsReviewing, Is.True);
        Assert.That(controller.reviewView.gameObject.activeSelf, Is.True);
        Assert.That(controller.tutorialView.gameObject.activeSelf, Is.False);
        Assert.That(controller.SelectionSlots.Count(slot => slot.gameObject.activeSelf), Is.EqualTo(1));
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("seen"));
        controller.SelectReviewTutorial("hidden"); Assert.That(controller.CurrentTutorialId, Is.EqualTo("seen"));
        controller.reviewView.nextButton.onClick.Invoke();
        Assert.That(controller.CurrentPageIndex, Is.EqualTo(1));
        Assert.That(controller.reviewView.nextButton.interactable, Is.False);
        controller.reviewView.closeButton.onClick.Invoke();
        Assert.That(controller.IsShowing, Is.False);
        Assert.That(TutorialCompletionHistory.IsCompleted("upgrade", "seen"), Is.False);
    }

    [Test]
    public void ReviewToggleSwitchesSharedPageAndQueuesNewAutomaticTutorials()
    {
        var controller = Controller("upgrade", "one", "two", "later");
        int starts = 0, finishes = 0;
        controller.tutorials[0].onStarted.AddListener(() => starts++);
        controller.tutorials[0].onCompleted.AddListener(() => finishes++);
        controller.ShowTutorial("one"); controller.Next(); controller.Next();
        controller.ShowTutorial("two"); controller.Skip();
        controller.OpenReview();
        Assert.That(controller.SelectionSlots.Count, Is.EqualTo(2));
        controller.SelectionSlots[1].toggle.isOn = true;
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("two"));
        Assert.That(controller.SelectionSlots[0].toggle.isOn, Is.False);
        Assert.That(controller.reviewView.CurrentData, Is.SameAs(controller.tutorials[1].data));
        Assert.That(starts, Is.EqualTo(1)); Assert.That(finishes, Is.EqualTo(1));
        Assert.That(controller.TryShowTutorial("later"), Is.True);
        Assert.That(controller.CurrentTutorialId, Is.EqualTo("two"));
        controller.CloseReview(); Assert.That(controller.CurrentTutorialId, Is.EqualTo("later"));
        Assert.That(controller.reviewView.gameObject.activeSelf, Is.False);
        Assert.That(controller.tutorialView.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void EmptyReviewDoesNotExposeUnseenTutorialsAndOldCompletedSavesRemainReviewable()
    {
        var controller = Controller("upgrade", "old");
        controller.OpenReview();
        Assert.That(controller.IsReviewing, Is.True);
        Assert.That(controller.CurrentTutorialId, Is.Null);
        Assert.That(controller.SelectionSlots.Count, Is.Zero);
        controller.CloseReview();
        PlayerPrefs.SetString(TutorialCompletionHistory.SaveKey, "{\"completedTutorialIds\":[\"upgrade\\nold\"]}");
        controller.OpenReview(); Assert.That(controller.CurrentTutorialId, Is.EqualTo("old"));
    }

    [Test]
    public void SpritePagesReplaceImagesOnTheSameAuthoredViewAndClearWhenClosed()
    {
        var controller = Controller("general", "media");
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f); owned.Add(sprite);
        var view = controller.tutorialView;
        view.spriteImage = Host("Authored Sprite", view.transform).AddComponent<Image>();
        view.pageCounter = Host("Counter", view.transform).AddComponent<TMPro.TextMeshProUGUI>();
        controller.tutorials[0].data.pages[0].sprite = sprite;
        controller.ShowTutorial("media");
        Assert.That(view.spriteImage.sprite, Is.SameAs(sprite)); Assert.That(view.pageCounter.text, Is.EqualTo("1 / 2"));
        controller.Next(); Assert.That(view.CurrentPage, Is.EqualTo(1));
        Assert.That(view.spriteImage.gameObject.activeSelf, Is.False);
        Assert.That(view.pageCounter.text, Is.EqualTo("2 / 2"));
        controller.Cancel(); Assert.That(view.spriteImage.sprite, Is.Null);
    }

    [Test]
    public void SharedViewUsesLocalizedTutorialAndPageDescriptions()
    {
        var controller = Controller("general", "localized");
        var view = controller.tutorialView;
        view.tutorialInfo = Host("Title", view.transform).AddComponent<DataInfoUI>();
        view.tutorialInfo.dataNameText = view.tutorialInfo.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        view.pageInfo = Host("Page title", view.transform).AddComponent<DataInfoUI>();
        view.pageInfo.dataNameText = view.pageInfo.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        view.pageInfo.descriptionText = Host("Description", view.transform).AddComponent<TMPro.TextMeshProUGUI>();
        var data = controller.tutorials[0].data;
        data.data = new[] { new Description { language = language.korean, dataName = "튜토리얼" } };
        data.pages[0].data = new[] { new Description { language = language.korean, dataName = "첫 페이지", description = "사용 설명" } };
        controller.ShowTutorial("localized");
        ((ISceneLocalizedUI)view.tutorialInfo).ApplyLanguage(language.korean, null);
        ((ISceneLocalizedUI)view.pageInfo).ApplyLanguage(language.korean, null);
        Assert.That(view.tutorialInfo.dataNameText.text, Is.EqualTo("튜토리얼"));
        Assert.That(view.pageInfo.dataNameText.text, Is.EqualTo("첫 페이지"));
        Assert.That(view.pageInfo.descriptionText.text, Is.EqualTo("사용 설명"));
    }

    [Test]
    public void VideoPagesUsePlayerTextureAndReleasePlaybackWhenSwitchingToSprite()
    {
        var controller = Controller("general", "video");
        var view = controller.tutorialView;
        view.spriteImage = Host("Authored Sprite", view.transform).AddComponent<Image>();
        var videoHost = Host("Authored Video", view.transform);
        view.videoImage = videoHost.AddComponent<RawImage>();
        view.videoPlayer = videoHost.AddComponent<UnityEngine.Video.VideoPlayer>();
        view.videoPlayer.playOnAwake = false;
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Editor/Tests/Fixtures/TutorialVideo.webm");
        Assert.That(clip, Is.Not.Null);
        var page = controller.tutorials[0].data.pages[0];
        page.mediaType = TutorialMediaType.Video; page.video = clip; page.loopVideo = false;
        controller.ShowTutorial("video");
        Assert.That(view.videoPlayer.clip, Is.SameAs(clip));
        Assert.That(view.videoPlayer.renderMode, Is.EqualTo(UnityEngine.Video.VideoRenderMode.APIOnly));
        Assert.That(view.videoPlayer.targetTexture, Is.Null);
        Assert.That(view.videoPlayer.isLooping, Is.False);
        Assert.That(view.videoImage.enabled, Is.False, "Old frames stay hidden until the first new decoded frame.");
        controller.Next();
        Assert.That(view.videoPlayer.clip, Is.Null); Assert.That(view.videoPlayer.isPlaying, Is.False);
        Assert.That(view.videoImage.texture, Is.Null); Assert.That(view.videoImage.gameObject.activeSelf, Is.False);
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
        var intro = controller.tutorials.Single(tutorial => tutorial.startOnSceneEnter);
        Assert.That(intro.id, Is.EqualTo("scene.introduction"));
        Assert.That(intro.delaySeconds, Is.EqualTo(3f));
        foreach (var panel in controller.tutorials.Where(tutorial => tutorial.id.StartsWith("panel.")))
        {
            CollectionAssert.Contains(panel.requiredTutorialIds, intro.id);
            Assert.That(panel.delaySeconds, Is.EqualTo(3f));
        }
        Assert.That(triggers.Length, Is.EqualTo(5));
        Assert.That(controller.tutorials.Select(tutorial => tutorial.id).Distinct().Count(), Is.EqualTo(6));
        foreach (var trigger in triggers)
        {
            Assert.That(trigger.controller, Is.SameAs(controller));
            Assert.That(trigger.mode, Is.EqualTo(TutorialTriggerMode.ToggleSelected));
            Assert.That(trigger.sourceToggle, Is.SameAs(trigger.GetComponent<Toggle>()));
            Assert.That(controller.tutorials.Any(tutorial => tutorial.id == trigger.tutorialId), Is.True);
        }
        Assert.That(controller.tutorialCanvas, Is.Not.Null);
        Assert.That(controller.tutorialView, Is.Not.Null);
        Assert.That(controller.reviewView, Is.Not.Null);
        Assert.That(controller.tutorials.All(entry => entry.data != null && entry.data.HasPages), Is.True);
    }

    [TestCase("UpgradeScene", "TutorialFrame")]
    [TestCase("GameGeneralScene", "Test")]
    public void AuthoredHelpButtonOpensTheSameScenesReview(string sceneName, string buttonName)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(root => root.GetComponentsInChildren<TutorialCanvasController>(true)).Single();
        var button = roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)).Single(b => b.name == buttonName &&
            Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "ToggleReview"));
        int index = Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Single(i => button.onClick.GetPersistentMethodName(i) == "ToggleReview");
        Assert.That(button.onClick.GetPersistentTarget(index), Is.SameAs(controller));
        Assert.That(controller.selectionTemplate, Is.Not.Null);
        Assert.That(controller.SelectionSlots.All(slot => slot != null && slot.toggle != null && slot.label != null), Is.True);
        Assert.That(controller.tutorialView.videoPlayer, Is.Not.Null);
        Assert.That(controller.reviewView.videoPlayer, Is.Not.Null);
    }

    [Test]
    public void GeneralGameplayTriggersMatchItsConfiguredChildScenes()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameGeneralScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(root => root.GetComponentsInChildren<TutorialCanvasController>(true)).Single();
        var general = roots.SelectMany(root => root.GetComponentsInChildren<GameGeneralSceneController>(true)).Single();
        Assert.That(controller.completionScope, Is.EqualTo("general"));
        Assert.That(controller.tutorials.Length, Is.EqualTo(4));
        Assert.That(controller.tutorials[0].triggerEvent, Is.EqualTo(TutorialSignals.EnemyAttackedPlant));
        Assert.That(controller.tutorials[0].sourceScene, Is.EqualTo(general.battleScene));
        for (int i = 1; i < controller.tutorials.Length; i++)
        {
            Assert.That(controller.tutorials[i].triggerEvent, Is.Empty);
            Assert.That(controller.tutorials[i].afterTutorialId, Is.EqualTo(controller.tutorials[i - 1].id));
        }
        Assert.That(controller.tutorials.All(tutorial => tutorial.delaySeconds == 3f), Is.True);
        Assert.That(controller.tutorialCanvas, Is.Not.Null);
    }
}
