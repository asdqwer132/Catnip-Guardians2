using UnityEngine;

public class ItemImpactVfx : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public ParticleSystem particle;

    [Header("Life Time")]
    public bool destroyAutomatically = true;
    public float lifeTime = 1f;

    private float timer;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (particle == null)
            particle = GetComponentInChildren<ParticleSystem>();
    }

    public void Play(ItemData itemData, Vector3 position, float radius)
    {
        //transform.position = position;

        //if (itemData != null)
        //    lifeTime = itemData.impactVfxLifeTime;

        //if (itemData != null && itemData.scaleImpactVfxByRadius)
        //{
        //    float diameter = Mathf.Max(0.01f, radius * 2f);
        //    transform.localScale = new Vector3(diameter, diameter, 1f);
        //}

        //if (animator != null)
        //    animator.Play(0, 0, 0f);

        //if (particle != null)
        //    particle.Play();
    }

    void Update()
    {
        if (!destroyAutomatically)
            return;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }
}