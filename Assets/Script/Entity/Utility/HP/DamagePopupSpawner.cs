using System.Collections.Generic;
using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    [Header("Popup")]
    public DamagePopup damagePopupPrefab;

    [Header("Limit")]
    [Min(1)] public int maxActivePopupCount = 5;

    [Tooltip("최대 개수에 도달했을 때 가장 오래된 팝업을 지우고 새 팝업을 띄울지 여부")]
    public bool releaseOldestWhenFull = true;

    [Header("Position")]
    public Transform popupPoint;
    public Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("Random")]
    public bool useRandomOffset = true;
    public float randomXRange = 0.25f;
    public float randomYRange = 0.15f;

    private readonly List<DamagePopup> activePopups = new List<DamagePopup>();

    public void ShowDamage(float damage)
    {
        if (damagePopupPrefab == null)
            return;

        if (activePopups.Count >= maxActivePopupCount)
        {
            if (!releaseOldestWhenFull)
                return;

            ReleaseOldestPopup();
        }

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject popupObject = ObjectPoolManager.instance.Spawn(
            damagePopupPrefab.gameObject,
            spawnPosition,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup == null)
            return;

        popup.Init(damage, this);
        activePopups.Add(popup);
    }

    public void OnPopupReleased(DamagePopup popup)
    {
        if (popup == null)
            return;

        activePopups.Remove(popup);
    }

    private void ReleaseOldestPopup()
    {
        if (activePopups.Count <= 0)
            return;

        DamagePopup oldestPopup = activePopups[0];

        if (oldestPopup != null)
            oldestPopup.Release();
        else
            activePopups.RemoveAt(0);
    }

    private Vector3 GetSpawnPosition()
    {
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

        return spawnPosition;
    }
}