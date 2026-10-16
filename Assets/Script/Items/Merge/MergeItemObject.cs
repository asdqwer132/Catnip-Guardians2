using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PooledObject))]
public class MergeItemObject : MonoBehaviour, IPoolable
{
    public ItemData item;
    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D body;
    private PooledObject pooled;
    private bool reserved;
    private float unlockAt;
    public MergegameManager Owner { get; private set; }
    public MergeItem Definition { get; private set; }
    public int LifeId { get; private set; }
    public uint Serial { get; private set; }
    public int PoolSpawnId => pooled != null ? pooled.SpawnId : 0;
    public bool IsLive => Owner != null && Definition != null && pooled != null && pooled.IsSpawned && isActiveAndEnabled;
    public bool CanMerge => IsLive && !reserved && Time.time >= unlockAt;
    public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
    public Vector2 Velocity
    {
        get {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
        set {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = value;
#else
            body.velocity = value;
#endif
        }
    }
    private void Awake() => Cache();
    private void Cache()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
        if (pooled == null) pooled = GetComponent<PooledObject>();
    }
    public void Initialize(MergegameManager owner, MergeItem definition, uint serial, float mergeDelay)
    {
        Cache();
        body.simulated = false;
        Owner = owner; Definition = definition; item = definition.item; Serial = serial;
        reserved = false; unlockAt = Time.time + EffectStatUtility.Safe(mergeDelay, 0f, 60f, 0.1f);
        unchecked { LifeId++; }
        body.position = transform.position;
        body.rotation = transform.eulerAngles.z;
        Velocity = Vector2.zero; body.angularVelocity = 0f;
        circleCollider.radius = definition.Radius; circleCollider.enabled = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = item.icon;
            spriteRenderer.enabled = item.icon != null;
            // 물리 루트는 건드리지 않고 프리팹의 Visual 자식만 반지름에 맞춥니다.
            if (spriteRenderer.transform != transform && item.icon != null)
            {
                Vector3 size = item.icon.bounds.size;
                float scale = definition.Radius * 2f / Mathf.Max(0.0001f, size.x, size.y);
                spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
    public void SetSimulated(bool value) { Cache(); body.simulated = value && IsLive; }
    internal bool Reserve() { if (!CanMerge) return false; reserved = true; return true; }
    internal void Unreserve() => reserved = false;
    public void OnSpawnedFromPool() { Cache(); ClearPayload(); }
    public void OnReturnedToPool() { Cache(); ClearPayload(); }
    private void ClearPayload()
    {
        var previous = Owner;
        Owner = null; Definition = null; item = null; reserved = false;
        unchecked { LifeId++; }
        body.simulated = false; Velocity = Vector2.zero; body.angularVelocity = 0f;
        circleCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (previous != null) previous.NotifyReturned(this);
    }
    private void OnCollisionEnter2D(Collision2D collision) => Contact(collision);
    private void OnCollisionStay2D(Collision2D collision) => Contact(collision);
    private void Contact(Collision2D collision)
    {
        if (!IsLive || collision.collider == null) return;
        var other = collision.collider.GetComponentInParent<MergeItemObject>();
        if (other != null && other.Owner == Owner) Owner.QueueContact(this, other);
    }
    public void ReturnToPool()
    {
        if (pooled != null && pooled.OwnerPool != null) pooled.OwnerPool.Release(gameObject, pooled.SpawnId);
    }
    private void OnDisable()
    {
        // 외부에서 피스를 숨겨도 활성 목록/풀의 대여 상태가 남지 않게 합니다.
        if (Owner != null) ReturnToPool();
    }
    private void OnDestroy() { if (Owner != null) Owner.NotifyReturned(this); }
}
