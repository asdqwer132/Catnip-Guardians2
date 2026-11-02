using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CurrencyPresentationTests
{
    private List<Object> owned;
    private Scene first, second;
    private GameObject Host(string name, Scene scene, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(false);
        SceneManager.MoveGameObjectToScene(host, scene); owned.Add(host);
        host.SetActive(active); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    private TextMeshProUGUI Text(string name, Scene scene)
        => Host(name, scene).AddComponent<TextMeshProUGUI>();
    private CurrencyData Data(bool korean = true)
    {
        var catalog = Asset<CurrencyData>();
        var data = new DefinedDataEntry<CurrencyType> { type = CurrencyType.CurrencyMain };
        catalog.entries.Add(data);
        data.data = korean ? new[] {
            new Description { language = language.english, dataName = "Gold" },
            new Description { language = language.korean, dataName = "골드" }
        } : new[] { new Description { language = language.english, dataName = "Gold" } };
        var texture = new Texture2D(2, 2); owned.Add(texture);
        data.icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(data.icon);
        return catalog;
    }
    private CurrencyUIGroup Group(Scene scene, CurrencyDisplaySource source, CurrencyData data, params CurrencyUIGroup.CurrencyUI[] rows)
    {
        var host = Host("Currency group", scene, false);
        var group = host.AddComponent<CurrencyUIGroup>(); group.source = source; group.currencyData = data; group.currencyUIs = rows;
        host.SetActive(true); return group;
    }
    private SceneLanguageMediator Mediator(Scene scene, language standalone = language.english)
    {
        var host = Host("Scene language", scene, false);
        var mediator = host.AddComponent<SceneLanguageMediator>(); mediator.standaloneLanguage = standalone;
        host.SetActive(true); return mediator;
    }
    private LanguageManager Core(language selected = language.english)
    {
        var host = Host("Core language", first, false);
        var manager = host.AddComponent<LanguageManager>(); manager.selectedLan = selected;
        host.SetActive(true); return manager;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        first = SceneManager.CreateScene("Currency UI A"); second = SceneManager.CreateScene("Currency UI B");
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        if (first.IsValid() && first.isLoaded) yield return SceneManager.UnloadSceneAsync(first);
        if (second.IsValid() && second.isLoaded) yield return SceneManager.UnloadSceneAsync(second);
        yield return new ExitPlayMode();
    }

    [Test]
    public void LateLanguageAndWalletUpdateNamesWithoutWritingOverAmounts()
    {
        var data = Data(); var name = Text("Name", first); var amount = Text("Amount", first);
        var icon = Host("Icon", first).AddComponent<Image>();
        var row = new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencyMain, nameText = name, amountText = amount, icon = icon };
        Group(first, CurrencyDisplaySource.Wallet, data, row);
        Assert.That(name.text, Is.EqualTo("Gold")); Assert.That(icon.sprite, Is.SameAs(data.entries[0].icon));
        var manager = Core(language.korean);
        Mediator(first);
        Assert.That(name.text, Is.EqualTo("골드"));
        var wallet = Host("Wallet", first).AddComponent<CurrencyManager>();
        int initial = wallet.GetCurrency(row.type);
        wallet.AddCurrency(row.type, 7);
        Assert.That(amount.text, Is.EqualTo((initial + 7).ToString()));
        manager.SetLanguage(language.english);
        Assert.That(name.text, Is.EqualTo("Gold"));
        Assert.That(amount.text, Is.EqualTo((initial + 7).ToString()));
    }

    [Test]
    public void AmountOnlyNameOnlyIconOnlyAndUnassignedDataRemainIndependent()
    {
        Mediator(first, language.korean);
        var amount = Text("Amount", first); var name = Text("Name", first);
        var icon = Host("Icon", first).AddComponent<Image>();
        var authored = Text("Authored Name", first); authored.text = "Keep this text";
        var data = Data();
        var group = Group(first, CurrencyDisplaySource.Wallet, data,
            new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencyMain, amountText = amount },
            new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencyMain, nameText = name },
            new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencyMain, icon = icon },
            null);
        Group(first, CurrencyDisplaySource.Wallet, null, new CurrencyUIGroup.CurrencyUI { nameText = authored });
        group.UpdateUI(CurrencyType.CurrencyMain, 19);
        Assert.That(amount.text, Is.EqualTo("19")); Assert.That(name.text, Is.EqualTo("골드"));
        Assert.That(icon.sprite, Is.SameAs(data.entries[0].icon)); Assert.That(authored.text, Is.EqualTo("Keep this text"));
        group.currencyUIs = null;
        Assert.DoesNotThrow(group.RefreshPresentation);
        Assert.DoesNotThrow(() => group.UpdateUI(CurrencyType.CurrencyMain, 0));
    }

    [Test]
    public void RoundCurrencyAndExistingLocalizedNameMarkerKeepTheirSeparateRoles()
    {
        Mediator(first); var manager = Core();
        var name = Text("Name", first); var amount = Text("Amount", first);
        var marker = name.gameObject.AddComponent<LocalizedText>(); marker.tmpText = name; marker.key = "Old label";
        var data = Data(); data.entries[0].type = CurrencyType.CurrencySub1;
        Group(first, CurrencyDisplaySource.CurrentGame, data,
            new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencySub1, nameText = name, amountText = amount });
        var stats = Host("Round statistics", first).AddComponent<GameStatisticsManager>();
        stats.AddCurrency(CurrencyType.CurrencySub1, 12);
        var wallet = Host("Wallet", first).AddComponent<CurrencyManager>();
        wallet.AddCurrency(CurrencyType.CurrencySub1, 100);
        manager.SetLanguage(language.korean);
        Assert.That(name.text, Is.EqualTo("골드")); Assert.That(amount.text, Is.EqualTo("12"));
        Assert.That(marker.source, Is.EqualTo(LocalizedTextSource.DataName));
        Assert.That(stats.GetCurrency(CurrencyType.CurrencySub1), Is.EqualTo(12));
        stats.ResetRound(); Assert.That(amount.text, Is.EqualTo("0"));
    }

    [Test]
    public void MissingTranslationFallsBackToEnglishAndMissingIconDoesNotShowAWhiteImage()
    {
        Mediator(first, language.korean);
        var data = Data(false); data.entries[0].icon = null;
        var name = Text("Name", first); var icon = Host("Icon", first).AddComponent<Image>();
        var group = Group(first, CurrencyDisplaySource.Wallet, data,
            new CurrencyUIGroup.CurrencyUI { nameText = name, icon = icon });
        Assert.That(name.text, Is.EqualTo("Gold")); Assert.That(icon.enabled, Is.False);
        group.currencyData = Asset<CurrencyData>(); group.RefreshPresentation();
        Assert.That(name.text, Is.EqualTo("CurrencyMain"));
    }

    [Test]
    public void DisabledGroupsAndMovedGroupsUseTheCurrentSceneLanguageOnReturn()
    {
        var original = Mediator(first); Mediator(second, language.korean);
        var name = Text("Name", first); var data = Data();
        var group = Group(first, CurrencyDisplaySource.Wallet, data,
            new CurrencyUIGroup.CurrencyUI { nameText = name });
        group.gameObject.SetActive(false); var manager = Core(language.korean);
        group.gameObject.SetActive(true); Assert.That(name.text, Is.EqualTo("골드"));
        Object.DestroyImmediate(manager.gameObject);
        Assert.That(name.text, Is.EqualTo("Gold"));
        SceneManager.MoveGameObjectToScene(group.gameObject, second);
        original.RefreshAll(); Assert.That(name.text, Is.EqualTo("골드"));
    }
}

public sealed class CurrencyPresentationAuthoringTests
{
    [Test]
    public void PreparedAssetsAndExistingGroupsHaveExplicitDisplayReferencesAndKeepLegacyAmounts()
    {
        const string path = "Assets/Data/Scriptable/Currency/Currencies.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<CurrencyData>(path);
        Assert.That(catalog != null, Is.True);
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            var entry = catalog.GetData(type);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.GetDataName(language.english), Is.Not.Empty);
            Assert.That(entry.GetDataName(language.korean), Is.Not.Empty);
        }
        foreach (string sceneName in new[] { "UpgradeScene", "GameGeneralScene" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            var expected = AssetDatabase.LoadAssetAtPath<CurrencyData>(path);
            foreach (var group in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CurrencyUIGroup>(true)))
            {
                Assert.That(group.currencyData, Is.SameAs(expected));
                foreach (var row in group.currencyUIs)
                {
                    Assert.That(expected.GetData(row.type), Is.Not.Null);
                    if (row.textObj == null) continue;
                    Assert.That(row.amountText != null, Is.True, sceneName + ": " + row.type);
                    var icon = row.textObj.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.name == "Icon");
                    Assert.That(row.icon, Is.SameAs(icon));
                }
            }
        }
        // This scene still serializes textUI, before the rename to amountText.
        var legacy = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeSceneLegucy.unity", OpenSceneMode.Single);
        var oldGroup = legacy.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CurrencyUIGroup>(true)).Single();
        Assert.That(oldGroup.currencyUIs.Where(row => row.textObj != null).All(row => row.amountText != null), Is.True);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
