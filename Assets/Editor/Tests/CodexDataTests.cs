using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CodexDataTests
{
    private sealed class CountingEffect : ItemEffectData
    {
        public int executions;
        public override void ExecuteEffect(ItemEffectContext context) => executions++;
    }

    private readonly List<Object> owned = new List<Object>();
    private T Data<T>() where T : ScriptableObject
    {
        var data = ScriptableObject.CreateInstance<T>(); owned.Add(data); return data;
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void ItemAndEnemyShareAuthoredEffectDescriptionsWithoutExecutingEffects()
    {
        var common = Data<CodexEffectDescription>();
        common.data = new[] { new Description { language = language.english, dataName = "Burn", detailDescription = "Authored effect details" } };
        var secondary = Data<CodexEffectDescription>();
        var effect = Data<CountingEffect>(); effect.codexDescription = common;
        var item = Data<ItemData>(); item.codexEffects = new[] { common, null, secondary, common };
        item.effectDatas = new ItemEffectData[] { null, effect };
        var enemy = Data<EnemyDataSet>(); enemy.codexEffects = new[] { common, common, null };
        Assert.That(CodexEffectUtility.GetDescriptions(item), Is.EqualTo(new[] { common, secondary }));
        Assert.That(CodexEffectUtility.GetDescriptions(enemy), Is.EqualTo(new[] { common }));
        Assert.That(common.GetDetailDescription(language.english), Is.EqualTo("Authored effect details"));
        Assert.That(effect.executions, Is.Zero);
        Assert.That(CodexEffectUtility.GetDescriptions(null), Is.Empty);
    }

    [Test]
    public void CharacterEntriesKeepSpriteAndLocalizedInformationInTheSameRecord()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var firstSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero); owned.Add(firstSprite);
        var secondSprite = Sprite.Create(texture, new Rect(1, 0, 1, 1), Vector2.zero); owned.Add(secondSprite);
        var character = Data<CharacterCodexData>();
        character.portraits.Add(new CharacterCodexPortrait
        {
            state = CharacterPortraitState.Emotion, stateId = "happy", sprite = firstSprite,
            data = new[] { new Description { language = language.english, dataName = "Happy", description = "Happy portrait" } }
        });
        character.portraits.Add(new CharacterCodexPortrait
        {
            state = CharacterPortraitState.Emotion, stateId = "angry", sprite = secondSprite,
            data = new[] { new Description { language = language.english, dataName = "Angry", description = "Angry portrait" } }
        });
        var happy = character.GetPortrait("happy");
        Assert.That(happy, Is.SameAs(character.GetPortrait(0))); Assert.That(happy.Icon, Is.SameAs(firstSprite));
        Assert.That(happy.GetDescription(language.english), Is.EqualTo("Happy portrait"));
        Assert.That(character.GetPortrait("angry").Icon, Is.SameAs(secondSprite));
        Assert.That(character.GetPortrait(-1), Is.Null); Assert.That(character.GetPortrait(2), Is.Null);
        Assert.That(character.GetPortrait("missing"), Is.Null);
    }

    [Test]
    public void MapsContainItemShapedStageRecordsWithoutInferringGameplayConfiguration()
    {
        var map = Data<MapCodexData>(); var stage = Data<StageCodexData>(); var enemy = Data<EnemyDataSet>();
        stage.map = map; stage.stageNumber = 3; stage.encounters = new[] { enemy }; map.stages = new[] { stage };
        stage.data = new[] { new Description { language = language.english, dataName = "Stage 3", detailDescription = "Authored stage description" } };
        ItemData item = map.stages[0];
        Assert.That(item, Is.SameAs(stage)); Assert.That(stage.map, Is.SameAs(map));
        Assert.That(item.GetDataName(language.english), Is.EqualTo("Stage 3"));
        Assert.That(stage.encounters[0], Is.SameAs(enemy));
        Assert.That(stage.itemRewards, Is.Empty); Assert.That(stage.entryCosts, Is.Empty);
        Assert.That(stage.currencyRewards, Is.Empty);
    }
}
