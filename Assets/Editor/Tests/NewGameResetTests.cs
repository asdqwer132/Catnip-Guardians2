using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class NewGameResetTests
{
    [Test]
    public void NewGameDeletesEveryCurrentJsonSaveAndPendingWriteWithoutDeletingOtherFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CatnipNewGame-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        string[] saves = { CodexDiscoveryHistory.SaveFileName, PotionRecipeHistory.SaveFileName,
            MergeDiscoveryHistory.SaveFileName, MergeBoxUpgradeManager.SaveFileName };
        try
        {
            foreach (string name in saves)
            {
                File.WriteAllText(Path.Combine(directory, name), "old progress");
                File.WriteAllText(Path.Combine(directory, name + ".tmp"), "pending old progress");
            }
            string unrelated = Path.Combine(directory, "unrelated.json");
            File.WriteAllText(unrelated, "keep");
            GameSaveData.DeleteJsonSaves(directory);
            GameSaveData.DeleteJsonSaves(directory); // A second New Game also works without existing saves.
            Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(new[] { unrelated }));
            Assert.That(File.ReadAllText(unrelated), Is.EqualTo("keep"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void AuthoredMainNewGameButtonCallsPrologueStartManager()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
        try
        {
            var callbacks = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Where(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(i => button.onClick.GetPersistentTarget(i) is PrologueStartManager
                        && button.onClick.GetPersistentMethodName(i) == nameof(PrologueStartManager.StartGame)));
            Assert.That(callbacks.Count(), Is.EqualTo(1));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
