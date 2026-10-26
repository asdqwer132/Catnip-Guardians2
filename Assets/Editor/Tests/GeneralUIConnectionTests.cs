using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GeneralUIConnectionTests
{
    private List<GameObject> owned;
    private GameObject Host(string name)
    {
        var host = new GameObject(name);
        host.SetActive(false);
        owned.Add(host);
        return host;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<GameObject>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        yield return new ExitPlayMode();
    }

    private GeneralUIDataSender Sender(out GameObject panel)
    {
        var host = Host("Battle sender");
        panel = Host("Authored panel");
        var button = host.AddComponent<ObjectToggleButton>();
        button.targetObjects = new[] { panel };
        button.initClose = true;
        var sender = host.AddComponent<GeneralUIDataSender>();
        var serialized = new SerializedObject(sender);
        serialized.FindProperty("uIData").FindPropertyRelative("toggleButton").objectReferenceValue = button;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        host.SetActive(true);
        return sender;
    }

    [UnityTest]
    public IEnumerator ReceiverFirstQueuesRequestsAndReconnectsAfterBattleUnload()
    {
        var host = Host("Core receiver");
        var receiver = host.AddComponent<GeneralUIDataReciever>();
        host.SetActive(true);
        receiver.TryUIDataToggleButton(true);
        var sender = Sender(out var panel);
        yield return null;
        yield return null;
        Assert.That(panel.activeSelf, Is.True, "The button's Start must not overwrite an early request.");
        sender.gameObject.SetActive(false);
        receiver.TryUIDataToggleButton(false);
        sender.gameObject.SetActive(true);
        yield return null;
        yield return null;
        Assert.That(panel.activeSelf, Is.False);
        var battle = SceneManager.CreateScene("Sender scene reload test");
        SceneManager.MoveGameObjectToScene(sender.gameObject, battle);
        SceneManager.MoveGameObjectToScene(panel, battle);
        Assert.That(sender.GetSenderSceneName(), Is.EqualTo(battle.name));
        yield return SceneManager.UnloadSceneAsync(battle);
        Assert.That(GeneralUIDataSender.instance == null, Is.True);
        receiver.TryUIDataToggleButton(true);
        var replacement = Sender(out var newPanel);
        yield return null;
        yield return null;
        Assert.That(GeneralUIDataSender.instance, Is.SameAs(replacement));
        Assert.That(newPanel.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator SenderFirstBindsWithoutClosingTheExistingUi()
    {
        var sender = Sender(out var panel);
        yield return null;
        yield return null;
        sender.GetToggleBtn().SetObjectActive(true);
        var host = Host("Late receiver");
        var receiver = host.AddComponent<GeneralUIDataReciever>();
        host.SetActive(true);
        Assert.That(panel.activeSelf, Is.True);
        receiver.TryUIDataToggleButton(false);
        Assert.That(panel.activeSelf, Is.False);
        host.SetActive(false);
        sender.GetToggleBtn().SetObjectActive(true);
        host.SetActive(true);
        Assert.That(panel.activeSelf, Is.True, "Rebinding must not reset the authored button state.");
        receiver.TryUIDataToggleButton(false);
        Assert.That(panel.activeSelf, Is.False);
    }
}
