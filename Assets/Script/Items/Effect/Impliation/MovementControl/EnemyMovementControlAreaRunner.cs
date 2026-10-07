using System;
using System.Collections.Generic;
using UnityEngine;

// 한 씬의 지속 영역을 함께 관리한다. 적별 코루틴/영역별 GameObject가 필요 없다.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class EnemyMovementControlAreaRunner : MonoBehaviour
{
    private static EnemyMovementControlAreaRunner instance;
    private readonly List<AreaState> activeAreas = new List<AreaState>();
    private readonly Stack<AreaState> inactiveAreas = new Stack<AreaState>();

    public int ActiveAreaCount => activeAreas.Count;

    internal static void StartArea(
        EnemyMovementControlEffect.AreaSettings settings,
        MovementControlAreaContext area,
        float lifetime,
        float interval, ItemEffectContext context)
    {
        if (instance == null || !instance.isActiveAndEnabled)
        {
            GameObject host = new GameObject("EnemyMovementControlAreas");
            instance = host.AddComponent<EnemyMovementControlAreaRunner>();
        }
        instance.AddArea(settings, area, lifetime, interval, context);
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void AddArea(
        EnemyMovementControlEffect.AreaSettings settings,
        MovementControlAreaContext area,
        float lifetime,
        float interval, ItemEffectContext context)
    {
        AreaState state = inactiveAreas.Count > 0 ? inactiveAreas.Pop() : new AreaState();
        state.Initialize(settings, area, lifetime, interval, context);
        activeAreas.Add(state);
        try
        {
            // 아이템 사용 시 즉시 한 번 적용. 이후 Update에서 간격에 맞춰 갱신한다.
            state.ApplyPulse();
        }
        catch (Exception exception)
        {
            if (activeAreas.Remove(state))
                ReturnArea(state);
            Debug.LogException(exception, this);
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            return;

        // 중첩 아이템 사용으로 새로 추가된 영역은 다음 Update부터 갱신한다.
        for (int i = activeAreas.Count - 1; i >= 0; i--)
        {
            AreaState state = activeAreas[i];
            bool keep;
            try
            {
                keep = state.Tick(deltaTime);
            }
            catch (Exception exception)
            {
                keep = false;
                Debug.LogException(exception, this);
            }

            // 적용 콜백에서 실행기가 비활성화된 경우 이미 모든 영역이 반환되었다.
            if (i >= activeAreas.Count || !ReferenceEquals(activeAreas[i], state))
                return;
            if (!keep)
            {
                activeAreas.RemoveAt(i);
                ReturnArea(state, state.Completed);
            }
        }
    }

    private void ReturnArea(AreaState state, bool completed = false)
    {
        state.Clear(completed);
        inactiveAreas.Push(state);
    }

    private void OnDisable()
    {
        ClearAreas();
    }

    private void OnDestroy()
    {
        ClearAreas();
        if (instance == this)
            instance = null;
    }

    private void ClearAreas()
    {
        for (int i = activeAreas.Count - 1; i >= 0; i--)
            ReturnArea(activeAreas[i]);
        activeAreas.Clear();
    }

    internal sealed class AreaState
    {
        private const float TimeEpsilon = 0.000001f;
        private EnemyMovementControlEffect.AreaSettings settings;
        private MovementControlAreaContext area;
        private float remainingLifetime;
        private float tickInterval;
        private float sinceLastQuery;
        private bool active;
        private ItemEffectContext context;
        private ItemEffectLease lease;
        internal bool Completed { get; private set; }

        private readonly EnemyMovementControlEffect.QueryBuffer buffer =
            new EnemyMovementControlEffect.QueryBuffer();
        private readonly Dictionary<Enemy, int> affected = new Dictionary<Enemy, int>();
        private readonly List<Enemy> removed = new List<Enemy>();

        internal bool IsActive => active;

        internal void Initialize(
            EnemyMovementControlEffect.AreaSettings nextSettings,
            MovementControlAreaContext nextArea,
            float lifetime,
            float interval, ItemEffectContext context)
        {
            this.context = context;
            lease = context != null ? context.RetainLifetime() : null;
            Completed = false;
            settings = nextSettings;
            area = nextArea;
            remainingLifetime = lifetime;
            tickInterval = interval;
            sinceLastQuery = 0f;
            active = true;
        }

        internal bool Tick(float deltaTime)
        {
            if (!active || (context != null && !context.CanContinue))
                return false;
            remainingLifetime = Mathf.Max(0f, remainingLifetime - deltaTime);
            if (remainingLifetime <= TimeEpsilon)
            {
                Completed = true;
                return false;
            }

            sinceLastQuery += deltaTime;
            if (sinceLastQuery + TimeEpsilon >= tickInterval)
            {
                // 큰 프레임의 누락 횟수를 한꺼번에 재생하지 않아 물리 탐색 폭증을 막는다.
                sinceLastQuery %= tickInterval;
                if (sinceLastQuery + TimeEpsilon >= tickInterval)
                    sinceLastQuery = 0f;
                ApplyPulse();
            }
            return active;
        }

        internal void ApplyPulse()
        {
            if (!active)
                return;
            try
            {
                EnemyMovementControlEffect.ApplyMovementControl(
                    settings, area, buffer, this, affected);
                RemoveExpiredOrExitedTargets();
            }
            finally
            {
                buffer.Clear();
            }
        }

        private void RemoveExpiredOrExitedTargets()
        {
            removed.Clear();
            foreach (KeyValuePair<Enemy, int> pair in affected)
            {
                Enemy enemy = pair.Key;
                bool currentLife = enemy != null && enemy.HitEffectLifeId == pair.Value;
                bool valid = currentLife && enemy.CanReceiveHitEffects && !enemy.movementControlImmune &&
                    enemy.movementControlResistance < 1f && enemy.mover != null &&
                    enemy.mover.isActiveAndEnabled && enemy.mover.IsMovementControlledBy(this);
                if (valid && (!settings.releaseOnExit || buffer.enemies.Contains(enemy)))
                    continue;

                if (currentLife && enemy.mover != null)
                    enemy.mover.CancelMovementControl(this);
                removed.Add(enemy);
            }
            for (int i = 0; i < removed.Count; i++)
                affected.Remove(removed[i]);
            removed.Clear();
        }

        internal void Clear(bool completed = false)
        {
            active = false;
            if (lease != null) lease.Finish(completed);
            lease = null;
            context = null;
            foreach (KeyValuePair<Enemy, int> pair in affected)
            {
                Enemy enemy = pair.Key;
                if (enemy != null && enemy.HitEffectLifeId == pair.Value && enemy.mover != null)
                    enemy.mover.CancelMovementControl(this);
            }
            affected.Clear();
            removed.Clear();
            buffer.Clear();
            settings = default(EnemyMovementControlEffect.AreaSettings);
            area = default(MovementControlAreaContext);
            remainingLifetime = 0f;
            sinceLastQuery = 0f;
        }
    }
}
