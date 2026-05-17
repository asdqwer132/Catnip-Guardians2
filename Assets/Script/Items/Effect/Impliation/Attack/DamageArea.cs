using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;

    [Header("Range")]
    public float radius = 1f;
    public CircleCollider2D circleCollider;
    public Transform rangeVisual;

    [Header("Life Time")]
    public float lifeTime = 0.2f;

    [Header("Hit Option")]
    public bool hitOnce = true;

    public GameObject owner;

    private float timer;
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();

    void Awake()
    {
        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (rangeVisual == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                rangeVisual = spriteRenderer.transform;
        }

        ApplyRadius();
    }

    public void Init(
     float damage,
     float radius,
     float lifeTime,
     bool hitOnce,
     GameObject owner
 )
    {
        this.damage = damage;
        this.radius = Mathf.Max(0.01f, radius);
        this.lifeTime = lifeTime;
        this.hitOnce = hitOnce;
        this.owner = owner;

        timer = 0f;
        hitObjects.Clear();

        ApplyRadius();
    }

    private void ApplyRadius()
    {
        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.radius = radius;
            circleCollider.isTrigger = true;
        }

        if (rangeVisual != null)
        {
            rangeVisual.localScale = new Vector3(
                radius * 2f,
                radius * 2f,
                1f
            );
        }

        transform.localScale = Vector3.one;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!hitOnce)
        {
            TryHit(other);
        }
    }

    private void TryHit(Collider2D other)
    {
        if (owner != null && other.gameObject == owner)
            return;

        if (hitOnce && hitObjects.Contains(other.gameObject))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);

        hitObjects.Add(other.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        float drawRadius = radius;

        if (circleCollider != null)
            drawRadius = circleCollider.radius;

        Gizmos.DrawWireSphere(transform.position, drawRadius);
    }
}