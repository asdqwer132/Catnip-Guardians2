using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

    private ImageFillUI Fill(string name)
    {
        var host = Host(name);
        host.AddComponent<RectTransform>();
        var image = host.AddComponent<Image>();
        image.type = Image.Type.Filled;
        var fill = host.AddComponent<ImageFillUI>();
        fill.fillImage = image;
        host.SetActive(true);
        return fill;
    }

    [UnityTest]
    public IEnumerator GeneralGaugesBindLateBattleAndRefreshAfterBattleReplacement()
    {
        var hp = Fill("General HP").gameObject.AddComponent<HealthBarUI>();
        hp.hpFill = hp.GetComponent<ImageFillUI>();
        var grow = Fill("General Grow");
        var host = Host("General gauge receiver");
        var receiver = host.AddComponent<GeneralBattleGaugeUI>();
        receiver.hp = hp;
        receiver.grow = grow;
        host.SetActive(true);

        var sender = Sender(out _);
        sender.gameObject.SetActive(false);
        var enemies = sender.gameObject.AddComponent<EnemyManager>();
        enemies.maxAliveEnemyCount = 4;
        var growth = sender.gameObject.AddComponent<GrowManager>();
        sender.GetData().enemyManager = enemies;
        sender.GetData().growManager = growth;
        growth.SetGrowth(30f);
        sender.gameObject.SetActive(true);
        yield return null;
        yield return null;
        Assert.That(grow.fillImage.fillAmount, Is.EqualTo(.3f).Within(.001f));
        growth.SetGrowth(65f);
        Assert.That(grow.fillImage.fillAmount, Is.EqualTo(.65f).Within(.001f));
        Assert.That(hp.enemyManager, Is.SameAs(enemies));
        sender.gameObject.SetActive(false);
        Assert.That(grow.fillImage.fillAmount, Is.Zero);
        Assert.That(hp.enemyManager, Is.Null);
        growth.SetGrowth(90f);
        Assert.That(grow.fillImage.fillAmount, Is.Zero, "Unloaded sources must be unsubscribed.");

        var replacement = Sender(out _);
        replacement.gameObject.SetActive(false);
        var nextGrowth = replacement.gameObject.AddComponent<GrowManager>();
        nextGrowth.SetGrowth(20f);
        replacement.GetData().growManager = nextGrowth;
        replacement.gameObject.SetActive(true);
        yield return null;
        yield return null;
        Assert.That(grow.fillImage.fillAmount, Is.EqualTo(.2f).Within(.001f));
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

    [UnityTest]
    public IEnumerator SavedBuffSettingSynchronizesLateSendersAndReloadedSettings()
    {
        const string key = "Setting_ShowBuff";
        bool existed = PlayerPrefs.HasKey(key);
        int saved = PlayerPrefs.GetInt(key, 1);
        try
        {
            PlayerPrefs.SetInt(key, 0);
            var receiverHost = Host("Settings receiver");
            receiverHost.AddComponent<GeneralUIDataReciever>();
            receiverHost.SetActive(true);
            var managerHost = Host("Late settings manager");
            var manager = managerHost.AddComponent<SettingManager>();
            managerHost.SetActive(true);
            Assert.That(manager.setting.showBuff, Is.False);
            var sender = Sender(out var panel);
            yield return null;
            yield return null;
            Assert.That(panel.activeSelf, Is.False);
            manager.SetShowBuff(true);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(1));
            Assert.That(panel.activeSelf, Is.True);
            sender.gameObject.SetActive(false);
            manager.SetShowBuff(false);
            sender.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(managerHost);
            var replacementHost = Host("Reloaded settings manager");
            var replacement = replacementHost.AddComponent<SettingManager>();
            replacementHost.SetActive(true);
            Assert.That(replacement.setting.showBuff, Is.False);
            replacement.SetShowBuff(true);
            Assert.That(panel.activeSelf, Is.True);
        }
        finally
        {
            if (existed) PlayerPrefs.SetInt(key, saved);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
