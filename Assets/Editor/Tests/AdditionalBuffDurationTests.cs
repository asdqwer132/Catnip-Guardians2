using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalBuffDurationTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;

    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }

    [SetUp]
    public void SetUp()
    {
        previousManager = BuffManager.instance;
        var host = new GameObject("Buff Duration Test");
        owned.Add(host);
        manager = host.AddComponent<BuffManager>();
        if (manager.Storage == null) manager.SendMessage("Awake", SendMessageOptions.RequireReceiver);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        BuffManager.instance = previousManager;
    }

    private FloatFieldBuffModifier Modifier(string type, string field, float add, float multiply)
    {
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = type;
        modifier.fieldName = field;
        modifier.addValue = add;
        modifier.multiplyValue = multiply;
        return modifier;
    }

    private ItemData DurationBooster(float add, float multiply, BuffApplyTiming timing = BuffApplyTiming.Dynamic)
    {
        ItemData item = Asset<ItemData>();
        BuffEffect effect = Asset<BuffEffect>();
        effect.targetResolver = Asset<AllItemsBuffTargetResolver>();
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        effect.buffInfo.applyTiming = timing;
        effect.includeSelf = false;
        effect.modifiers = new BuffModifier[] { Modifier(nameof(BuffInfo), nameof(BuffInfo.duration), add, multiply) };
        item.effectDatas = new ItemEffectData[] { effect };
        return item;
    }

    private ItemData FoodWithEightSecondBuff()
    {
        // Food1과 같은 8초의 DamageArea 공격력 버프를 테스트용 에셋으로 준비합니다.
        ItemData item = Asset<ItemData>();
        BuffEffect effect = Asset<BuffEffect>();
        BuffTargetGroupResolver resolver = Asset<BuffTargetGroupResolver>();
        resolver.targetGroup = "DamageArea";
        effect.targetResolver = resolver;
        effect.buffInfo.duration = 8f;
        effect.modifiers = new BuffModifier[] {
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), 0f, 0.1f)
        };
        item.effectDatas = new ItemEffectData[] { effect };
        return item;
    }

    private void Use(ItemData item)
        => ItemEffectExecutor.ExecuteItem(item, Vector3.zero, Vector3.zero, Vector3.right,
            null, null, manager, triggerSpecialItems: false);

    private ActiveBuff ActiveFrom(ItemData item)
    {
        ActiveBuff result = manager.Storage.activeBuffs.Find(buff => buff.sourceItemData == item);
        Assert.That(result, Is.Not.Null);
        return result;
    }

    [TestCase(2f, 0f, 10f, BuffApplyTiming.Dynamic)]
    [TestCase(0f, 0.5f, 12f, BuffApplyTiming.Dynamic)]
    [TestCase(2f, 0.5f, 15f, BuffApplyTiming.Dynamic)]
    [TestCase(0f, 0.5f, 12f, BuffApplyTiming.Snapshot)]
    public void DurationModifierChangesActualGrantedBuffOnceAndKeepsBaseAsset(
        float add, float multiply, float expected, BuffApplyTiming timing)
    {
        ItemData food = FoodWithEightSecondBuff();
        Use(DurationBooster(add, multiply, timing));
        Use(food);

        // 등록 후의 실제 남은 시간을 확인합니다. Snapshot/Dynamic 모두 한 번만 적용되어야 합니다.
        ActiveBuff granted = ActiveFrom(food);
        Assert.That(granted.maxTime, Is.EqualTo(expected).Within(0.0001f));
        Assert.That(granted.remainTime, Is.EqualTo(expected).Within(0.0001f));
        Assert.That(((BuffEffect)food.effectDatas[0]).buffInfo.duration, Is.EqualTo(8f));
        Assert.That(manager.GetBuffedStatForItem(new DamageAreaAttackStat { damageAreaPower = 5f }, food, null)
            .damageAreaPower, Is.EqualTo(5f), "지속시간 증가는 공격력을 변경하면 안 됩니다.");
    }

    [Test]
    public void BoosterLeavesRunningTimeAloneAndRemovalRestoresFutureGrants()
    {
        ItemData food = FoodWithEightSecondBuff();
        Use(food);
        ActiveBuff granted = ActiveFrom(food);
        granted.Tick(2f);

        ItemData booster = DurationBooster(0f, 0.5f);
        Use(booster);
        Assert.That(granted.maxTime, Is.EqualTo(8f));
        Assert.That(granted.remainTime, Is.EqualTo(6f), "기존 버프의 남은 시간은 그대로입니다.");

        Use(food); // Refresh 재부여는 증가된 부여 시간으로 다시 시작합니다.
        Assert.That(ActiveFrom(food), Is.SameAs(granted));
        Assert.That(granted.maxTime, Is.EqualTo(12f));
        Assert.That(granted.remainTime, Is.EqualTo(12f));
        manager.RemoveBuffHandle(ActiveFrom(booster));
        Assert.That(granted.remainTime, Is.EqualTo(12f), "증가 효과가 사라져도 이미 부여된 시간은 유지합니다.");
        Use(food);
        Assert.That(granted.remainTime, Is.EqualTo(8f));
    }

    [Test]
    public void DurationBoosterExcludesItsOwnItemAndDoesNotAddUseCharges()
    {
        ItemData booster = DurationBooster(0f, 0.5f);
        Use(booster);
        Assert.That(manager.GetBuffedStatForItem(new BuffInfo { duration = 8f }, booster, null).duration,
            Is.EqualTo(8f), "Include Self가 꺼져 있으면 부여 아이템 자신은 제외합니다.");

        ItemData food = FoodWithEightSecondBuff();
        BuffEffect effect = (BuffEffect)food.effectDatas[0];
        effect.buffInfo.useLimitType = BuffUseLimitType.UseCount;
        effect.buffInfo.maxUseCount = 3;
        Use(food);
        Assert.That(ActiveFrom(food).remainUseCount, Is.EqualTo(3), "지속시간 증가는 사용 횟수를 증가시키지 않습니다.");
    }

    [Test]
    public void SpecialStatusGrantAlsoUsesExtendedDurationAndAddsTheNewGrantAmount()
    {
        Use(DurationBooster(0f, 0.5f));
        ItemData item = Asset<ItemData>();
        GrantSpecialStatusEffect effect = Asset<GrantSpecialStatusEffect>();
        effect.specialStatusKey = Asset<SpecialStatusDefinition>();
        effect.target = SpecialStatusQueryTarget.SourceItem;
        effect.lifetimeSettings.duration = 8f;
        item.effectDatas = new ItemEffectData[] { effect };

        Use(item);
        ActiveBuff granted = ActiveFrom(item);
        Assert.That(granted.remainTime, Is.EqualTo(12f));
        Use(item); // 특수 상태의 기본 AddRemaining은 늘어난 12초를 추가합니다.
        Assert.That(ActiveFrom(item), Is.SameAs(granted));
        Assert.That(granted.remainTime, Is.EqualTo(24f));
        Assert.That(effect.lifetimeSettings.duration, Is.EqualTo(8f));
    }
}
