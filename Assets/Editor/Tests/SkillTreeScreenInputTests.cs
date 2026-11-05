using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SkillTreeScreenInputTests : SkillTreeLayoutFixture
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Copy the authored information frame and its actual button, including
        // prefab overrides and persistent click listeners. Isolate gameplay/save managers.
        var source = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        var panels = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SkillTreeMapPanel>(true)).Single();
        var frame = Object.Instantiate(panels.nodeInformation.transform.parent.gameObject);
        var canvas = new GameObject("Skill input test canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(600, 1000); scaler.matchWidthOrHeight = 1;
        frame.transform.SetParent(canvas.transform, false);
        var rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(600, 900); rect.localScale = Vector3.one;
        foreach (var behaviour in frame.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(behaviour is UIBehaviour) && !(behaviour is ItemTooltipUI)) behaviour.enabled = false;
        var isolated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(canvas, isolated);
        EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(isolated);
        yield return new EnterPlayMode();
        owned = new List<Object>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Dispose();
        yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator AuthoredButtonRaycastsSwapsItsOwnSpriteAndPurchasesTheDisplayedSkill()
    {
        var canvas = SceneManager.GetActiveScene().GetRootGameObjects().Single(r => r.name == "Skill input test canvas");
        var info = canvas.GetComponentInChildren<CostTooltipUI>(true);
        var button = info.unlockButtonPanel.GetComponent<Button>();
        var image = button.GetComponent<Image>();
        var sprites = button.spriteState;
        var walletHost = Host("Wallet"); walletHost.SetActive(false);
        var wallet = walletHost.AddComponent<CurrencyManager>(); wallet.defaultStartAmount = 0;
        walletHost.SetActive(true);
        var manager = Host("Manager").AddComponent<SkillTreeManager>();
        var data = Data("Screen input skill");
        data.costs = new[] { new Cost { currencyType = CurrencyType.CurrencyMain, amount = 10 } };
        var node = Host("Tooltip source").AddComponent<SkillNodeUI>(); node.SetData(data);
        var provider = node.gameObject.AddComponent<SkillNodeTooltipProvider>();
        info.Show(provider);
        yield return null;
        Canvas.ForceUpdateCanvases();

        Assert.That(info.IsVisible, Is.True);
        Assert.That(button.targetGraphic, Is.SameAs(image), "Sprite Swap must target this button, not the shop's Buy image.");
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
        Assert.That(sprites.highlightedSprite, Is.Not.Null); Assert.That(sprites.pressedSprite, Is.Not.Null);
        var rect = button.GetComponent<RectTransform>();
        var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        foreach (float x in new[] { .1f, .5f, .9f })
        {
            pointer.position = RectTransformUtility.WorldToScreenPoint(null,
                rect.TransformPoint(new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, x), rect.rect.center.y)));
            var hits = new List<RaycastResult>(); raycaster.Raycast(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.SameAs(button.gameObject),
                "The authored button is covered by " + hits[0].gameObject.name);
        }

        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.That(image.overrideSprite, Is.SameAs(sprites.highlightedSprite));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Assert.That(image.overrideSprite, Is.SameAs(sprites.pressedSprite));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(manager.IsUnlocked(data.dataId), Is.False, "No funds must leave the skill locked.");
        Assert.That(info.IsVisible, Is.True);

        wallet.AddCurrency(CurrencyType.CurrencyMain, 10);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(manager.IsUnlocked(data.dataId), Is.True);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.Zero);
        Assert.That(info.IsVisible, Is.False);
        Assert.That(info.alternatePanels.All(p => p == null || p.activeSelf), Is.True);
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
        Assert.That(button.spriteState.highlightedSprite, Is.SameAs(sprites.highlightedSprite));
        Assert.That(button.spriteState.pressedSprite, Is.SameAs(sprites.pressedSprite));
    }
}
