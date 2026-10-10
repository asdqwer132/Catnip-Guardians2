using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalThrowArrivalTests
{
    private readonly List<Object> owned = new List<Object>();
    private Random.State randomState;
    private GameObject Host()
    {
        var host = new GameObject("Throw Arrival Test"); owned.Add(host); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private ItemThrowMover Mover()
    {
        ItemThrowMover mover = Host().AddComponent<ItemThrowMover>();
        mover.destroyOnArrive = false; mover.arriveTime = 1f;
        mover.arrivalTiming.mode = ItemThrowArrivalMode.WeightAndRandomOffset;
        mover.arrivalTiming.randomOffset = 0f;
        return mover;
    }
    [SetUp] public void Setup() { randomState = Random.state; ThrowMoverTestProbe.LastCreated = null; }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); Random.state = randomState; ThrowMoverTestProbe.LastCreated = null;
    }

    [Test]
    public void RandomRangeReachesBothLimitsAndIgnoresWeight()
    {
        var timing = new ItemThrowArrivalTiming {
            mode = ItemThrowArrivalMode.RandomRange, minArriveTime = 0.5f, maxArriveTime = 2f,
            distribution = ItemThrowArrivalDistribution.Uniform
        };
        Assert.That(timing.Resolve(1f, 3f, 100f, 0f), Is.EqualTo(0.5f));
        Assert.That(timing.Resolve(1f, 3f, 0f, 1f), Is.EqualTo(2f));
        Assert.That(timing.Resolve(1f, 3f, 0f, 0.25f), Is.EqualTo(timing.Resolve(1f, 3f, 100f, 0.25f)));
    }

    [Test]
    public void DistributionPresetsChangeLikelihoodInsideTheSameRange()
    {
        var timing = new ItemThrowArrivalTiming();
        timing.distribution = ItemThrowArrivalDistribution.PreferShort;
        Assert.That(timing.TransformSample(0.25f), Is.EqualTo(0.0625f));
        timing.distribution = ItemThrowArrivalDistribution.PreferLong;
        Assert.That(timing.TransformSample(0.25f), Is.EqualTo(0.4375f));
        timing.distribution = ItemThrowArrivalDistribution.PreferMiddle;
        Assert.That(timing.TransformSample(0.25f), Is.EqualTo(Mathf.Sqrt(0.125f)).Within(0.00001f));
        timing.distribution = ItemThrowArrivalDistribution.PreferEdges;
        Assert.That(timing.TransformSample(0.25f), Is.EqualTo(0.15625f));
    }

    [Test]
    public void CustomCurveTransformsSamplesRatherThanBeingTreatedAsADensity()
    {
        var timing = new ItemThrowArrivalTiming {
            mode = ItemThrowArrivalMode.RandomRange, minArriveTime = 1f, maxArriveTime = 2f,
            distribution = ItemThrowArrivalDistribution.CustomCurve,
            customCurve = AnimationCurve.Linear(0f, 0.2f, 1f, 0.6f)
        };
        Assert.That(timing.Resolve(1f, 3f, 0f, 0f), Is.EqualTo(1.2f).Within(0.00001f));
        Assert.That(timing.Resolve(1f, 3f, 0f, 0.5f), Is.EqualTo(1.4f).Within(0.00001f));
        Assert.That(timing.Resolve(1f, 3f, 0f, 1f), Is.EqualTo(1.6f).Within(0.00001f));
        timing.customCurve = AnimationCurve.Linear(0f, -1f, 1f, 2f);
        Assert.That(timing.Resolve(1f, 3f, 0f, 0f), Is.EqualTo(1f));
        Assert.That(timing.Resolve(1f, 3f, 0f, 1f), Is.EqualTo(2f));
        timing.customCurve = null;
        Assert.That(timing.Resolve(1f, 3f, 0f, 0.25f), Is.EqualTo(1.25f));
    }

    [Test]
    public void WeightMovesTheOffsetWindowToLaterTimes()
    {
        var timing = new ItemThrowArrivalTiming {
            mode = ItemThrowArrivalMode.WeightAndRandomOffset,
            minArriveTime = 0.5f, maxArriveTime = 3f, randomOffset = 0.2f, secondsPerWeight = 0.25f,
            distribution = ItemThrowArrivalDistribution.Uniform
        };
        Assert.That(timing.Resolve(1f, 3f, 2f, 0f), Is.EqualTo(1.3f).Within(0.00001f));
        Assert.That(timing.Resolve(1f, 3f, 2f, 1f), Is.EqualTo(1.7f).Within(0.00001f));
        for (int i = 0; i <= 100; i++)
            Assert.That(timing.Resolve(1f, 3f, 3f, i / 100f),
                Is.GreaterThanOrEqualTo(timing.Resolve(1f, 3f, 1f, i / 100f)));
    }

    [Test]
    public void WeightOffsetUsesTheChosenDistributionAndClipsBeforeSampling()
    {
        var timing = new ItemThrowArrivalTiming {
            mode = ItemThrowArrivalMode.WeightAndRandomOffset,
            minArriveTime = 1f, maxArriveTime = 2f, randomOffset = 0.2f, secondsPerWeight = 0.5f,
            distribution = ItemThrowArrivalDistribution.PreferShort
        };
        Assert.That(timing.Resolve(1f, 3f, 1f, 0.5f), Is.EqualTo(1.4f).Within(0.00001f));
        Assert.That(timing.Resolve(1f, 3f, 100f, 0.5f), Is.EqualTo(1.85f).Within(0.00001f));
        Assert.That(timing.Resolve(1f, 3f, 100f, 1f), Is.EqualTo(2f));
    }

    [Test]
    public void FixedAndZeroOffsetModesAreDeterministic()
    {
        var timing = new ItemThrowArrivalTiming();
        Assert.That(timing.Resolve(0.7f, 3f, 100f, 0f), Is.EqualTo(0.7f));
        timing.mode = ItemThrowArrivalMode.WeightAndRandomOffset; timing.randomOffset = 0f;
        Assert.That(timing.Resolve(1f, 3f, 2f, 0f), Is.EqualTo(timing.Resolve(1f, 3f, 2f, 1f)));
    }

    [Test]
    public void InvalidValuesAndReversedBoundsRemainFiniteAndPositive()
    {
        var timing = new ItemThrowArrivalTiming {
            mode = ItemThrowArrivalMode.WeightAndRandomOffset,
            minArriveTime = 2f, maxArriveTime = 1f, secondsPerWeight = float.PositiveInfinity,
            randomOffset = float.NaN
        };
        Assert.That(timing.Resolve(float.NaN, 3f, float.NaN, float.NaN), Is.EqualTo(1f));
        timing.mode = ItemThrowArrivalMode.RandomRange;
        Assert.That(timing.Resolve(1f, 0.5f, 0f, 0.5f), Is.EqualTo(0.5f));
        Assert.That(ItemThrowArrivalTiming.ClampDuration(float.NaN, -1f), Is.EqualTo(0.01f));
    }

    [Test]
    public void MoverCapturesWeightAndSettingsOnlyOnceAndCallsArrivalOnce()
    {
        ItemThrowMover mover = Mover(); int arrivals = 0;
        mover.Init(Vector3.zero, new Vector3(3f, 2f, 0f), null, () => arrivals++, itemWeight: 2f);
        Assert.That(mover.MoveDuration, Is.EqualTo(1.4f).Within(0.00001f));
        Assert.That(mover.speed, Is.EqualTo(3f / 1.4f).Within(0.00001f));
        mover.arrivalTiming.secondsPerWeight = 100f; mover.arriveTime = 100f;
        mover.Tick(0.7f);
        Assert.That(mover.transform.position.x, Is.EqualTo(1.5f).Within(0.00001f));
        Assert.That(arrivals, Is.Zero);
        mover.Tick(0.7f); mover.Tick(10f);
        Assert.That(arrivals, Is.EqualTo(1));
        Assert.That(mover.transform.position, Is.EqualTo(new Vector3(3f, 2f, 0f)));
        Assert.That(mover.IsMoving, Is.False);
    }

    [Test]
    public void DirectionRotationFollowsArcAndOverridesSpinThroughArrival()
    {
        ItemThrowMover mover = Mover();
        mover.faceMoveDirection = true; mover.spinWhileMoving = true;
        mover.directionAngleOffset = 0f; mover.autoArcHeightByDistance = false;
        mover.arcHeight = 2f; mover.arcPeakProgress = 0.5f;
        mover.InitMove(Vector3.zero, new Vector3(4f, 0f, 0f), 1f, null);
        Assert.That(Vector3.Angle(mover.transform.right, new Vector3(4f, 8f, 0f)), Is.LessThan(0.01f));
        mover.Tick(0.5f);
        Assert.That(Vector3.Angle(mover.transform.right, Vector3.right), Is.LessThan(0.01f));
        mover.Tick(0.5f);
        Assert.That(Vector3.Angle(mover.transform.right, new Vector3(4f, -8f, 0f)), Is.LessThan(0.01f));
    }

    [Test]
    public void DirectionRotationSupportsLeftVerticalAndStationaryThrowsWithOffset()
    {
        ItemThrowMover mover = Mover(); mover.faceMoveDirection = true;
        mover.autoArcHeightByDistance = false; mover.arcHeight = 0f;
        mover.InitMove(Vector3.zero, Vector3.left, 1f, null);
        Assert.That(Vector3.Angle(mover.transform.up, Vector3.left), Is.LessThan(0.01f));
        mover.InitMove(Vector3.zero, Vector3.up, 1f, null);
        mover.Tick(0.25f);
        Assert.That(Vector3.Angle(mover.transform.up, Vector3.up), Is.LessThan(0.01f));
        Quaternion previous = mover.transform.rotation;
        mover.InitMove(Vector3.zero, Vector3.zero, 1f, null);
        mover.Tick(1f);
        Assert.That(Quaternion.Angle(previous, mover.transform.rotation), Is.LessThan(0.01f));
    }

    [TestCase(false, 3f)]
    [TestCase(false, -3f)]
    [TestCase(true, 3f)]
    [TestCase(true, -3f)]
    public void ZeroArcMovesInAStraightLineAndKeepsFacingTheTarget(bool automatic, float targetY)
    {
        ItemThrowMover mover = Mover(); mover.faceMoveDirection = true;
        mover.directionAngleOffset = 0f; mover.autoArcHeightByDistance = automatic;
        mover.arcHeight = mover.minArcHeight = mover.maxArcHeight = mover.arcHeightDistanceMultiplier = 0f;
        mover.arcPeakProgress = 0.1f;
        Vector3 start = new Vector3(-2f, 1f, 0f);
        Vector3 target = new Vector3(4f, targetY, 0f);
        int arrivals = 0;
        mover.InitMove(start, target, 1f, () => arrivals++);
        Assert.That(Vector3.Angle(mover.transform.right, target - start), Is.LessThan(0.01f));
        for (int step = 1; step <= 4; step++)
        {
            mover.Tick(0.25f);
            Assert.That(Vector3.Distance(mover.transform.position, Vector3.Lerp(start, target, step / 4f)),
                Is.LessThan(0.0001f));
            Assert.That(Vector3.Angle(mover.transform.right, target - start), Is.LessThan(0.01f));
        }
        Assert.That(arrivals, Is.EqualTo(1));
    }

    [Test]
    public void DefaultRotationStillSpinsAndCanBeDisabled()
    {
        ItemThrowMover mover = Mover(); Assert.That(mover.faceMoveDirection, Is.False);
        mover.spinSpeed = 90f;
        mover.InitMove(Vector3.zero, Vector3.right, 1f, null);
        mover.Tick(0.5f);
        Assert.That(Mathf.DeltaAngle(mover.transform.eulerAngles.z, 45f), Is.EqualTo(0f).Within(0.01f));
        mover.spinWhileMoving = false; mover.Tick(0.25f);
        Assert.That(Mathf.DeltaAngle(mover.transform.eulerAngles.z, 45f), Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void ExplicitFlightTimeBypassesRandomAndWeightSettings()
    {
        ItemThrowMover mover = Mover(); int arrivals = 0;
        mover.InitMove(Vector3.zero, Vector3.right, 0.3f, () => arrivals++);
        Assert.That(mover.MoveDuration, Is.EqualTo(0.3f));
        mover.Tick(0.3f); Assert.That(arrivals, Is.EqualTo(1));
    }

    [Test]
    public void DisabledMoverCancelsItsArrivalCallback()
    {
        ItemThrowMover mover = Mover(); int arrivals = 0;
        mover.Init(Vector3.zero, Vector3.right, null, () => arrivals++, itemWeight: 1f);
        mover.gameObject.SetActive(false);
        // EditMode does not guarantee play-only MonoBehaviour callbacks.
        typeof(ItemThrowMover).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(mover, null);
        mover.Tick(10f);
        Assert.That(arrivals, Is.Zero);
        Assert.That(mover.IsMoving, Is.False);
    }

    [Test]
    public void ThrowExecutorPassesItemWeightAndRunsEffectsOnlyAfterArrival()
    {
        ItemThrowMover prefab = Mover(); prefab.gameObject.AddComponent<ThrowMoverTestProbe>();
        ThrowMoverTestProbe.LastCreated = null;
        ItemThrowExecutor thrower = Host().AddComponent<ItemThrowExecutor>();
        thrower.throwMoverPrefab = prefab; thrower.showTargetRange = false;
        thrower.itemEffectExecutor = Host().AddComponent<ItemEffectExecutor>();
        CombatEffectRecordingEffect effect = Asset<CombatEffectRecordingEffect>();
        ItemData item = Asset<ItemData>(); item.weight = 2f; item.effectDatas = new ItemEffectData[] { effect };
        Assert.That(thrower.Throw(item, Vector3.zero, Vector3.right, null, null, 0, triggerSpecialItems: false), Is.True);
        ItemThrowMover mover = ThrowMoverTestProbe.LastCreated;
        Assert.That(mover, Is.Not.Null); owned.Add(mover.gameObject);
        Assert.That(mover.MoveDuration, Is.EqualTo(1.4f).Within(0.00001f));
        item.weight = 100f;
        Assert.That(effect.calls, Is.Empty);
        mover.Tick(0.7f); Assert.That(effect.calls, Is.Empty);
        mover.Tick(0.7f); Assert.That(effect.calls.Count, Is.EqualTo(1));
    }
}
