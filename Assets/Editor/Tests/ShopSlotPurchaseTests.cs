using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class ShopSlotPurchaseTests
{
    private List<Object> owned;
    private CurrencyManager wallet;
    private InventoryManager inventory;
    private ShopManager shop;
    private ItemBoxData box;
    private ItemData reward;

    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private Sprite Sprite()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite); return sprite;
    }
    private AnimationClip Clip(Sprite start, Sprite end)
    {
        var clip = new AnimationClip { frameRate = 10f }; owned.Add(clip);
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"),
            new[] { new ObjectReferenceKeyframe { time = 0f, value = start }, new ObjectReferenceKeyframe { time = 0.1f, value = end } });
        return clip;
    }
    private static void ExpectFailure() => LogAssert.Expect(LogType.Log, new Regex(@"\[Shop\] 구매 실패:"));
    private int Money => wallet.GetCurrency(CurrencyType.CurrencyMain);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
        wallet = Host("Wallet").AddComponent<CurrencyManager>();
        inventory = Host("Inventory").AddComponent<InventoryManager>();
        reward = Asset<ItemData>(); reward.dataId = "Reward";
        box = Asset<ItemBoxData>(); box.icon = Sprite();
        box.costs = new[] { new Cost { currencyType = CurrencyType.CurrencyMain, amount = 10 } };
        box.gachaItems = new[] { new GachaItemInfo { itemData = reward, weight = 1 } };
        box.openClip = Clip(box.icon, Sprite());
        shop = Host("Shop").AddComponent<ShopManager>();
        shop.boxPoolManager = Host("Box pool").AddComponent<ItemBoxPoolManager>();
        shop.boxPoolManager.allBoxList = new[] { box }; shop.boxPoolManager.allowDuplicate = true;
        shop.displayBoxCount = 2; shop.boxButtons = new ShopBoxButton[2];
        var group = Host("Shop slots").AddComponent<ToggleGroup>(); group.allowSwitchOff = true;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Main/Shop/ShopSlot.prefab");
        for (int i = 0; i < 2; i++)
        {
            var slot = Object.Instantiate(prefab, group.transform).GetComponent<ShopBoxButton>();
            slot.toggle.group = group; shop.boxButtons[i] = slot;
        }
        shop.InitShop();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }

    [Test]
    public void SuccessChargesOnceGrantsOnceAndLocksOnlyTheSelectedOffer()
    {
        int before = Money; var first = shop.boxButtons[0]; var second = shop.boxButtons[1];
        first.toggle.isOn = true;
        Assert.That(shop.TryBuySelectedBox(), Is.True);
        Assert.That(Money, Is.EqualTo(before - 10)); Assert.That(inventory.GetItemAmount(reward), Is.EqualTo(1));
        Assert.That(first.IsPurchased, Is.True); Assert.That(first.toggle.interactable, Is.False);
        Assert.That(first.openAnimation.PlayedClip, Is.SameAs(box.openClip));
        Assert.That(second.IsPurchased, Is.False); Assert.That(second.openAnimation.PlayedClip, Is.Null);
        ExpectFailure(); Assert.That(shop.TryBuySelectedBox(), Is.False);
        Assert.That(Money, Is.EqualTo(before - 10)); Assert.That(inventory.GetItemAmount(reward), Is.EqualTo(1));
    }

    [Test]
    public void DirectBuyBoxCannotRepurchaseSoldOffersButDuplicateOffersRemainIndependent()
    {
        int before = Money;
        shop.BuyBox(box); shop.BuyBox(box);
        Assert.That(shop.boxButtons[0].IsPurchased && shop.boxButtons[1].IsPurchased, Is.True);
        ExpectFailure(); shop.BuyBox(box);
        Assert.That(Money, Is.EqualTo(before - 20)); Assert.That(inventory.GetItemAmount(reward), Is.EqualTo(2));
    }

    [Test]
    public void InsufficientMoneyDoesNotAnimateLockOrGrantAndTheSameSelectionCanRetry()
    {
        int before = Money; box.costs[0].amount = before + 1;
        var slot = shop.boxButtons[0]; slot.toggle.isOn = true;
        ExpectFailure(); Assert.That(shop.TryBuySelectedBox(), Is.False);
        Assert.That(Money, Is.EqualTo(before)); Assert.That(inventory.GetItemAmount(reward), Is.Zero);
        Assert.That(slot.IsPurchased, Is.False); Assert.That(slot.toggle.isOn, Is.True);
        Assert.That(slot.openAnimation.PlayedClip, Is.Null);
        wallet.AddCurrency(CurrencyType.CurrencyMain, 1);
        Assert.That(shop.TryBuySelectedBox(), Is.True);
    }

    [Test]
    public void MissingRewardsAndRepeatedCurrencyCostsCannotCausePartialPayment()
    {
        int before = Money; var slot = shop.boxButtons[0]; slot.toggle.isOn = true;
        box.gachaItems = System.Array.Empty<GachaItemInfo>();
        ExpectFailure(); Assert.That(shop.TryBuySelectedBox(), Is.False);
        Assert.That(Money, Is.EqualTo(before)); Assert.That(slot.IsPurchased, Is.False);
        box.gachaItems = new[] { new GachaItemInfo { itemData = reward, weight = 1 } };
        box.costs = new[] { new Cost { amount = before / 2 + 1 }, new Cost { amount = before / 2 + 1 } };
        ExpectFailure(); Assert.That(shop.TryBuySelectedBox(), Is.False);
        Assert.That(Money, Is.EqualTo(before)); Assert.That(slot.openAnimation.PlayedClip, Is.Null);
    }

    [Test]
    public void MissingInventoryCannotSpendMoneyOrLockTheSlot()
    {
        int before = Money; Object.DestroyImmediate(inventory.gameObject);
        shop.boxButtons[0].toggle.isOn = true;
        ExpectFailure(); Assert.That(shop.TryBuySelectedBox(), Is.False);
        Assert.That(Money, Is.EqualTo(before)); Assert.That(shop.boxButtons[0].IsPurchased, Is.False);
    }

    [Test]
    public void RerollClearsPurchasedStateSelectionAndAnimationForAllSlots()
    {
        shop.boxButtons[0].toggle.isOn = true; Assert.That(shop.TryBuySelectedBox(), Is.True);
        shop.boxButtons[1].toggle.isOn = true;
        shop.rerollManager = Host("Reroll").AddComponent<ShopRerollManager>();
        shop.rerollManager.firstRerollFree = true; shop.RerollShop();
        foreach (var slot in shop.boxButtons)
        {
            Assert.That(slot.IsPurchased, Is.False); Assert.That(slot.toggle.interactable, Is.True);
            Assert.That(slot.toggle.isOn, Is.False); Assert.That(slot.openAnimation.PlayedClip, Is.Null);
            Assert.That(slot.icon.sprite, Is.SameAs(box.icon));
        }
    }

    [Test]
    public void FailedPaidRerollKeepsPurchasedSlotsLocked()
    {
        shop.boxButtons[0].toggle.isOn = true; Assert.That(shop.TryBuySelectedBox(), Is.True);
        shop.rerollManager = Host("Reroll").AddComponent<ShopRerollManager>();
        shop.rerollManager.price = Money + 1;
        shop.RerollShop(); Assert.That(shop.boxButtons[0].IsPurchased, Is.True);
    }

    [UnityTest]
    public IEnumerator OpeningAnimatesTheSelectedImageWhilePausedAndHoldsTheFinalFrame()
    {
        var slot = shop.boxButtons[0]; Time.timeScale = 0f;
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(box.openClip)[0];
        var frames = AnimationUtility.GetObjectReferenceCurve(box.openClip, binding);
        slot.toggle.isOn = true; Assert.That(shop.TryBuySelectedBox(), Is.True);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(slot.icon.sprite, Is.SameAs(frames[1].value));
        Assert.That(slot.openAnimation.IsPlaying, Is.False);
        slot.RefreshData(); Assert.That(slot.icon.sprite, Is.SameAs(frames[1].value));
        Assert.That(shop.boxButtons[1].icon.sprite, Is.SameAs(box.icon));
    }

    [Test]
    public void SwitchingGroupedOffersDoesNotToggleAwayTheNewSelection()
    {
        var first = shop.boxButtons[0]; var second = shop.boxButtons[1];
        first.toggle.isOn = true; second.toggle.isOn = true;
        Assert.That(first.toggle.isOn, Is.False); Assert.That(second.toggle.isOn, Is.True);
        Assert.That(shop.IsSelectedButton(second), Is.True);
    }
}
