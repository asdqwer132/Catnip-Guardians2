using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class MergeBoxSceneTests
{
    private readonly List<Object> owned = new List<Object>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        foreach (var obj in owned) if (obj != null) Object.DestroyImmediate(obj);
        owned.Clear();
        yield return new ExitPlayMode();
    }

    private MergeBoxSceneController Controller()
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var value = root.GetComponentInChildren<MergeBoxSceneController>(true);
            if (value != null) return value;
        }
        Assert.Fail("The authored item scene needs its box controller connection.");
        return null;
    }

    private static IEnumerator WaitGameTime(float duration)
    {
        float until = Time.time + duration;
        float timeout = Time.realtimeSinceStartup + 10f;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until));
    }

    [UnityTest]
    public IEnumerator AuthoredDropdownSelectsAllEightBoxesAndKeepsEachBoxInsideItsFrame()
    {
        var controller = Controller();
        controller.SetFeeding(false);
        Assert.That(controller.boxes.Length, Is.EqualTo(8));
        Assert.That(controller.seriesDropdown.options.Count, Is.EqualTo(8));
        Vector3 first = controller.boxes[0].boxRoot.position;
        for (int i = 0; i < 8; i++)
        {
            var entry = controller.boxes[i];
            Assert.That(entry.board.spawnPoint, Is.Not.Null);
            Assert.That(entry.board.mergeMaterials.Count, Is.EqualTo(controller.boxes[0].board.mergeMaterials.Count));
            if (i > 0) Assert.That(entry.board.mergeMaterials[0], Is.Not.SameAs(controller.boxes[0].board.mergeMaterials[0]));
            controller.seriesDropdown.value = i;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(controller.SelectedBoard, Is.SameAs(entry.board));
            Assert.That(controller.selection.board, Is.SameAs(entry.board));
            AssertFrame(controller.fitter);
            Assert.That(controller.boxes[0].boxRoot.position, Is.EqualTo(first), "Switching views must not drag the row of boxes around.");
        }
        foreach (var rect in new[] { new Rect(.6f, 0, .4f, .85f), new Rect(0, 0, 1, 1), new Rect(.1f, 0, .3f, 1) })
        {
            controller.fitter.worldCamera.rect = rect;
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertFrame(controller.fitter);
        }
        foreach (var entry in controller.boxes)
            foreach (var sprite in entry.boxRoot.GetComponentsInChildren<SpriteRenderer>(true))
                Assert.That(sprite.sharedMaterial.shader.name, Is.Not.EqualTo("Universal Render Pipeline/2D/Sprite-Lit-Default"));
    }

    public static void AssertFrame(UIWorldBoundsFitter fitter)
    {
        var corners = new Vector3[4];
        fitter.frame.GetWorldCorners(corners);
        var canvas = fitter.frame.GetComponentInParent<Canvas>().rootCanvas;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
        Vector2 actualMin = fitter.worldCamera.WorldToScreenPoint(fitter.worldRoot.TransformPoint(fitter.localBounds.min));
        Vector2 actualMax = fitter.worldCamera.WorldToScreenPoint(fitter.worldRoot.TransformPoint(fitter.localBounds.max));
        var size = max - min;
        string detail = $"viewport={fitter.worldCamera.rect}, frame={min}..{max}, box={actualMin}..{actualMax}, camera={fitter.worldCamera.transform.position}";
        Assert.That((actualMin - min - Vector2.Scale(size, fitter.frameOccupancy.min)).magnitude, Is.LessThan(.5f), detail);
        Assert.That((actualMax - min - Vector2.Scale(size, fitter.frameOccupancy.max)).magnitude, Is.LessThan(.5f), detail);
    }

    [UnityTest]
    public IEnumerator AutomaticFeedCyclesMaterialsAndPauseDoesNotSpendStock()
    {
        var controller = Controller();
        controller.SetFeeding(false);
        var board = controller.boxes[0].board;
        board.ClearItems();
        // 실제 풀을 사용하되 다른 상자/벽과 겹치지 않는 독립 투입 위치에서 주기를 검사합니다.
        var point = new GameObject("Test feed point");
        owned.Add(point);
        point.transform.position = new Vector3(50000, 50000, 0);
        board.spawnPoint = point.transform;
        board.dropInterval = .1f;
        board.logActions = false;
        board.automaticDrop = true;
        var order = new List<MergeItem>();
        board.onBoardChanged.AddListener(() =>
        {
            if (board.ActiveItems.Count > 0) order.Add(board.ActiveItems[board.ActiveItems.Count - 1].Definition);
        });
        var a = board.mergeMaterials[0];
        var b = board.mergeMaterials[1];
        Assert.That(a.defaultCount, Is.GreaterThan(1));
        Assert.That(b.defaultCount, Is.GreaterThan(0));
        board.SelectMaterial(0);
        board.SetFeeding(true);
        float timeout = Time.realtimeSinceStartup + 10;
        while (order.Count < 3 && Time.realtimeSinceStartup < timeout) yield return null;
        board.SetFeeding(false);
        CollectionAssert.AreEqual(new[] { a.mergeItem, b.mergeItem, a.mergeItem }, order);
        int countA = a.defaultCount, countB = b.defaultCount;
        yield return WaitGameTime(.25f);
        Assert.That(a.defaultCount, Is.EqualTo(countA));
        Assert.That(b.defaultCount, Is.EqualTo(countB));
        board.SetFeeding(true);
        yield return WaitGameTime(.13f);
        board.SetFeeding(false);
        Assert.That(order.Count, Is.EqualTo(4));
        Assert.That(order[3], Is.SameAs(b.mergeItem));
        foreach (var piece in board.ActiveItems)
            foreach (var sprite in piece.GetComponentsInChildren<SpriteRenderer>(true))
                Assert.That(sprite.sharedMaterial.shader.name, Is.Not.EqualTo("Universal Render Pipeline/2D/Sprite-Lit-Default"));
    }

    [UnityTest]
    public IEnumerator FailedAutomaticFeedDoesNotAdvanceMaterialOrConsumeStock()
    {
        var controller = Controller();
        controller.SetFeeding(false);
        var board = controller.boxes[0].board;
        board.logActions = false;
        board.dropInterval = .05f;
        board.SelectMaterial(0);
        board.spawnPoint = null;
        var material = board.currentMaterial;
        int bonus = material.defaultCount;
        board.SetFeeding(true);
        yield return WaitGameTime(.12f);
        board.SetFeeding(false);
        Assert.That(board.currentMaterial, Is.SameAs(material));
        Assert.That(material.defaultCount, Is.EqualTo(bonus));
        Assert.That(board.ActiveItems.Count, Is.Zero);
    }
}

public class GeneralComposedGaugeAndBoxTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameGeneralScene.unity", OpenSceneMode.Single);
        var core = EditorSceneManager.OpenScene("Assets/Scenes/CoreScene.unity", OpenSceneMode.Additive);
        // Headless Linux의 OS 커서 호출을 제외하고 실제 Core 매니저들과 함께 검사합니다.
        foreach (var root in core.GetRootGameObjects())
            foreach (var cursor in root.GetComponentsInChildren<CursorChanger>(true))
                cursor.gameObject.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        yield return new EnterPlayMode();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }

    private static T InScene<T>(string sceneName) where T : Component
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    [UnityTest]
    public IEnumerator GeneralBootConnectsGaugesAndFramesWithoutItemLighting()
    {
        var general = InScene<GameGeneralSceneController>(GameSceneRoutes.General);
        float timeout = Time.realtimeSinceStartup + 30;
        while (!general.IsReady && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(general.IsReady, Is.True);
        var boxes = InScene<MergeBoxSceneController>(GameSceneRoutes.Item);
        boxes.SetFeeding(false);
        var receiver = InScene<GeneralBattleGaugeUI>(GameSceneRoutes.General);
        var data = GeneralUIDataSender.instance.GetData();
        Assert.That(receiver.hp.enemyManager, Is.SameAs(data.enemyManager));
        Assert.That(receiver.hp.hpFill.fillImage.fillAmount, Is.EqualTo(
            (float)data.enemyManager.CurrentAliveEnemyCount / data.enemyManager.maxAliveEnemyCount).Within(.001f));
        Assert.That(receiver.grow.fillImage.fillAmount, Is.EqualTo(data.growManager.GrowProgress01).Within(.001f));

        foreach (float ratio in new[] { .6f, .7f, .5f })
        {
            general.SetBattleRatio(ratio);
            boxes.seriesDropdown.value = (boxes.SelectedIndex + 1) % boxes.boxes.Length;
            yield return null;
            Canvas.ForceUpdateCanvases();
            MergeBoxSceneTests.AssertFrame(boxes.fitter);
            Assert.That(boxes.fitter.worldCamera, Is.SameAs(general.ItemCamera));
        }
        foreach (var root in general.ItemScene.GetRootGameObjects())
            foreach (var light in root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
                Assert.That(light.enabled, Is.False, "The authored disabled item light must remain disabled.");
        foreach (var entry in boxes.boxes)
            foreach (var sprite in entry.boxRoot.GetComponentsInChildren<SpriteRenderer>(true))
                Assert.That(sprite.sharedMaterial.shader.name, Is.Not.EqualTo("Universal Render Pipeline/2D/Sprite-Lit-Default"));
    }
}
