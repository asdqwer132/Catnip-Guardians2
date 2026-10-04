using System.Collections.Generic;
using UnityEngine;

// ObjectPoolManager에 자동으로 추가한다. 원본 연출 프리팹마다 비활성 템플릿을 한 번 만든다.
[DisallowMultipleComponent]
public class EffectVisualPool : MonoBehaviour
{
    private sealed class TemplateEntry
    {
        public GameObject prefab;
        public int prewarmedCount;
    }

    private readonly Dictionary<ImpactVfxInstance, TemplateEntry> templates =
        new Dictionary<ImpactVfxInstance, TemplateEntry>();
    private ObjectPoolManager owner;
    private Transform templatesParent;

    public static EffectVisualAnchor Spawn(
        ImpactVfxInstance prefab,
        Vector3 position,
        Quaternion rotation,
        int prewarmCount = 0
    )
    {
        if (prefab == null)
            return null;

        ObjectPoolManager manager = ObjectPoolManager.instance;
        if (manager == null)
            return CreateUnpooled(prefab, position, rotation);

        EffectVisualPool service = GetService(manager);
        TemplateEntry entry = service.GetTemplate(prefab);
        service.EnsurePrewarmed(entry, prewarmCount);

        // 동시에 사용 중인 객체는 Spawn이 다시 꺼내지 않는다.
        GameObject root = manager.Spawn(entry.prefab, position, rotation);
        return root != null ? root.GetComponent<EffectVisualAnchor>() : null;
    }

    public static void Prewarm(ImpactVfxInstance prefab, int count)
    {
        ObjectPoolManager manager = ObjectPoolManager.instance;
        if (manager == null || prefab == null || count <= 0)
            return;

        EffectVisualPool service = GetService(manager);
        service.EnsurePrewarmed(service.GetTemplate(prefab), count);
    }

    private static EffectVisualPool GetService(ObjectPoolManager manager)
    {
        EffectVisualPool service = manager.GetComponent<EffectVisualPool>();
        if (service == null)
            service = manager.gameObject.AddComponent<EffectVisualPool>();
        service.owner = manager;
        return service;
    }

    private TemplateEntry GetTemplate(ImpactVfxInstance source)
    {
        TemplateEntry entry;
        if (templates.TryGetValue(source, out entry) && entry.prefab != null)
            return entry;

        if (templatesParent == null)
        {
            GameObject container = new GameObject("EffectVisualTemplates");
            container.SetActive(false);
            container.transform.SetParent(transform, false);
            templatesParent = container.transform;
        }

        GameObject root = new GameObject(source.name + "_PooledVisual");
        root.SetActive(false);
        root.transform.SetParent(templatesParent, false);

        PooledObject pooled = root.AddComponent<PooledObject>();
        pooled.SetPoolGroup(PoolObjectGroup.Effect);
        EffectVisualAnchor anchor = root.AddComponent<EffectVisualAnchor>();

        // 원본 프리팹/에셋을 수정하지 않고 비활성 자식 복제본만 사용한다.
        ImpactVfxInstance visual = CreateVisualChild(source, root.transform);
        anchor.SetVisualTemplate(visual);

        entry = new TemplateEntry { prefab = root };
        templates[source] = entry;
        return entry;
    }

    private void EnsurePrewarmed(TemplateEntry entry, int requestedCount)
    {
        int count = Mathf.Max(0, requestedCount);
        if (count <= entry.prewarmedCount)
            return;
        owner.Prewarm(entry.prefab, count);
        entry.prewarmedCount = count;
    }

    private static EffectVisualAnchor CreateUnpooled(
        ImpactVfxInstance prefab, Vector3 position, Quaternion rotation
    )
    {
        GameObject root = new GameObject("EffectVisualAnchor");
        root.SetActive(false);
        root.transform.SetPositionAndRotation(position, rotation);
        EffectVisualAnchor anchor = root.AddComponent<EffectVisualAnchor>();
        ImpactVfxInstance visual = CreateVisualChild(prefab, root.transform);
        anchor.SetVisualTemplate(visual);

        root.SetActive(true);
        visual.OnSpawnedFromPool();
        return anchor;
    }

    private static ImpactVfxInstance CreateVisualChild(ImpactVfxInstance prefab, Transform parent)
    {
        ImpactVfxInstance visual = Object.Instantiate(prefab, parent);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visual.enabled = true;
        visual.gameObject.SetActive(true);

        ParticleSystem[] particles = visual.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        return visual;
    }
}
