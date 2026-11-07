using System.Collections;
using UnityEngine;

public class ObjectRandomTeleporter : MonoBehaviour
{

    [Header("Teleport Interval")]
    [SerializeField] private float diameter = 5f;
    [SerializeField] private float interval = 3f;
    [SerializeField] private float randomOffset = 1f;


    [Header("UI")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform root;
    private Vector3 centerPosition;

    private void Start()
    {
        centerPosition = root != null ? root.position : transform.position;
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        while (true)
        {
            float delay = Mathf.Max(0.01f,
                interval + Random.Range(-randomOffset, randomOffset));

            yield return new WaitForSeconds(delay);

            Teleport();
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            diameter * 0.5f
        );
    }
    private void Teleport()
    {
        Vector2 offset = Random.insideUnitCircle * (diameter * 0.5f);

        transform.position = centerPosition +
            new Vector3(offset.x, offset.y, 0f);
        LookAt();
    }
    private void LookAt()
    {

        if (root == null || spriteRenderer == null) return;

        float directionX = root.position.x - transform.position.x;

        if (Mathf.Abs(directionX) > 0.001f)
            spriteRenderer.flipX = directionX < 0f;
    }
}
