using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class MergeItemImportEditorTests
{
    private string folder, workbook;
    private ItemData item;
    private GameObject prefab;
    private readonly List<GameObject> hosts = new List<GameObject>();
    private T Asset<T>(string name) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, folder + "/" + name + ".asset");
        return asset;
    }
    private GameObject Host(string name)
    {
        var host = new GameObject(name); hosts.Add(host); return host;
    }
    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string name = "__MergeImportTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", name); folder = "Assets/" + name;
        workbook = Path.Combine(Path.GetTempPath(), name + ".xlsx");
        item = Asset<ItemData>("Material"); item.dataId = "test-material"; item.series = ItemSeries.Weapon;
        var host = Host("Merge prefab"); host.AddComponent<MergeItemObject>();
        prefab = PrefabUtility.SaveAsPrefabAsset(host, folder + "/Merge.prefab");
    }
    [TearDown]
    public void TearDown()
    {
        foreach (var host in hosts) if (host != null) Object.DestroyImmediate(host);
        hosts.Clear();
        foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) Undo.ClearUndo(asset);
        }
        AssetDatabase.DeleteAsset(folder);
        if (File.Exists(workbook)) File.Delete(workbook);
    }

    private void Excel(params string[][] rows)
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var data = new XElement(main + "sheetData");
        for (int r = 0; r < rows.Length; r++)
        {
            var row = new XElement(main + "row", new XAttribute("r", r + 1));
            for (int c = 0; c < rows[r].Length; c++)
                row.Add(new XElement(main + "c", new XAttribute("r", ((char)('A' + c)).ToString() + (r + 1)),
                    new XAttribute("t", "inlineStr"), new XElement(main + "is", new XElement(main + "t", rows[r][c]))));
            data.Add(row);
        }
        using (var zip = new ZipArchive(File.Create(workbook), ZipArchiveMode.Create))
        {
            Write(zip, "xl/workbook.xml", new XElement(main + "workbook", new XAttribute(XNamespace.Xmlns + "r", rel),
                new XElement(main + "sheets", new XElement(main + "sheet", new XAttribute("name", "MergeItems"), new XAttribute("sheetId", 1), new XAttribute(rel + "id", "rId1")))));
            XNamespace pack = "http://schemas.openxmlformats.org/package/2006/relationships";
            Write(zip, "xl/_rels/workbook.xml.rels", new XElement(pack + "Relationships", new XElement(pack + "Relationship",
                new XAttribute("Id", "rId1"), new XAttribute("Target", "worksheets/sheet1.xml"), new XAttribute("Type", rel.NamespaceName + "/worksheet"))));
            Write(zip, "xl/worksheets/sheet1.xml", new XElement(main + "worksheet", data));
            Write(zip, "_rels/.rels", new XElement(pack + "Relationships", new XElement(pack + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Target", "xl/workbook.xml"), new XAttribute("Type", rel.NamespaceName + "/officeDocument"))));
            XNamespace types = "http://schemas.openxmlformats.org/package/2006/content-types";
            Write(zip, "[Content_Types].xml", new XElement(types + "Types",
                new XElement(types + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(types + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(types + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(types + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));
        }
    }
    private static void Write(ZipArchive zip, string name, XElement root)
    {
        using (var stream = zip.CreateEntry(name).Open()) new XDocument(root).Save(stream);
    }
    private MergeItemExcelPlan Read() => MergeItemExcelImport.Read(workbook, "MergeItems", folder, folder, folder + "/Output");

    [Test]
    public void RealXlsxCreatesAndUpdatesMergeDefinitionsWithoutChangingTheirGuid()
    {
        Excel(new[] { "itemData", "prefab", "radius" }, new[] { item.dataId, AssetDatabase.GetAssetPath(prefab), "0.35" });
        var plan = Read(); Assert.That(plan.errors, Is.Empty);
        MergeItemExcelImport.Apply(plan);
        var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(plan.entries[0].path);
        Assert.That(definition.item, Is.SameAs(item)); Assert.That(definition.prefab, Is.SameAs(prefab));
        Assert.That(definition.radius, Is.EqualTo(.35f));
        string guid = AssetDatabase.AssetPathToGUID(plan.entries[0].path);
        Excel(new[] { "itemData", "prefab", "radius" }, new[] { AssetDatabase.GetAssetPath(item), "guid:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)), "0.7" });
        plan = Read(); Assert.That(plan.errors, Is.Empty); Assert.That(plan.entries[0].existing, Is.SameAs(definition));
        MergeItemExcelImport.Apply(plan);
        Assert.That(definition.radius, Is.EqualTo(.7f));
        Assert.That(AssetDatabase.AssetPathToGUID(plan.entries[0].path), Is.EqualTo(guid));
        Assert.That(AssetDatabase.FindAssets("t:MergeItem", new[] { folder }).Length, Is.EqualTo(1));
    }

    [Test]
    public void InvalidRowsPreventAllChangesAndAmbiguousNamesAreRejected()
    {
        AssetDatabase.CreateFolder(folder, "Output");
        var original = ScriptableObject.CreateInstance<MergeItem>();
        original.item = item; original.prefab = prefab; original.radius = .3f;
        AssetDatabase.CreateAsset(original, folder + "/Output/Existing.asset");
        Asset<ItemData>("Bad row item");
        Excel(new[] { "itemData", "prefab", "radius" }, new[] { item.dataId, "Merge", "0.8" }, new[] { "Bad row item", "Merge", "-1" });
        var plan = Read(); Assert.That(plan.errors, Is.Not.Empty);
        Assert.Throws<InvalidOperationException>(() => MergeItemExcelImport.Apply(plan));
        Assert.That(original.radius, Is.EqualTo(.3f));
        Assert.That(AssetDatabase.FindAssets("t:MergeItem", new[] { folder }).Length, Is.EqualTo(1));
        var duplicate = Asset<ItemData>("Duplicate"); duplicate.dataId = item.dataId;
        Excel(new[] { "itemData", "prefab", "radius" }, new[] { item.dataId, "Merge", "0.5" });
        Assert.That(Read().errors, Is.Not.Empty);
    }

    [Test]
    public void FormulaCellsAreRejectedBeforeCreatingAssets()
    {
        Excel(new[] { "itemData", "prefab", "radius" }, new[] { item.dataId, "Merge", "0.5" });
        using (var zip = new ZipArchive(File.Open(workbook, FileMode.Open), ZipArchiveMode.Update))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
            XDocument xml;
            using (var stream = entry.Open()) xml = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var cell in xml.Descendants(ns + "c"))
                if ((string)cell.Attribute("r") == "C2") cell.Add(new XElement(ns + "f", "1/2"));
            entry.Delete();
            using (var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open()) xml.Save(stream);
        }
        var plan = Read();
        Assert.That(plan.errors, Is.Not.Empty);
        Assert.That(AssetDatabase.FindAssets("t:MergeItem", new[] { folder }), Is.Empty);
    }

    [Test]
    public void FolderImportFiltersSeriesAndPreservesTheAuthoredMaterialUiAndCounts()
    {
        var a = Asset<MergeItem>("Weapon"); a.item = item; a.prefab = prefab;
        var food = Asset<ItemData>("Food"); food.series = ItemSeries.Food;
        var b = Asset<MergeItem>("Food merge"); b.item = food; b.prefab = prefab;
        var manager = Host("Manager").AddComponent<MergegameManager>(); manager.itemSeries = ItemSeries.Weapon;
        var stock = new MergeItemMaterial(a, null, 7, 2); manager.mergeMaterials.Add(stock);
        MergeManagerFolderImport.ApplyItems(manager, folder, true, true);
        Assert.That(manager.mergeItems, Is.EqualTo(new[] { a }));
        Assert.That(manager.mergeMaterials[0], Is.SameAs(stock));
        Assert.That(stock.count, Is.EqualTo(7)); Assert.That(stock.defaultCount, Is.EqualTo(2));
        MergeManagerFolderImport.ApplyItems(manager, folder, true, false);
        Assert.That(manager.mergeItems.Count, Is.EqualTo(2));
    }

    [Test]
    public void LegacyGraphNodesUpgradeToItemDataWithoutDeletingMergeAssets()
    {
        var definition = Asset<MergeItem>("Legacy definition"); definition.item = item;
        var graph = Asset<MergeRecipeGraphAsset>("Legacy graph");
        graph.items.Add(new MergeGraphItemNode());
        var serialized = new SerializedObject(graph);
        serialized.FindProperty("items").GetArrayElementAtIndex(0).FindPropertyRelative("legacyDefinition").objectReferenceValue = definition;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        MergeRecipeGraphEditing.Upgrade(graph);
        Assert.That(graph.items[0].itemData, Is.SameAs(item));
        Assert.That(AssetDatabase.Contains(definition), Is.True);
        Assert.That(MergeRecipeGraphEditing.Validate(graph), Is.Empty);
    }
}
