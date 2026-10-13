using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PrefabVisualTests
{
    private readonly List<Object> owned = new List<Object>();
    private GameObject Host(string name = "Prefab Visual Test")
    { var host = new GameObject(name); owned.Add(host); return host; }
    private T Asset<T>() where T : ScriptableObject
    { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private PrefabVisualAnimator Animator(out GameObject root, out SpriteRenderer sprite)
    {
        root = Host(); root.SetActive(false);
        var child = new GameObject("Visual"); child.transform.SetParent(root.transform, false);
        sprite = child.AddComponent<SpriteRenderer>(); sprite.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
        var animator = root.AddComponent<PrefabVisualAnimator>(); animator.visualTarget = child.transform;
        animator.entrance.duration = 0.1f; animator.exit.duration = 0.1f;
        root.SetActive(true); return animator;
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
    [Test]
    public void AreaCreationNeedsADefinitionAndHasNoSecondStatOrVisualConfiguration()
    {
        var effect = Asset<AreaEffectData>();
        var context = new ItemEffectContext(null,Asset<ItemData>(),Vector3.zero,Vector3.zero,null);
        Assert.That(effect.TryExecute(context), Is.False, "정의가 없으면 생성 성공이나 연출 완료로 취급하지 않습니다.");
        effect.areaDefinition = Asset<AreaDefinition>();
        Assert.That(effect.BaseStat, Is.SameAs(effect.areaDefinition.stat));
        foreach (string field in new[] { "groundStat", "overrideDefinitionStat", "areaVisualPrefab", "scaleVisualByRadius" })
            Assert.That(typeof(AreaEffectData).GetField(field), Is.Null, "중복 설정이 Inspector에 다시 노출되지 않아야 합니다.");
    }
    [Test]
    public void AreaTakesItsVisualAndScalingOptionFromTheDefinition()
    {
        GameObject template; SpriteRenderer sprite; var animation = Animator(out template, out sprite);
        var definition = Asset<AreaDefinition>(); definition.visualPrefab = template;
        definition.stat.groundRadius = 3f; definition.scaleVisualByRadius = false;
        var effect = Asset<AreaEffectData>(); effect.areaDefinition = definition;
        var first = Host().AddComponent<ReactiveGroundArea>();
        first.Init(effect,new ItemEffectContext(null,Asset<ItemData>(),Vector3.zero,Vector3.zero,null));
        Assert.That(first.ControlledRangeVisual, Is.Not.Null);
        Assert.That(first.ControlledRangeVisual.localScale, Is.EqualTo(Vector3.one));
        definition.scaleVisualByRadius = true;
        var second = Host().AddComponent<ReactiveGroundArea>();
        second.Init(effect,new ItemEffectContext(null,Asset<ItemData>(),Vector3.zero,Vector3.zero,null));
        Assert.That(second.ControlledRangeVisual.localScale, Is.EqualTo(new Vector3(6f,6f,1f)));
        var runtimeAnimation = second.ControlledRangeVisual.GetComponent<PrefabVisualAnimator>();
        Assert.That(runtimeAnimation.Play(), Is.True); runtimeAnimation.Advance(0.1f);
        float remaining; Assert.That(second.TryGetVisualLifetime(out remaining), Is.True);
        Assert.That(remaining, Is.EqualTo(definition.stat.groundLifetime));
    }
    [Test]
    public void VisualAnimationLeavesColliderAndRangeParentAloneAndRestoresOnDisable()
    {
        GameObject root; SpriteRenderer sprite; var animation = Animator(out root, out sprite);
        var rangeRoot = new GameObject("RangeVisual"); rangeRoot.transform.SetParent(root.transform, false);
        sprite.transform.SetParent(rangeRoot.transform, false); rangeRoot.transform.localScale = new Vector3(4f,4f,1f);
        var collider = root.AddComponent<CircleCollider2D>(); collider.radius = 2f;
        var damage = root.AddComponent<DamageArea>(); damage.rangeVisual = rangeRoot.transform;
        sprite.transform.localScale = Vector3.one;
        damage.InitWithSnapshotAndDynamicBuff(new DamageAreaAttackStat { damageAreaRange = 2f, damageAreaLifeTime = 10f }, null, null, null, null);
        animation.loop.rotate = true; animation.loop.pulse = true; animation.loop.sway = true;
        Assert.That(animation.Play(), Is.True); animation.Advance(0.2f);
        Assert.That(collider.radius, Is.EqualTo(2f)); Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(rangeRoot.transform.localScale, Is.EqualTo(new Vector3(4f,4f,1f)));
        Assert.That(sprite.transform.localRotation, Is.Not.EqualTo(Quaternion.identity));
        root.SetActive(false);
        Assert.That(sprite.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(sprite.transform.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(sprite.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(sprite.color, Is.EqualTo(new Color(0.2f,0.4f,0.6f,0.8f)));
    }
    [Test]
    public void VisualRejectsRootColliderAndGameplayControlledRangeTransform()
    {
        GameObject root; SpriteRenderer sprite; var animation = Animator(out root, out sprite);
        animation.visualTarget = root.transform; Assert.That(animation.Play(), Is.False);
        animation.visualTarget = sprite.transform; var collider = sprite.gameObject.AddComponent<BoxCollider2D>();
        Assert.That(animation.Play(), Is.False); Object.DestroyImmediate(collider);
        var area = root.AddComponent<DamageArea>(); area.rangeVisual = sprite.transform;
        Assert.That(animation.Play(), Is.False);
    }
    [Test]
    public void ExplicitExitCompletesOnceAndDoesNotDespawnGameplayObject()
    {
        GameObject root; SpriteRenderer sprite; var animation = Animator(out root, out sprite);
        animation.entrance.enabled = false; int completed = 0;
        animation.onExitCompleted.AddListener(() => completed++);
        animation.Play(); animation.PlayExit(); animation.Advance(0.05f); animation.PlayExit();
        animation.Advance(0.05f); animation.Advance(1f);
        Assert.That(completed, Is.EqualTo(1)); Assert.That(root.activeSelf, Is.True);
        Assert.That(sprite.color.a, Is.Zero); animation.Stop(); Assert.That(sprite.color.a, Is.EqualTo(0.8f));
    }
    [Test]
    public void CommonAnimationReusesOneObjectAndRestartsItsColorsAndClock()
    {
        ObjectPoolManager previous = ObjectPoolManager.instance;
        var manager = Host("Visual Pool Test").AddComponent<ObjectPoolManager>(); manager.SendMessage("Awake");
        GameObject prefab; SpriteRenderer templateSprite; Animator(out prefab, out templateSprite); prefab.SetActive(false);
        GameObject instance = null;
        try
        {
            GameObject first = manager.Spawn(prefab, Vector3.zero, Quaternion.identity);
            instance = first;
            var animation = first.GetComponent<PrefabVisualAnimator>(); animation.Advance(0.1f);
            Assert.That(first.GetComponentInChildren<SpriteRenderer>().color.a, Is.EqualTo(0.8f));
            int oldSpawn = first.GetComponent<PooledObject>().SpawnId; manager.Release(first, oldSpawn);
            GameObject second = manager.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.That(second, Is.SameAs(first)); second.GetComponent<PrefabVisualAnimator>().Advance(0f);
            Assert.That(second.GetComponentInChildren<SpriteRenderer>().color.a, Is.Zero);
            manager.Release(first, oldSpawn); Assert.That(second.activeSelf, Is.True, "이전 재생의 반환은 새 재생을 건드리지 않습니다.");
            second.GetComponent<PrefabVisualAnimator>().Advance(0.1f);
            Assert.That(second.GetComponentInChildren<SpriteRenderer>().color.a, Is.EqualTo(0.8f));
            manager.Release(second);
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
            ObjectPoolManager.instance = previous;
        }
    }
    [Test]
    public void RectangleGizmoUsesItsRotatedBoxRatherThanTheDisabledCircle()
    {
        var root = Host(); root.transform.SetPositionAndRotation(new Vector3(3f,4f,0f), Quaternion.Euler(0f,0f,90f));
        var circle = root.AddComponent<CircleCollider2D>(); var area = root.AddComponent<DirectionalRectangleDamageArea>();
        area.SetRectangleSize(2f,4f); var ranges = new List<PrefabVisualRange>(); area.CollectPrefabRanges(ranges);
        Assert.That(circle.enabled, Is.False); Assert.That(ranges.Count, Is.EqualTo(1));
        Assert.That(ranges[0].shape, Is.EqualTo(PrefabRangeShape.Rectangle));
        Assert.That(Vector3.Distance(ranges[0].center,new Vector3(1f,4f,0f)), Is.LessThan(0.0001f));
        Assert.That(Vector3.Distance(ranges[0].corner0,new Vector3(3f,3f,0f)), Is.LessThan(0.0001f));
    }
    [Test]
    public void SectorGizmoMatchesRadiusAndForwardAngle()
    {
        var root = Host(); var circle = root.AddComponent<CircleCollider2D>(); var area = root.AddComponent<SectorDamageArea>();
        root.transform.rotation = Quaternion.Euler(0f,0f,-90f); circle.radius = 3f; area.sectorAngle = 90f;
        var ranges = new List<PrefabVisualRange>(); area.CollectPrefabRanges(ranges);
        Assert.That(ranges.Count, Is.EqualTo(1)); Assert.That(ranges[0].radius, Is.EqualTo(3f));
        Assert.That(ranges[0].shape, Is.EqualTo(PrefabRangeShape.Sector));
        Assert.That(Vector3.Distance(ranges[0].ArcPoint(0.5f),Vector3.right*3f), Is.LessThan(0.0001f));
    }
    [Test]
    public void DefinedAndConsumableAreaGizmoUsesWorldRadiusAndReadsRuntimeLifetime()
    {
        var definition = Asset<AreaDefinition>(); definition.stat.groundRadius = 2.5f; definition.stat.groundLifetime = 8f;
        var effect = Asset<AreaEffectData>(); effect.areaDefinition = definition;
        var area = Host().AddComponent<ReactiveGroundArea>(); area.transform.localScale = new Vector3(7f,7f,1f);
        area.Init(effect,new ItemEffectContext(null,Asset<ItemData>(),Vector3.zero,Vector3.zero,null));
        var ranges = new List<PrefabVisualRange>(); area.CollectPrefabRanges(ranges);
        Assert.That(ranges.Count, Is.EqualTo(1)); Assert.That(ranges[0].radius, Is.EqualTo(2.5f), "실제 조회는 월드 반경을 사용하므로 Visual의 크기를 다시 곱하지 않습니다.");
        float remaining; Assert.That(area.TryGetVisualLifetime(out remaining), Is.True); Assert.That(remaining, Is.EqualTo(8f));
        area.Consume(); Assert.That(area == null, Is.True);
    }
    [Test]
    public void GenericGizmoFindsAreaSourceThroughTheVisualPrefabParent()
    {
        var effect = Asset<ReactiveGroundEffect>(); effect.areaDefinition = Asset<AreaDefinition>();
        effect.areaDefinition.stat.groundRadius = 3f;
        var source = Host().AddComponent<ReactiveGroundArea>();
        source.Init(effect,new ItemEffectContext(null,Asset<ItemData>(),Vector3.zero,Vector3.zero,null));
        var child = new GameObject("Visual"); child.transform.SetParent(source.transform,false);
        var gizmo = child.AddComponent<PrefabRangeGizmo>(); var ranges = new List<PrefabVisualRange>(); gizmo.CollectRanges(ranges);
        Assert.That(ranges.Count, Is.EqualTo(1)); Assert.That(ranges[0].radius, Is.EqualTo(3f));
    }
}
