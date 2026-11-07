using System.Collections;
using UnityEngine;

public class ObjectRandomTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private float diameter = 5f;
    [SerializeField] private float interval = 3f;
    [SerializeField] private float randomOffset = 1f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform root;
    [SerializeField] private Transform target;

    private void Start()
    {
        Teleport();
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        while (true)
        {
            float delay = Mathf.Max(
                0.01f,
                interval + Random.Range(-randomOffset, randomOffset)
            );

            yield return new WaitForSeconds(delay);

            Teleport();
        }
    }

    private void Teleport()
    {
        if (root == null || target == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = diameter * 0.5f;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            0f
        );

        Vector3 position = root.position + offset;
        position.z = target.position.z;

        target.position = position;

        LookAt();
    }

    private void LookAt()
    {
        if (root == null || spriteRenderer == null || target == null)
            return;

        float directionX = root.position.x - target.position.x;

        if (Mathf.Abs(directionX) > 0.001f)
            spriteRenderer.flipX = directionX < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 center = root != null
            ? root.position
            : transform.position;

        Gizmos.DrawWireSphere(center, diameter * 0.5f);
    }
}