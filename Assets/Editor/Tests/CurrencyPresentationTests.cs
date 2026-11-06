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
    private CurrencyUIGroup Group(Scene scene, CurrencyDisplaySource source, CurrencyData data, params CurrencyUI[] rows)
    {
        var host = Host("Currency group", scene, false);
        var group = host.AddComponent<CurrencyUIGroup>(); group.source = source; group.currencyData = data; group.currencyUIs = rows;
        foreach (var row in rows)
            if (row != null) { row.transform.SetParent(host.transform, false); row.gameObject.SetActive(true); }
        host.SetActive(true); return group;
    }
    private CurrencyUI Row(Scene scene, CurrencyType type = CurrencyType.CurrencyMain,
        TextMeshProUGUI amountText = null, TextMeshProUGUI nameText = null, Image icon = null)
    {
        var row = Host("Currency row", scene, false).AddComponent<CurrencyUI>();
        row.type = type; row.amountText = amountText; row.nameText = nameText; row.icon = icon;
        return row;
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
        var row = Row(first, nameText: name, amountText: amount, icon: icon);
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
            Row(first, amountText: amount),
            Row(first, nameText: name),
            Row(first, icon: icon),
            null);
        Group(first, CurrencyDisplaySource.Wallet, null, Row(first, nameText: authored));
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
            Row(first, CurrencyType.CurrencySub1, nameText: name, amountText: amount));
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
            Row(first, nameText: name, icon: icon));
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
            Row(first, nameText: name));
        group.gameObject.SetActive(false); var manager = Core(language.korean);
        group.gameObject.SetActive(true); Assert.That(name.text, Is.EqualTo("골드"));
        Object.DestroyImmediate(manager.gameObject);
        Assert.That(name.text, Is.EqualTo("Gold"));
        SceneManager.MoveGameObjectToScene(group.gameObject, second);
        original.RefreshAll(); Assert.That(name.text, Is.EqualTo("골드"));
    }

    [Test]
    public void NeedCurrencyPrefabSeparatesOwnedAndRequiredAmountsAndKeepsItsOwnLocalizedReferences()
    {
        Mediator(first); var languageManager = Core();
        var template = AssetDatabase.LoadAssetAtPath<CurrencyUI>("Assets/Prefab/UI/Utility/Currency/NeedCurrency.prefab");
        Assert.That(template, Is.Not.Null);
        var row = Object.Instantiate(template); owned.Add(row.gameObject);
        SceneManager.MoveGameObjectToScene(row.gameObject, first);
        var catalog = Data();
        var group = Group(first, CurrencyDisplaySource.Wallet, catalog, row);
        row.SetRequiredAmount(12); group.UpdateUI(CurrencyType.CurrencyMain, 7);
        Assert.That(row.amountText.name, Is.EqualTo("HaveCount"));
        Assert.That(row.requiredAmountText.name, Is.EqualTo("NeedCount"));
        Assert.That(row.amountText.text, Is.EqualTo("7")); Assert.That(row.requiredAmountText.text, Is.EqualTo("12"));
        languageManager.SetLanguage(language.korean);
        Assert.That(row.nameText.text, Is.EqualTo("골드"));
        Assert.That(row.amountText.text, Is.EqualTo("7")); Assert.That(row.requiredAmountText.text, Is.EqualTo("12"));
        row.type = CurrencyType.CurrencySub1;
        row.SetAmount(0); Assert.That(row.gameObject.activeSelf, Is.True, "An unmet required amount must stay visible.");
        row.SetRequiredAmount(0); Assert.That(row.gameObject.activeSelf, Is.False);
        row.SetAmount(3); Assert.That(row.gameObject.activeSelf, Is.True);
    }
}

public sealed class CurrencyPresentationAuthoringTests
{
    [Test]
    public void PreparedCurrencyAssetsAndConfiguredRowsKeepTheirAuthoredDisplayReferences()
    {
        const string path = "Assets/Data/Scriptable/DisplayData/Currencies.asset";
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
                    if (row == null) continue; // Unconfigured navigation slots stay unconfigured.
                    Assert.That(expected.GetData(row.type), Is.Not.Null);
                    Assert.That(row.currencyData, Is.SameAs(expected));
                    if (row.amountText != null) Assert.That(row.amountText.transform.IsChildOf(row.transform), Is.True);
                    if (row.nameText != null) Assert.That(row.nameText.transform.IsChildOf(row.transform), Is.True);
                    if (row.icon != null) Assert.That(row.icon.transform.IsChildOf(row.transform), Is.True);
                }
            }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void CurrencyRowPrefabsAndGroupsUseComponentReferencesWithoutInlineDisplayBindings()
    {
        const string folder = "Assets/Prefab/UI/Utility/Currency/";
        foreach (string name in new[] { "NeedCurrency", "MainCurrrency", "SubCurrrency" })
        {
            var row = AssetDatabase.LoadAssetAtPath<CurrencyUI>(folder + name + ".prefab");
            Assert.That(row, Is.Not.Null, name);
            Assert.That(row.amountText, Is.Not.Null, name); Assert.That(row.icon, Is.Not.Null, name);
            Assert.That(row.amountText.transform.IsChildOf(row.transform), Is.True);
            Assert.That(row.icon.transform.IsChildOf(row.transform), Is.True);
        }
        foreach (string name in new[] { "NeedCurrencyArea", "CurrencyList" })
        {
            var group = AssetDatabase.LoadAssetAtPath<CurrencyUIGroup>(folder + name + ".prefab");
            Assert.That(group.currencyUIs.Length, Is.EqualTo(9));
            Assert.That(group.currencyUIs.All(r => r != null), Is.True, name);
            Assert.That(group.currencyUIs.Select(r => r.type).Distinct().Count(), Is.EqualTo(9), name);
            foreach (var row in group.currencyUIs)
            {
                Assert.That(row.amountText.transform.IsChildOf(row.transform), Is.True, name);
                if (row.nameText != null) Assert.That(row.nameText.transform.IsChildOf(row.transform), Is.True, name);
            }
        }
    }
}
