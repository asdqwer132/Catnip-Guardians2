using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class MergeContainerTokenView : MonoBehaviour
{
    public MergeContainerRuntime Container { get; private set; }
    public int TokenId { get; private set; }
    public Rigidbody2D Body { get; private set; }
    public float Radius { get; private set; }
    private SpriteRenderer visual;
    public void Bind(MergeContainerRuntime container, int id, MergeContainerItem definition, Sprite fallback, Material material)
    {
        Container = container; TokenId = id;
        if (Body == null) Body = GetComponent<Rigidbody2D>();
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        Body.simulated = true; Body.gravityScale = 1f; Body.velocity = Vector2.zero; Body.angularVelocity = 0f;
        Body.rotation = 0f;
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Radius = Mathf.Clamp(definition.radius, 0.08f, Mathf.Min(container.Stats.width, container.Stats.height) * 0.2f);
        var collider = GetComponent<CircleCollider2D>(); collider.enabled = true; collider.radius = Radius;
        // 콜라이더는 실제 반경, 스프라이트만 별도 자식에서 맞춥니다.
        transform.localScale = Vector3.one;
        visual.enabled = false;
        var child = transform.Find("Icon");
        if (child == null) { child = new GameObject("Icon").transform; child.SetParent(transform, false); child.gameObject.layer = gameObject.layer; }
        visual = child.GetComponent<SpriteRenderer>();
        if (visual == null) visual = child.gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = definition.item.icon != null ? definition.item.icon : fallback;
        visual.sharedMaterial = material; visual.sortingOrder = 5;
        float size = Mathf.Max(0.01f, Mathf.Max(visual.sprite.bounds.size.x, visual.sprite.bounds.size.y));
        child.localScale = Vector3.one * (Radius * 2f / size);
        child.localPosition = Vector3.zero; visual.enabled = true; SetSelected(false);
    }
    public void SetSelected(bool selected)
    { if (visual != null) visual.color = selected ? new Color(1f, 0.75f, 0.25f) : Color.white; }
    public void Release()
    {
        Container = null; TokenId = 0;
        if (Body != null) { Body.simulated = false; Body.velocity = Vector2.zero; Body.angularVelocity = 0f; }
        GetComponent<CircleCollider2D>().enabled = false; gameObject.SetActive(false);
    }
    private void OnCollisionEnter2D(Collision2D collision) => Contact(collision);
    private void OnCollisionStay2D(Collision2D collision) => Contact(collision);
    private void Contact(Collision2D collision)
    {
        var other = collision.gameObject.GetComponent<MergeContainerTokenView>();
        if (Container != null && other != null && other.Container == Container && TokenId < other.TokenId)
            Container.TryMerge(this, other);
    }
}
