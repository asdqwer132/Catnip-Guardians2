using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class CurrencyPersistenceTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SessionState.SetBool("CurrencyTests.HadSave", PlayerPrefs.HasKey(CurrencyManager.SaveKey));
        SessionState.SetString("CurrencyTests.Backup", PlayerPrefs.GetString(CurrencyManager.SaveKey, ""));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        PlayerPrefs.DeleteKey(CurrencyManager.SaveKey);
    }

    private static CurrencyManager CreateWallet(int main, int sub)
    {
        var host = new GameObject("Wallet fixture");
        host.SetActive(false);
        var wallet = host.AddComponent<CurrencyManager>();
        wallet.initialAmounts = new[]
        {
            new Cost { currencyType = CurrencyType.CurrencyMain, amount = main },
            new Cost { currencyType = CurrencyType.CurrencySub1, amount = sub }
        };
        host.SetActive(true);
        return wallet;
    }

    [UnityTest]
    public IEnumerator FirstRunUsesSeparateDefaultsAndRestartRestoresEarnedSpentAndZeroBalances()
    {
        var wallet = CreateWallet(100, 7);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(100));
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencySub1), Is.EqualTo(7));
        Assert.That(PlayerPrefs.HasKey(CurrencyManager.SaveKey), Is.True);
        wallet.AddCurrency(CurrencyType.CurrencyMain, 25);
        Assert.That(wallet.SpendCurrency(CurrencyType.CurrencySub1, 7), Is.True);
        string beforeFailedSpend = PlayerPrefs.GetString(CurrencyManager.SaveKey);
        Assert.That(wallet.SpendCurrency(CurrencyType.CurrencyMain, 999), Is.False);
        Assert.That(PlayerPrefs.GetString(CurrencyManager.SaveKey), Is.EqualTo(beforeFailedSpend));
        Object.Destroy(wallet.gameObject);
        yield return null;
        wallet = CreateWallet(999, 999);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(125));
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencySub1), Is.Zero);
    }

    [UnityTest]
    public IEnumerator MissingCurrencyUsesItsOwnDefaultAndDeletingSaveRestoresInitialAmounts()
    {
        PlayerPrefs.SetString(CurrencyManager.SaveKey, "{\"balances\":[{\"currencyType\":0,\"amount\":0}]}");
        var wallet = CreateWallet(100, 7);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.Zero);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencySub1), Is.EqualTo(7));
        Object.Destroy(wallet.gameObject);
        yield return null;
        PlayerPrefs.DeleteKey(CurrencyManager.SaveKey);
        wallet = CreateWallet(100, 7);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(100));
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencySub1), Is.EqualTo(7));
    }

    [UnityTest]
    public IEnumerator DuplicateManagerCannotOverwriteWalletAndBrokenSaveFallsBackToDefaults()
    {
        var wallet = CreateWallet(100, 7);
        wallet.AddCurrency(CurrencyType.CurrencyMain, 1);
        var duplicate = CreateWallet(999, 999);
        yield return null;
        Assert.That(duplicate == null, Is.True);
        Assert.That(CurrencyManager.instance, Is.SameAs(wallet));
        Object.Destroy(wallet.gameObject);
        yield return null;
        PlayerPrefs.SetString(CurrencyManager.SaveKey, "{broken");
        LogAssert.Expect(LogType.Warning, "Currency save is invalid. Using configured initial amounts.");
        wallet = CreateWallet(20, 3);
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(20));
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencySub1), Is.EqualTo(3));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying)
        {
            if (CurrencyManager.instance != null) Object.Destroy(CurrencyManager.instance.gameObject);
            yield return null;
            yield return new ExitPlayMode();
        }
        if (SessionState.GetBool("CurrencyTests.HadSave", false))
            PlayerPrefs.SetString(CurrencyManager.SaveKey, SessionState.GetString("CurrencyTests.Backup", ""));
        else PlayerPrefs.DeleteKey(CurrencyManager.SaveKey);
        PlayerPrefs.Save();
        SessionState.EraseBool("CurrencyTests.HadSave");
        SessionState.EraseString("CurrencyTests.Backup");
    }
}
