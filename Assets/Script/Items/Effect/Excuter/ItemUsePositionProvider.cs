using UnityEngine;

public class ItemUsePositionProvider : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Use Start Position")]
    public Transform useStartPoint;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public Vector3 GetUseStartPosition(GameObject owner)
    {
        Vector3 position;

        if (useStartPoint != null)
        {
            position = useStartPoint.position;
        }
        else if (owner != null)
        {
            position = owner.transform.position;
        }
        else
        {
            position = transform.position;
        }

        position.z = 0f;
        return position;
    }

    public Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;

        return worldPosition;
    }
}