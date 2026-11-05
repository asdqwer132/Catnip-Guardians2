using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public sealed class SkillTreeHoverTests : SkillTreeLayoutFixture
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
    }

    [UnityTearDown]
    public IEnumerator TearDown() { Dispose(); yield return new ExitPlayMode(); }

    private TextMeshProUGUI Text(string name, Transform parent)
        => Host(name, parent).AddComponent<TextMeshProUGUI>();

    [UnityTest]
    public IEnumerator AuthoredNodeTriggersKeepInformationOnExitAndRestoreMapOnSuccessfulUnlock()
    {
        Host("Wallet").AddComponent<CurrencyManager>();
        var manager = Host("Manager").AddComponent<SkillTreeManager>();
        var root = Host("Skill panel"); root.SetActive(false);
        var panels = root.AddComponent<SkillTreeMapPanel>();
        var mapHost = Host("MapInfo", root.transform);
        var map = mapHost.AddComponent<ItemTooltipUI>();
        map.tooltipPanel = mapHost; map.positionAtAnchor = false; map.removePannel = false;
        map.amountText = Text("Unlocked count", mapHost.transform);
        var skillHost = Host("SkillInfo", root.transform); skillHost.SetActive(false);
        var skill = skillHost.AddComponent<CostTooltipUI>();
        skill.tooltipPanel = skillHost; skill.positionAtAnchor = false;
        skill.dataNameText = Text("Skill name", skillHost.transform);
        skill.alternatePanels = new[] { mapHost };
        skill.needCurrencyPanel = Host("Need currency", skillHost.transform);
        skill.unlockButtonPanel = Host("Unlock button", skillHost.transform);
        skill.unlockedPanel = Host("Already unlocked", skillHost.transform);
        var graph = Host("Graph", root.transform);
        var first = Data("First skill"); var second = Data("Second skill");
        second.skillMap = first.skillMap; first.skillMap.totalNodeCount = 2;
        var selector = Host("Map selector", root.transform).AddComponent<SkillMapTooltipProvider>();
        Set(selector, "mapData", first.skillMap);
        panels.mapInformation = map; panels.nodeInformation = skill;
        panels.maps = new[] { new SkillTreeMapPanel.MapBinding
        {
            graph = graph.GetComponent<RectTransform>(), information = selector,
            toggleText = Text("Map name", root.transform)
        } };
        var template = AssetDatabase.LoadAssetAtPath<SkillNodeUI>("Assets/Prefab/UI/SkillTree/SkillNode.prefab");
        var firstNode = Object.Instantiate(template, graph.transform); owned.Add(firstNode.gameObject);
        var secondNode = Object.Instantiate(template, graph.transform); owned.Add(secondNode.gameObject);
        firstNode.SetData(first); secondNode.SetData(second);
        var injector = root.AddComponent<SkillTreeUIInjecter>();
        injector.NodesParents = new[] { graph.transform }; injector.tooltipUI = skill;
        root.SetActive(true);
        yield return null;
        Assert.That(mapHost.activeSelf, Is.True); Assert.That(skillHost.activeSelf, Is.False);
        var hover = firstNode.GetComponent<ToggleTooltipTrigger>();
        var otherHover = secondNode.GetComponent<ToggleTooltipTrigger>();
        Assert.That(hover.tooltipUI, Is.SameAs(skill));
        Assert.That(otherHover.tooltipUI, Is.SameAs(skill));
        var pointer = new PointerEventData(null);

        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.That(skill.IsVisible, Is.True, "The very first hover must survive the inactive tooltip's first Awake.");
        Assert.That(skill.dataNameText.text, Is.EqualTo(first.name));
        Assert.That(mapHost.activeSelf, Is.False);
        Assert.That(skill.needCurrencyPanel.activeSelf, Is.True);
        Assert.That(skill.unlockButtonPanel.activeSelf, Is.True);
        Assert.That(skill.unlockedPanel.activeSelf, Is.False);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        yield return null;
        Assert.That(skillHost.activeSelf, Is.True, "Leaving the node keeps its information without pinning it against other nodes.");
        Assert.That(mapHost.activeSelf, Is.False);
        Assert.That(manager.UnlockSkill(first), Is.True);
        yield return null;
        Assert.That(skillHost.activeSelf, Is.False); Assert.That(mapHost.activeSelf, Is.True);
        Assert.That(map.amountText.text, Is.EqualTo("1"), "Restored map information must include the latest unlock count.");

        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.That(skill.needCurrencyPanel.activeSelf, Is.False);
        Assert.That(skill.unlockButtonPanel.activeSelf, Is.False);
        Assert.That(skill.unlockedPanel.activeSelf, Is.True);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.That(skillHost.activeSelf, Is.False); Assert.That(mapHost.activeSelf, Is.True);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(otherHover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.That(skill.dataNameText.text, Is.EqualTo(second.name));
        Assert.That(skillHost.activeSelf, Is.True); Assert.That(mapHost.activeSelf, Is.False);
        Assert.That(skill.needCurrencyPanel.activeSelf, Is.True);
        Assert.That(skill.unlockButtonPanel.activeSelf, Is.True);
        Assert.That(skill.unlockedPanel.activeSelf, Is.False);
        ExecuteEvents.Execute(otherHover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.That(manager.UnlockSkill(first), Is.False, "An already unlocked skill cannot be purchased twice.");
        Assert.That(skillHost.activeSelf, Is.True); Assert.That(mapHost.activeSelf, Is.False);
        panels.SelectMap(0);
        Assert.That(skillHost.activeSelf, Is.False); Assert.That(mapHost.activeSelf, Is.True);
        ExecuteEvents.Execute(otherHover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        secondNode.gameObject.SetActive(false);
        Assert.That(skillHost.activeSelf, Is.False); Assert.That(mapHost.activeSelf, Is.True);
    }
}
