using UnityEngine;

public class EnemySimpleProjectile : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;
    [SerializeField] private LayerMask targetLayerMask;

    [Header("Option")]
    public bool destroyOnHit = true;
    public bool canPierce = false;
    [Min(1)] public int pierceCount = 1;

    private Vector2 direction;
    private GameObject owner;
    private bool initialized;
    private int hitCount;

    public void Init(GameObject ownerObject, Vector2 shootDirection, float projectileDamage, float projectileSpeed, float projectileLifeTime, LayerMask hitLayerMask)
    {
        owner = ownerObject;
        direction = shootDirection.sqrMagnitude <= 0.0001f ? Vector2.right : shootDirection.normalized;
        damage = projectileDamage;
        speed = projectileSpeed;
        lifeTime = Mathf.Max(0.05f, projectileLifeTime);
        targetLayerMask = hitLayerMask;
        initialized = true;
        hitCount = 0;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 수명은 이동과 같은 시계를 사용해 시간 정지 중 소멸하지 않는다.
    }

    private void Update() { Tick(Time.deltaTime); }

    public void Tick(float deltaTime)
    {
        if (!initialized || TimeStopRuntime.IsStopped(TimeStopTargets.EnemyProjectiles))
            return;

        deltaTime = Mathf.Max(0f, deltaTime);
        lifeTime -= deltaTime;
        if (lifeTime <= 0f) { initialized = false; Destroy(gameObject); return; }
        transform.position += (Vector3)(direction * speed * deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized || TimeStopRuntime.IsStopped(TimeStopTargets.EnemyProjectiles))
            return;

        if (owner != null && other.transform.IsChildOf(owner.transform))
            return;

        if (((1 << other.gameObject.layer) & targetLayerMask.value) == 0)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead)
            return;

        damageable.TakeDamage(damage);
        hitCount++;

        if (!canPierce || destroyOnHit || hitCount >= pierceCount)
            Destroy(gameObject);
    }
}
