using UnityEngine;

public class DebuffZone : MonoBehaviour
{
    public GameObject owner;
    public float duration = 5f;
    public float slowRate = 0.3f;
    public float damagePerSecond = 2f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerStay(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            return;

        //enemy.ApplySlow(slowRate);
        enemy.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}