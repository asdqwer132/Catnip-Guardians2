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

    #region Target

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

    #endregion

    #region Stat

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

    #endregion

    #region Move Command

    public void StopMove()
    {
        if (Mover != null)
            Mover.Stop();
    }

    public void MoveDirection(Vector2 direction, float speed)
    {
        if (Mover != null)
        {
            Mover.MoveDirection(direction, speed);
            return;
        }

        if (Enemy == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f || speed <= 0f)
        {
            StopMove();
            return;
        }

        Vector2 delta = direction.normalized * speed * Time.deltaTime;
        Enemy.transform.position += (Vector3)delta;

        if (Visual != null)
            Visual.PlayMove(direction.normalized);
    }

    public void MoveBy(Vector2 delta)
    {
        if (Mover != null)
        {
            Mover.MoveBy(delta);
            return;
        }

        if (Enemy == null)
            return;

        if (delta.sqrMagnitude <= 0.0000001f)
        {
            StopMove();
            return;
        }

        Enemy.transform.position += (Vector3)delta;

        if (Visual != null)
            Visual.PlayMove(delta.normalized);
    }

    // 이전 액션 코드 호환용
    public void MoveBy(Vector2 delta, bool playMove)
    {
        MoveBy(delta);
    }

    public void MoveToPosition(Vector3 targetPosition, float speed, float stopDistance = 0.03f)
    {
        if (Mover != null)
        {
            Mover.MoveToPositionWithSpeed(targetPosition, speed, stopDistance);
            return;
        }

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

    public void SetPosition(Vector3 position)
    {
        if (Mover != null)
        {
            Mover.SetPosition(position);
            return;
        }

        if (Enemy != null)
            Enemy.transform.position = position;
    }

    public void Teleport(Vector3 position, Vector2 lookDirection)
    {
        if (Mover != null)
        {
            Mover.Teleport(position, lookDirection);
            return;
        }

        if (Enemy != null)
            Enemy.transform.position = position;

        if (Visual != null)
            Visual.LookDirection(lookDirection);
    }

    public void FaceDirection(Vector2 direction)
    {
        if (Mover != null)
        {
            Mover.FaceDirection(direction);
            return;
        }
    }


    #endregion

    #region Attack / Visual

    public void CancelDefaultAttack()
    {
        if (Attack != null && Attack.IsAttacking)
            Attack.CancelAttack();
    }

    public void TickDefaultAttack()
    {
        if (Attack == null)
            return;

        Attack.TickAttack();
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

    #endregion

    #region Damage

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

    #endregion

    #region Point

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
                    Vector2 direction = DirectionToTarget;

                    if (direction.sqrMagnitude <= 0.0001f)
                        direction = Vector2.right;

                    return selfPosition + (Vector3)(direction.normalized * distance);
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

    #endregion



    #region Runtime Modifier

    public void AddRuntimeModifier(EnemyPatternRuntimeModifier modifier)
    {
        if (Runner != null)
            Runner.AddRuntimeModifier(modifier);
    }

    #endregion
}