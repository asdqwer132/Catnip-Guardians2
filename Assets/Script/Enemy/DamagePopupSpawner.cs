using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    [Header("Popup")]
    public DamagePopup damagePopupPrefab;

    [Header("Position")]
    public Transform popupPoint;
    public Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("Random")]
    public bool useRandomOffset = true;
    public float randomXRange = 0.25f;
    public float randomYRange = 0.15f;

    public void ShowDamage(float damage)
    {
        if (damagePopupPrefab == null)
            return;

        Vector3 spawnPosition;

        if (popupPoint != null)
            spawnPosition = popupPoint.position;
        else
            spawnPosition = transform.position + offset;

        if (useRandomOffset)
        {
            spawnPosition.x += Random.Range(-randomXRange, randomXRange);
            spawnPosition.y += Random.Range(0f, randomYRange);
        }

        DamagePopup popup = Instantiate(
            damagePopupPrefab,
            spawnPosition,
            Quaternion.identity
        );

        popup.Init(damage);
    }
}