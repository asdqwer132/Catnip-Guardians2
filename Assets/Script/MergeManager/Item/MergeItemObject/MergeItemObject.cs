using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
public class MergeItemObject : MonoBehaviour, IPoolable
{
    public ItemData item;
    [Header("플레이어 선택")]
    public bool allowDragSelection = true;
    public bool allowClickSelection = true;
    public bool CanSelectWithPointer(bool dragged) => CanSelect && (dragged ? allowDragSelection : allowClickSelection);

    public Collider2D colider2d;
    public SpriteRenderer spriteRenderer;

    [Header("프리팹별 비주얼 옵션")]
    [Tooltip("켜면 ItemData의 아이콘을 적용합니다. 끄면 프리팹 SpriteRenderer에 지정한 스프라이트를 사용합니다.")]
    public bool useItemIcon = true;

    public Rigidbody2D body;

    [SerializeField] private PooledObject pooled;

    private Vector3 prefabScale;
    private bool hasPrefabScale;

    private bool reserved;
    private object heldBy;
    private bool rootedHold;
    private RigidbodyType2D heldBodyType;
    private bool heldColliderEnabled, heldSimulated;

    public bool IsHeld => heldBy != null;
    public bool IsHeldBy(object holder) => holder != null && ReferenceEquals(heldBy, holder);

    private float unlockAt;

    public MergegameManager Owner { get; private set; }
    public MergeItem Definition { get; private set; }
    public int LifeId { get; private set; }
    public uint Serial { get; private set; }

    public int PoolSpawnId => pooled != null ? pooled.SpawnId : 0;

    public bool IsLive =>
        Owner != null &&
        Definition != null &&
        pooled != null &&
        pooled.IsSpawned &&
        isActiveAndEnabled;

    public bool CanMerge =>
        IsLive && !reserved && !IsHeld && Time.time >= unlockAt;

    public bool CanSelect =>
        IsLive && !reserved && (!IsHeld || rootedHold);

    public Vector2 Position =>
        body != null ? body.position : (Vector2)transform.position;

    public Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = value;
#else
            body.velocity = value;
#endif
        }
    }

    private void Awake()
    {
        Cache();
    }

    private void Cache()
    {
        if (!hasPrefabScale)
        {
            prefabScale = transform.localScale;
            hasPrefabScale = true;
        }

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        // CircleCollider2D / BoxCollider2D 모두 지원
        if (colider2d == null)
            colider2d = GetComponent<Collider2D>();

        if (pooled == null)
            pooled = GetComponent<PooledObject>();
    }

    public virtual void Initialize(
        MergegameManager owner,
        MergeItem definition,
        uint serial,
        float mergeDelay)
    {
        Cache();

        body.simulated = false;

        Owner = owner;
        Definition = definition;
        item = definition.item;
        Serial = serial;

        reserved = false;

        unlockAt = Time.time +
            EffectStatUtility.Safe(mergeDelay, 0f, 60f, 0.1f);

        unchecked
        {
            LifeId++;
        }

        body.position = transform.position;
        body.rotation = transform.eulerAngles.z;

        Velocity = Vector2.zero;
        body.angularVelocity = 0f;

        ApplyRadius(definition.Radius);

        if (colider2d != null)
            colider2d.enabled = true;

        if (spriteRenderer != null)
        {
            if (useItemIcon)
                spriteRenderer.sprite = item.icon;

            spriteRenderer.enabled = spriteRenderer.sprite != null;
        }
    }

    // 콜라이더의 원래 모양과 비주얼의 상대 크기를 유지한 채 전체를 조절합니다.
    private void ApplyRadius(float radius)
    {
        // 풀에서 같은 인스턴스를 다시 써도 이전 아이템의 배율이 누적되지 않습니다.
        transform.localScale = prefabScale;

        float currentRadius;
        if (colider2d is CircleCollider2D circle)
        {
            currentRadius = circle.radius * Mathf.Max(
                circle.transform.TransformVector(Vector3.right).magnitude,
                circle.transform.TransformVector(Vector3.up).magnitude);
        }
        else if (colider2d is BoxCollider2D box)
        {
            currentRadius = Mathf.Max(
                box.transform.TransformVector(Vector3.right * box.size.x).magnitude,
                box.transform.TransformVector(Vector3.up * box.size.y).magnitude) * 0.5f;
        }
        else if (colider2d is CapsuleCollider2D capsule)
        {
            currentRadius = Mathf.Max(
                capsule.transform.TransformVector(Vector3.right * capsule.size.x).magnitude,
                capsule.transform.TransformVector(Vector3.up * capsule.size.y).magnitude) * 0.5f;
        }
        else return;

        if (currentRadius <= 0.0001f) return;
        float scale = radius / currentRadius;
        transform.localScale = new Vector3(prefabScale.x * scale, prefabScale.y * scale, prefabScale.z);
    }

    public void SetSimulated(bool value)
    {
        Cache();
        body.simulated = value && IsLive;
    }

    internal bool Reserve()
    {
        if (!CanMerge)
            return false;

        reserved = true;
        return true;
    }

    internal bool ReserveReplacement(object holder)
    {
        if (IsHeldBy(holder) && IsLive && !reserved)
        {
            reserved = true;
            return true;
        }

        return Reserve();
    }

    public bool TryHold(object holder, bool rooted)
    {
        if (holder == null || IsHeld || !CanSelect)
            return false;

        Cache();

        heldBy = holder;
        rootedHold = rooted;
        heldBodyType = body.bodyType;

        heldColliderEnabled = colider2d.enabled;
        heldSimulated = body.simulated;

        Velocity = Vector2.zero;
        body.angularVelocity = 0f;

        if (rooted)
        {
            body.bodyType = RigidbodyType2D.Static;
        }
        else
        {
            body.simulated = false;
            colider2d.enabled = false;
        }

        return true;
    }

    public bool ReleaseHold(object holder)
    {
        if (heldBy == null || !ReferenceEquals(heldBy, holder))
            return false;

        body.bodyType = heldBodyType;
        body.simulated = heldSimulated;

        colider2d.enabled = heldColliderEnabled;
        heldBy = null;

        return true;
    }

    internal void Unreserve()
    {
        reserved = false;
    }

    public void OnSpawnedFromPool()
    {
        Cache();
        ClearPayload();
    }

    public void OnReturnedToPool()
    {
        Cache();
        ClearPayload();
    }

    protected virtual void ResetPoolState()
    {
    }

    private void ClearPayload()
    {
        if (heldBy != null)
            ReleaseHold(heldBy);

        ResetPoolState();

        var previous = Owner;

        Owner = null;
        Definition = null;
        item = null;
        reserved = false;

        unchecked
        {
            LifeId++;
        }

        body.simulated = false;

        Velocity = Vector2.zero;
        body.angularVelocity = 0f;

        colider2d.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (previous != null)
            previous.NotifyReturned(this);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Contact(collision);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        Contact(collision);
    }

    private void Contact(Collision2D collision)
    {
        if (!IsLive || collision.collider == null)
            return;

        var other =
            collision.collider.GetComponentInParent<MergeItemObject>();

        if (other != null && other.Owner == Owner)
            Owner.QueueContact(this, other);
    }

    public void ReturnToPool()
    {
        if (pooled != null && pooled.OwnerPool != null)
            pooled.OwnerPool.Release(gameObject, pooled.SpawnId);
    }

    private void OnDestroy()
    {
        if (Owner != null)
            Owner.NotifyReturned(this);
    }
}