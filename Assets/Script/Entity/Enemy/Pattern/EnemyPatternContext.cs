using UnityEngine;

public class EnemyPatternContext
{
    public Enemy Enemy { get; private set; }
    public EnemyPatternRunner Runner { get; private set; }
    public ActorTarget Target { get; private set; }
    public ActorMover Mover { get; private set; }
    public ActorAttack Attack { get; private set; }
    public ActorVisual Visual { get; private set; }

    public Vector3 Position => Enemy != null ? Enemy.transform.position : Vector3.zero;
    public bool HasEnemy => Enemy != null;
    public bool HasTarget => Target != null && Target.HasTarget && Target.TargetTransform != null;

    public Vector2 DirectionToTarget
    {
        get
        {
            Transform target = GetTargetTransform();
            if (Enemy == null || target == null)
                return Vector2.zero;

            Vector2 direction = target.position - Enemy.transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
                return Vector2.zero;

            return direction.normalized;
        }
    }

    public EnemyPatternContext(Enemy enemy, EnemyPatternRunner runner)
    {
        Bind(enemy, runner);
    }

    public void Bind(Enemy enemy, EnemyPatternRunner runner)
    {
        Enemy = enemy;
        Runner = runner;

        if (Enemy == null)
        {
            Target = null;
            Mover = null;
            Attack = null;
            Visual = null;
            return;
        }

        Target = Enemy.actorTarget != null ? Enemy.actorTarget : Enemy.GetComponent<ActorTarget>();
        Mover = Enemy.mover != null ? Enemy.mover : Enemy.GetComponent<ActorMover>();
        Attack = Enemy.attack != null ? Enemy.attack : Enemy.GetComponent<ActorAttack>();
        Visual = Enemy.visual != null ? Enemy.visual : Enemy.GetComponent<ActorVisual>();
    }

    public Transform GetTargetTransform()
    {
        if (Target == null || !Target.HasTarget)
            return null;

        return Target.TargetTransform;
    }

    public IDamageable GetTargetDamageable()
    {
        Transform target = GetTargetTransform();
        if (target == null)
            return null;

        return target.GetComponentInParent<IDamageable>();
    }

    public float GetDistanceToTarget()
    {
        if (Enemy == null || Target == null)
            return float.MaxValue;

        return Target.GetDistanceFrom(Enemy.transform);
    }

    public float GetHpRatio()
    {
        if (Enemy == null || Enemy.health == null)
            return 1f;

        float maxHp = Enemy.health.MaxHp;
        if (maxHp <= 0f)
            return 1f;

        return Mathf.Clamp01(Enemy.health.Hp / maxHp);
    }

    public float GetHp()
    {
        if (Enemy == null || Enemy.health == null)
            return 0f;

        return Enemy.health.Hp;
    }

    public float GetAttackDamage()
    {
        if (Enemy != null)
            return Enemy.GetAttackPower();

        if (Attack != null)
            return Attack.damage;

        return 1f;
    }

    public float GetMoveSpeed()
    {
        if (Mover != null)
            return Mover.speed;

        return 1f;
    }

    public void StopMove()
    {
        if (Mover != null)
            Mover.LockMove();
    }
    public void UnLockMove()
    {
        if (Mover != null)
            Mover.UnlockMove();
    }

    public void CancelDefaultAttack()
    {
        if (Attack != null && Attack.IsAttacking)
            Attack.CancelAttack();
    }

    public void LookDirection(Vector2 direction)
    {
        if (Visual != null)
            Visual.LookDirection(direction);
    }

    public void PlayMove(Vector2 direction)
    {
        if (Visual != null)
            Visual.PlayMove(direction);
    }

    public void PlayAttack(Vector2 direction)
    {
        if (Visual != null)
            Visual.PlayAttack(direction);
    }

    public void PlayHit()
    {
        if (Visual != null)
            Visual.PlayHit();
    }

    public void MoveBy(Vector2 delta, bool playMove = true)
    {
        if (Enemy == null)
            return;

        if (playMove && delta.sqrMagnitude > 0.0001f)
            PlayMove(delta.normalized);

        Enemy.transform.position += (Vector3)delta;
    }

    public void MoveDirection(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            StopMove();
            return;
        }

        MoveBy(direction.normalized * speed * Time.deltaTime, true);
    }

    public void MoveToPosition(Vector3 targetPosition, float speed, float stopDistance = 0.03f)
    {
        if (Enemy == null)
            return;

        Vector2 toTarget = targetPosition - Enemy.transform.position;
        if (toTarget.magnitude <= stopDistance)
        {
            StopMove();
            return;
        }

        MoveDirection(toTarget, speed);
    }

    public bool IsTargetInRadius(float radius)
    {
        Transform target = GetTargetTransform();
        if (Enemy == null || target == null)
            return false;

        return Vector2.Distance(Enemy.transform.position, target.position) <= radius;
    }

    public void DamageTarget(float damage)
    {
        if (Target != null)
            Target.DamageTarget(damage);
    }

    public Vector3 ResolvePoint(EnemyPatternPointType pointType, float distance = 0f, float radius = 0f)
    {
        Vector3 selfPosition = Position;
        Transform target = GetTargetTransform();
        Vector3 targetPosition = target != null ? target.position : selfPosition;

        switch (pointType)
        {
            case EnemyPatternPointType.Target:
                return targetPosition;

            case EnemyPatternPointType.InFrontOfSelf:
            {
                Vector2 dir = DirectionToTarget;
                if (dir.sqrMagnitude <= 0.0001f)
                    dir = Vector2.right;
                return selfPosition + (Vector3)(dir.normalized * distance);
            }

            case EnemyPatternPointType.RandomAroundTarget:
                return targetPosition + (Vector3)(Random.insideUnitCircle * Mathf.Max(0f, radius));

            case EnemyPatternPointType.RandomAroundSelf:
                return selfPosition + (Vector3)(Random.insideUnitCircle * Mathf.Max(0f, radius));

            case EnemyPatternPointType.Self:
            default:
                return selfPosition;
        }
    }

    public void AddRuntimeModifier(EnemyPatternRuntimeModifier modifier)
    {
        if (Runner != null)
            Runner.AddRuntimeModifier(modifier);
    }
}
