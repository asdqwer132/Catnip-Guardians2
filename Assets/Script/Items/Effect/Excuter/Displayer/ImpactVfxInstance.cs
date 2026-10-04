using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ImpactVfxInstance : MonoBehaviour, IPoolable
{
    [Header("Component")]
    [SerializeField] private Animator animator;

    private EffectVisualContext visualContext;
    private Transform scaleRoot;
    private Vector3 baseScale;
    private bool useRadiusScale;
    private float lifeTime;
    private float timer;
    private bool initialized;
    private bool resolvingAnimatorLifeTime;
    private Coroutine lifeTimeRoutine;

    private EffectVisualAnchor anchor;
    private PooledObject pooledRoot;
    private int spawnId;
    private bool returning;
    private bool componentsCached;
    private Animator[] animators;
    private float[] initialAnimatorSpeeds;
    private ParticleSystem[] particles;
    private ParticleSystemSimulationSpace[] initialSimulationSpaces;
    private Transform[] initialCustomSimulationSpaces;
    private bool[] initialTrailWorldSpaces;
    private Transform[] visualTransforms;
    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;
    private Vector3[] initialScales;
    private bool[] initialActiveStates;
    private readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>(4);

    public Transform FollowTarget => visualContext != null ? visualContext.followTarget : null;
    public Transform PlaybackRoot => scaleRoot != null ? scaleRoot : transform;

    private void Awake()
    {
        CacheComponents();
    }

    public void Init(
        EffectVisualContext context,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime,
        bool useAnimatorClipLifeTime,
        Transform scaleRoot = null,
        bool attachParticlesToFollowTarget = true
    )
    {
        StopLifeTimeRoutine();
        CacheComponents();
        returning = false;
        BindPoolLease(scaleRoot);
        visualContext = context;
        this.baseScale = baseScale;
        this.useRadiusScale = useRadiusScale;
        // 다른 재생의 루트를 위치/크기 갱신에 사용하지 않는다.
        this.scaleRoot = scaleRoot != null &&
            (scaleRoot == transform || transform.IsChildOf(scaleRoot))
            ? scaleRoot : (anchor != null ? anchor.transform : transform);
        this.lifeTime = SafeLifeTime(lifeTime);
        timer = 0f;
        initialized = context != null;
        RefreshFollowPosition();
        RefreshScale();
        ConfigureParticleFollow(attachParticlesToFollowTarget &&
            context != null && context.hasFollowTarget);

        if (initialized && useAnimatorClipLifeTime &&
            animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController != null && !TrySetAnimatorLifeTime() &&
            initialized && isActiveAndEnabled && visualContext == context)
        {
            resolvingAnimatorLifeTime = true;
            lifeTimeRoutine = StartCoroutine(ResolveAnimatorLifeTime());
        }
    }

    // 기존 사용자 스크립트/프리팹은 같은 Init 인자로 호출해도 동작한다.
    public void Init(
        ItemEffectData effectData,
        ItemEffectContext context,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime,
        bool useAnimatorClipLifeTime = true
    )
    {
        EffectVisualContext compatibleContext = new EffectVisualContext(
            transform.position,
            transform.rotation,
            scaleResolver: (scale, useRadius) =>
                effectData != null && context != null
                    ? effectData.GetCurrentImpactScale(context, scale, useRadius)
                    : scale
        );
        Init(compatibleContext, baseScale, useRadiusScale, lifeTime, useAnimatorClipLifeTime);
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (!visualContext.CanContinuePlayback)
        {
            ReturnToPool();
            return;
        }

        // Animator가 첫 클립을 준비하는 최대 한 프레임 동안은 수명 타이머를 대기한다.
        if (resolvingAnimatorLifeTime)
            return;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            ReturnToPool();
    }

    private void LateUpdate()
    {
        // 공유 에셋/앵커의 추적 상태를 참조하지 않고 이 재생의 대상만 읽는다.
        RefreshFollowPosition();
        RefreshScale();
    }

    private void RefreshFollowPosition()
    {
        if (!initialized || returning || scaleRoot == null || visualContext == null ||
            !visualContext.hasFollowTarget || !visualContext.CanContinuePlayback)
            return;

        if (pooledRoot != null &&
            (!pooledRoot.IsSpawned || pooledRoot.SpawnId != spawnId))
            return;

        Vector3 position = visualContext.followTarget.position;
        position.z = 0f;
        scaleRoot.position = position;
    }

    public void RefreshScale()
    {
        if (!initialized || returning || scaleRoot == null || visualContext == null ||
            !visualContext.CanContinuePlayback || (pooledRoot != null &&
                (!pooledRoot.IsSpawned || pooledRoot.SpawnId != spawnId)))
            return;

        scaleRoot.localScale = visualContext.GetCurrentScale(baseScale, useRadiusScale);
    }

    private IEnumerator ResolveAnimatorLifeTime()
    {
        yield return null;
        if (initialized)
            TrySetAnimatorLifeTime();
        resolvingAnimatorLifeTime = false;
        lifeTimeRoutine = null;
    }

    private bool TrySetAnimatorLifeTime()
    {
        if (animator == null || !animator.isActiveAndEnabled ||
            animator.runtimeAnimatorController == null || animator.layerCount == 0)
            return false;

        EffectVisualContext expectedContext = visualContext;
        PooledObject expectedRoot = pooledRoot;
        int expectedSpawnId = spawnId;
        animator.Update(0f);
        if (!initialized || visualContext != expectedContext || (expectedRoot != null &&
            (!expectedRoot.IsSpawned || expectedRoot.SpawnId != expectedSpawnId)))
            return false;
        clipInfos.Clear();
        animator.GetCurrentAnimatorClipInfo(0, clipInfos);
        if (clipInfos.Count == 0)
            return false;

        int selectedIndex = 0;
        for (int i = 1; i < clipInfos.Count; i++)
        {
            if (clipInfos[i].weight > clipInfos[selectedIndex].weight)
                selectedIndex = i;
        }

        AnimationClip clip = clipInfos[selectedIndex].clip;
        if (clip == null || clip.length <= 0f)
            return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float speed = Mathf.Abs(animator.speed * state.speed * state.speedMultiplier);
        if (speed <= 0.0001f || float.IsNaN(speed) || float.IsInfinity(speed))
            return false;

        lifeTime = SafeLifeTime(clip.length / speed);
        return true;
    }

    private void CacheComponents()
    {
        if (componentsCached)
            return;

        componentsCached = true;
        if (animator == null || (animator.transform != transform &&
            !animator.transform.IsChildOf(transform)))
            animator = GetComponentInChildren<Animator>(true);

        animators = GetComponentsInChildren<Animator>(true);
        initialAnimatorSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++)
            initialAnimatorSpeeds[i] = animators[i].speed;

        particles = GetComponentsInChildren<ParticleSystem>(true);
        initialSimulationSpaces = new ParticleSystemSimulationSpace[particles.Length];
        initialCustomSimulationSpaces = new Transform[particles.Length];
        initialTrailWorldSpaces = new bool[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            initialSimulationSpaces[i] = main.simulationSpace;
            initialCustomSimulationSpaces[i] = main.customSimulationSpace;
            initialTrailWorldSpaces[i] = particles[i].trails.worldSpace;
        }
        visualTransforms = GetComponentsInChildren<Transform>(true);
        initialPositions = new Vector3[visualTransforms.Length];
        initialRotations = new Quaternion[visualTransforms.Length];
        initialScales = new Vector3[visualTransforms.Length];
        initialActiveStates = new bool[visualTransforms.Length];
        for (int i = 0; i < visualTransforms.Length; i++)
        {
            Transform visual = visualTransforms[i];
            initialPositions[i] = visual.localPosition;
            initialRotations[i] = visual.localRotation;
            initialScales[i] = visual.localScale;
            initialActiveStates[i] = visual.gameObject.activeSelf;
        }
    }

    public void OnSpawnedFromPool()
    {
        CacheComponents();
        ClearRuntime();
        returning = false;
        BindPoolLease();
        RestoreVisualTransforms();
        ConfigureParticleFollow(false);

        PooledObject expectedRoot = pooledRoot;
        int expectedSpawnId = spawnId;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator current = animators[i];
            if (current == null || current.runtimeAnimatorController == null)
                continue;
            current.speed = initialAnimatorSpeeds[i];
            current.Rebind();
            if (current.isActiveAndEnabled)
                current.Update(0f);
        }

        if (!isActiveAndEnabled || (expectedRoot != null &&
            (!expectedRoot.IsSpawned || expectedRoot.SpawnId != expectedSpawnId)))
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            // 프리팹의 자동 Destroy/Disable이 풀링된 자식을 없애지 않도록 수명은 이 컴포넌트가 담당한다.
            ParticleSystem.MainModule main = particle.main;
            main.stopAction = ParticleSystemStopAction.None;
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (main.playOnAwake && particle.gameObject.activeInHierarchy)
                particle.Play(false);
        }
    }

    public void OnReturnedToPool()
    {
        returning = true;
        ClearRuntime();
        StopParticles();
    }

    public void ReturnToPool()
    {
        if (returning)
            return;
        returning = true;

        if (anchor != null)
        {
            anchor.ReturnToPool();
            return;
        }

        if (pooledRoot != null && pooledRoot.HasPoolOwner)
        {
            if (!pooledRoot.IsSpawned || pooledRoot.SpawnId != spawnId)
                return;
            if (pooledRoot.OwnerPool != null)
            {
                pooledRoot.OwnerPool.Release(pooledRoot.gameObject, spawnId);
                return;
            }
            pooledRoot.gameObject.SetActive(false);
            Destroy(pooledRoot.gameObject);
            return;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void BindPoolLease(Transform requestedRoot = null)
    {
        pooledRoot = ObjectPoolManager.FindManagedRoot(gameObject);
        anchor = requestedRoot != null
            ? requestedRoot.GetComponent<EffectVisualAnchor>() : null;
        if (anchor != null && !anchor.OwnsVisual(this))
            anchor = null;
        if (anchor == null && pooledRoot != null)
            anchor = pooledRoot.GetComponent<EffectVisualAnchor>();
        if (anchor == null)
            anchor = GetComponentInParent<EffectVisualAnchor>();
        if (anchor != null && !anchor.OwnsVisual(this))
            anchor = null;
        spawnId = pooledRoot != null ? pooledRoot.SpawnId : 0;
    }

    private void ConfigureParticleFollow(bool attach)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            ParticleSystemSimulationSpace space = initialSimulationSpaces[i];
            Transform customSpace = initialCustomSimulationSpaces[i];
            bool trailWorldSpace = initialTrailWorldSpaces[i];
            if (attach && scaleRoot != null)
            {
                // 각 파티클은 자기 Transform을 기준으로 시뮬레이션한다.
                // 그 부모인 이 재생의 루트가 이동하면 이미 방출된 파티클도 함께 이동한다.
                space = ParticleSystemSimulationSpace.Local;
                customSpace = null;
                trailWorldSpace = false;
            }

            ParticleSystem.MainModule main = particle.main;
            ParticleSystem.TrailModule trails = particle.trails;
            if (main.simulationSpace == space && main.customSimulationSpace == customSpace &&
                trails.worldSpace == trailWorldSpace)
                continue;

            // 재생 중 공간을 바꾸면 이미 방출된 파티클이 이동할 수 있으므로 처음부터 재생한다.
            bool wasPlaying = particle.isPlaying;
            bool wasPaused = particle.isPaused;
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.simulationSpace = space;
            main.customSimulationSpace = customSpace;
            trails.worldSpace = trailWorldSpace;
            if ((wasPlaying || wasPaused) && particle.gameObject.activeInHierarchy)
            {
                particle.Play(false);
                if (wasPaused)
                    particle.Pause(false);
            }
        }
    }

    private void RestoreVisualTransforms()
    {
        for (int i = 0; i < visualTransforms.Length; i++)
        {
            Transform visual = visualTransforms[i];
            if (visual == null)
                continue;
            visual.localPosition = initialPositions[i];
            visual.localRotation = initialRotations[i];
            visual.localScale = initialScales[i];
            visual.gameObject.SetActive(initialActiveStates[i]);
        }
    }

    private void StopParticles()
    {
        if (!componentsCached)
            return;
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
                particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void ClearRuntime()
    {
        StopLifeTimeRoutine();
        initialized = false;
        visualContext = null;
        scaleRoot = null;
        anchor = null;
        pooledRoot = null;
        spawnId = 0;
        timer = 0f;
        lifeTime = 0f;
        clipInfos.Clear();
    }

    private static float SafeLifeTime(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Max(0.01f, value);
    }

    private void StopLifeTimeRoutine()
    {
        if (lifeTimeRoutine != null)
            StopCoroutine(lifeTimeRoutine);
        lifeTimeRoutine = null;
        resolvingAnimatorLifeTime = false;
    }

    private void OnDisable()
    {
        ClearRuntime();
        if (!returning)
            StopParticles();
    }
}
