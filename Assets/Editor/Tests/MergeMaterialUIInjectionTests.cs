using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MergeMaterialUIInjectionTests
{
    private readonly List<Object> owned = new List<Object>();
    private string folder;
    private GameObject Host(string name, Transform parent = null)
    {
        var host = new GameObject(name, typeof(RectTransform));
        if (parent != null) host.transform.SetParent(parent, false);
        owned.Add(host); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private MergeUI Slot(Transform parent, string name)
    {
        var host = Host(name, parent); var ui = host.AddComponent<MergeUI>();
        ui.countText = Host("Main", host.transform).AddComponent<TextMeshProUGUI>();
        ui.defaultCountText = Host("Bonus", host.transform).AddComponent<TextMeshProUGUI>();
        ui.toggle = host.AddComponent<Toggle>(); ui.slotObj = host;
        return ui;
    }
    private MergeItemInjecter Injector(MergegameManager manager, GameObject root, MergeItem[] items, int count, int bonus)
    {
        var injector = manager.GetComponent<MergeItemInjecter>();
        if (injector == null) injector = manager.gameObject.AddComponent<MergeItemInjecter>();
        var serialized = new SerializedObject(injector);
        serialized.FindProperty("mergegameManager").objectReferenceValue = manager;
        serialized.FindProperty("mergeUIRoot").objectReferenceValue = root;
        var field = serialized.FindProperty("mergeItems"); field.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        serialized.FindProperty("count").intValue = count; serialized.FindProperty("defaultCount").intValue = bonus;
        serialized.ApplyModifiedPropertiesWithoutUndo(); return injector;
    }
    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        folder = "Assets/__MergeUIInjection_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
    }
    [TearDown]
    public void TearDown()
    {
        foreach (var asset in owned) if (asset != null) Undo.ClearUndo(asset);
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(folder);
    }
    [Test]
    public void InjectionIncludesInactiveSlotsInOrderAndImmediatelyRefreshesBothCounters()
    {
        var manager = Host("Manager").AddComponent<MergegameManager>(); var root = Host("Slots");
        var first = Slot(root.transform, "First"); var second = Slot(root.transform, "Second");
        first.gameObject.SetActive(false); root.SetActive(false);
        var a = Asset<MergeItem>(); var b = Asset<MergeItem>();
        var injector = Injector(manager, root, new[] { a, b }, 12, 3); injector.Inject();
        Assert.That(manager.mergeMaterials.Count, Is.EqualTo(2));
        Assert.That(manager.mergeMaterials[0].mergeUI, Is.SameAs(first)); Assert.That(manager.mergeMaterials[1].mergeUI, Is.SameAs(second));
        Assert.That(first.countText.text, Is.EqualTo("12")); Assert.That(first.defaultCountText.text, Is.EqualTo("(+3)"));
        Assert.That(second.countText.text, Is.EqualTo("12")); Assert.That(first.gameObject.activeSelf, Is.True);
        manager.mergeMaterials[0].count = manager.mergeMaterials[0].defaultCount = 0; manager.RefreshUI();
        Assert.That(first.gameObject.activeSelf, Is.False);
        injector.Inject();
        Assert.That(manager.mergeMaterials[0].mergeUI, Is.SameAs(first)); Assert.That(first.gameObject.activeSelf, Is.True);
        Assert.That(first.countText.text, Is.EqualTo("12")); Assert.That(first.defaultCountText.text, Is.EqualTo("(+3)"));
    }
    [Test]
    public void HiddenBoardInjectionCannotOverwriteTheSelectedBoardsSharedUi()
    {
        var selected = Host("Selected").AddComponent<MergegameManager>(); var hidden = Host("Hidden").AddComponent<MergegameManager>();
        var root = Host("Shared slots"); var ui = Slot(root.transform, "Slot"); var item = Asset<MergeItem>();
        Injector(selected, root, new[] { item }, 5, 1).Inject(); hidden.SetMaterialUIEnabled(false);
        Injector(hidden, root, new[] { item }, 20, 2).Inject();
        Assert.That(ui.countText.text, Is.EqualTo("5")); Assert.That(ui.defaultCountText.text, Is.EqualTo("(+1)"));
        selected.SetMaterialUIEnabled(false); hidden.SetMaterialUIEnabled(true);
        Assert.That(ui.countText.text, Is.EqualTo("20")); Assert.That(ui.defaultCountText.text, Is.EqualTo("(+2)"));
    }
    [Test]
    public void TooFewSlotsDoesNotDiscardExistingMaterials()
    {
        var manager = Host("Manager").AddComponent<MergegameManager>(); var root = Host("Slots"); Slot(root.transform, "Only slot");
        var existing = new MergeItemMaterial(Asset<MergeItem>(), null, 7, 4); manager.mergeMaterials.Add(existing);
        var injector = Injector(manager, root, new[] { Asset<MergeItem>(), Asset<MergeItem>() }, 10, 10);
        LogAssert.Expect(LogType.Error, "MergeItemInjecter: Merge Items 개수만큼 MergeUI 슬롯이 필요합니다. 기존 데이터는 유지합니다.");
        injector.Inject();
        Assert.That(manager.mergeMaterials.Count, Is.EqualTo(1)); Assert.That(manager.mergeMaterials[0], Is.SameAs(existing));
        Assert.That(existing.count, Is.EqualTo(7)); Assert.That(existing.defaultCount, Is.EqualTo(4));
    }
    [Test]
    public void EditorInjectionSurvivesSavingAndReloadingTheScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Merge/MergeManager.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); owned.Add(instance);
        var manager = instance.GetComponent<MergegameManager>(); var root = Host("Slots"); Slot(root.transform, "Slot");
        var definition = ScriptableObject.CreateInstance<MergeItem>(); AssetDatabase.CreateAsset(definition, folder + "/Item.asset");
        Injector(manager, root, new[] { definition }, 17, 6).Inject();
        string path = folder + "/Injected.unity"; EditorSceneManager.SaveScene(manager.gameObject.scene, path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); MergegameManager restored = null;
        foreach (var host in scene.GetRootGameObjects())
        { var board = host.GetComponent<MergegameManager>(); if (board != null) restored = board; }
        Assert.That(restored, Is.Not.Null); Assert.That(restored.mergeMaterials.Count, Is.EqualTo(1));
        var material = restored.mergeMaterials[0];
        Assert.That(material.mergeItem, Is.SameAs(definition)); Assert.That(material.mergeUI, Is.Not.Null);
        Assert.That(material.count, Is.EqualTo(17)); Assert.That(material.defaultCount, Is.EqualTo(6));
        restored.RefreshUI(); Assert.That(material.mergeUI.countText.text, Is.EqualTo("17"));
        Assert.That(material.mergeUI.defaultCountText.text, Is.EqualTo("(+6)"));
    }
    [Test]
    public void AuthoredItemSceneRefreshWritesToItsAssignedMaterialTexts()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single); int checkedSlots = 0;
        foreach (var root in scene.GetRootGameObjects()) foreach (var board in root.GetComponentsInChildren<MergegameManager>(true))
        {
            board.SetMaterialUIEnabled(true);
            foreach (var material in board.mergeMaterials)
            {
                Assert.That(material.mergeUI, Is.Not.Null); Assert.That(material.mergeUI.countText, Is.Not.Null);
                Assert.That(material.mergeUI.defaultCountText, Is.Not.Null);
                material.count = 13; material.defaultCount = 7;
            }
            board.RefreshUI();
            foreach (var material in board.mergeMaterials)
            {
                Assert.That(material.mergeUI.countText.text, Is.EqualTo("13"));
                Assert.That(material.mergeUI.defaultCountText.text, Is.EqualTo("(+7)")); checkedSlots++;
            }
        }
        Assert.That(checkedSlots, Is.GreaterThan(0), "씬에 재료와 연결된 UI가 있어야 합니다.");
    }
}
