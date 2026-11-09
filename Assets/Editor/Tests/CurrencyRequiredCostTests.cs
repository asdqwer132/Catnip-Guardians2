using NUnit.Framework;
using UnityEngine;
using TMPro;

public class CurrencyRequiredCostTests
{
    [Test] public void RequiredCostsAggregateDuplicatesAndClearRowsWithoutUsingWalletAmounts()
    {
        var host = new GameObject("Costs");
        try
        {
            var group = host.AddComponent<CurrencyUIGroup>();
            var row = new GameObject("Cost", typeof(RectTransform)); row.transform.SetParent(host.transform);
            var ui = row.AddComponent<CurrencyUI>(); ui.type = CurrencyType.CurrencyMain;
            ui.amountText = row.AddComponent<TextMeshProUGUI>();
            group.SetRequiredCosts(new[] { new Cost { amount = 3 }, new Cost { amount = 4 } });
            Assert.That(ui.amountText.text, Is.EqualTo("7")); Assert.That(group.DisplaysWallet, Is.False);
            group.SetRequiredCosts(System.Array.Empty<Cost>());
            Assert.That(ui.amountText.text, Is.EqualTo("0")); Assert.That(row.activeSelf, Is.False);
        }
        finally { Object.DestroyImmediate(host); }
    }
}
