using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class UpgradeProgressSaveTests
{
    [Test]
    public void SavedSnapshotRestoresAssetReferencesCountsAndEquipmentWithoutUpgradeScene()
    {
        string directory = Path.Combine(Path.GetTempPath(), "UpgradeSaveTest-" + System.Guid.NewGuid());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, UpgradeProgressSave.SaveFileName);
        try
        {
            var catalog = Resources.Load<UpgradeProgressCatalog>("UpgradeProgressCatalog");
            Assert.That(catalog, Is.Not.Null);
            var item = catalog.entries.Select(value => value.asset).OfType<ItemData>().First();
            var node = catalog.entries.Select(value => value.asset).OfType<SkillNodeData>().First();
            var data = new UpgradeToGeneralData { mainPlantIndex = 3 };
            data.materialItems.Add(new InventoryItem(item, 7));
            data.unlockedSkillNodes.Add(node);
            data.potionSlots.Add(new PotionEquipmentSnapshot { slotIndex = 2, potion = item });
            UpgradeProgressSave.Write(data, directory);
            Assert.That(File.ReadAllText(path), Does.Contain("assetKey"));
            var loaded = UpgradeProgressSave.Read(directory);
            Assert.That(loaded.materialItems[0].itemData, Is.SameAs(item));
            Assert.That(loaded.materialItems[0].amount, Is.EqualTo(7));
            Assert.That(loaded.unlockedSkillNodes[0], Is.SameAs(node));
            Assert.That(loaded.potionSlots[0].potion, Is.SameAs(item));
            Assert.That(loaded.potionSlots[0].slotIndex, Is.EqualTo(2));
            Assert.That(loaded.mainPlantIndex, Is.EqualTo(3));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
