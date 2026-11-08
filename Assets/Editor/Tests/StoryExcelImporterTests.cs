using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class StoryExcelImporterTests
{
    private const string Example = "Assets/Data/Excel/StoryTest.xlsx";
    private const string Output = "Assets/__StoryExcelTest";
    private string workbook;

    [SetUp]
    public void SetUp()
    {
        workbook = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xlsx");
        File.Copy(Example, workbook);
    }

    [TearDown]
    public void TearDown()
    {
        Undo.ClearAll();
        if (AssetDatabase.IsValidFolder(Output)) AssetDatabase.DeleteAsset(Output);
        if (File.Exists(workbook)) File.Delete(workbook);
    }

    private void EditSheet(string sheet, Action<XDocument> edit)
    {
        using (var archive = ZipFile.Open(workbook, ZipArchiveMode.Update))
        {
            var entry = archive.GetEntry("xl/worksheets/" + sheet + ".xml");
            XDocument document;
            using (var input = entry.Open()) document = XDocument.Load(input);
            edit(document);
            entry.Delete();
            using (var output = archive.CreateEntry("xl/worksheets/" + sheet + ".xml").Open()) document.Save(output);
        }
    }

    [Test]
    public void WorkbookBuildsNineLocalizedStoriesWithNaturallyOrderedSprites()
    {
        var plan = StoryExcelImporter.Read(workbook, Output);
        Assert.That(plan.Count, Is.EqualTo(9));
        Assert.That(plan.Sum(entry => entry.dialogues.Length), Is.EqualTo(44));
        Assert.That(AssetDatabase.IsValidFolder(Output), Is.False, "Preview does not create assets.");
        var first = plan.First(entry => entry.assetPath.EndsWith("StoryData 1.asset", StringComparison.Ordinal));
        Assert.That(first.sprites.Length, Is.EqualTo(3));
        Assert.That(AssetDatabase.GetAssetPath(first.sprites[0]), Does.EndWith("Stroy (1)/01-01-forest-discovery.png"));
        Assert.That(first.dialogues[0].nextImage, Is.False);
        Assert.That(first.dialogues[1].nextImage, Is.True);
        Assert.That(first.dialogues[0].GetDialogue(language.korean).speaker, Is.EqualTo("나레이션"));
        Assert.That(first.dialogues[0].GetDialogue(language.english).dialogue, Does.StartWith("Test dialogue"));
    }

    [Test]
    public void ReimportUpdatesExistingAssetWithoutChangingGuidAndPreservesMultilineText()
    {
        Assert.That(StoryExcelImporter.Import(workbook, Output), Is.EqualTo(9));
        string path = Output + "/StoryData 1.asset";
        string guid = AssetDatabase.AssetPathToGUID(path);
        var existing = AssetDatabase.LoadAssetAtPath<StoryData>(path);
        EditSheet("sheet2", doc =>
        {
            var cell = doc.Descendants().Single(e => e.Name.LocalName == "c" && (string)e.Attribute("r") == "F2");
            cell.Descendants().Single(e => e.Name.LocalName == "t").Value = "수정 대사\n두 번째 줄 & <태그>";
        });
        StoryExcelImporter.Import(workbook, Output);
        Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
        Assert.That(AssetDatabase.LoadAssetAtPath<StoryData>(path), Is.SameAs(existing));
        Assert.That(existing.dialogues[0].GetDialogue(language.korean).dialogue, Is.EqualTo("수정 대사\n두 번째 줄 & <태그>"));
        Assert.That(AssetDatabase.FindAssets("t:StoryData", new[] { Output }).Length, Is.EqualTo(9));
    }

    [Test]
    public void MissingTranslationRowAbortsBeforeAnyExistingDataChanges()
    {
        StoryExcelImporter.Import(workbook, Output);
        var existing = AssetDatabase.LoadAssetAtPath<StoryData>(Output + "/StoryData 1.asset");
        string original = EditorJsonUtility.ToJson(existing);
        EditSheet("sheet2", doc => doc.Descendants().Single(e => e.Name.LocalName == "row" && (string)e.Attribute("r") == "3").Remove());
        Assert.Throws<InvalidDataException>(() => StoryExcelImporter.Import(workbook, Output));
        Assert.That(EditorJsonUtility.ToJson(existing), Is.EqualTo(original));
    }

    [Test]
    public void MismatchedImageFlagsAreRejectedBeforeCreatingAssets()
    {
        EditSheet("sheet2", doc => doc.Descendants().Single(e => e.Name.LocalName == "c" && (string)e.Attribute("r") == "D2")
            .Elements().Single(e => e.Name.LocalName == "v").Value = "1");
        Assert.Throws<InvalidDataException>(() => StoryExcelImporter.Import(workbook, Output));
        Assert.That(AssetDatabase.IsValidFolder(Output), Is.False);
    }

    [Test]
    public void MissingSpriteFolderIsRejectedBeforeCreatingAssets()
    {
        EditSheet("sheet1", doc => doc.Descendants().Single(e => e.Name.LocalName == "c" && (string)e.Attribute("r") == "C2")
            .Descendants().Single(e => e.Name.LocalName == "t").Value = "Assets/MissingStorySprites");
        Assert.Throws<InvalidDataException>(() => StoryExcelImporter.Import(workbook, Output));
        Assert.That(AssetDatabase.IsValidFolder(Output), Is.False);
    }

    [Test]
    public void FormulaDialogueIsRejectedWithoutUsingCachedValue()
    {
        EditSheet("sheet1", doc =>
        {
            var cell = doc.Descendants().Single(e => e.Name.LocalName == "c" && (string)e.Attribute("r") == "F2");
            cell.Add(new XElement(cell.Name.Namespace + "f", "A2"));
        });
        Assert.Throws<InvalidDataException>(() => StoryExcelImporter.Import(workbook, Output));
        Assert.That(AssetDatabase.IsValidFolder(Output), Is.False);
    }
}
