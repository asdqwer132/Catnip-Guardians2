using TMPro;
using UnityEngine;

public class SubPlantManager : ItemRecipeManager
{
    public static SubPlantManager instance;

    [Header("Throw")]
    public ItemThrowExecutor throwExecutor;

    [Header("Item Pos")]
    public Transform startPos;
    public Transform[] itemPosis;

    private void Awake()
    {
        instance = this;
    }

    public override void Combine() { }

    public void ThrowAllItems()
    {
        if (throwExecutor == null)
        {
            Debug.LogWarning("[SubPlantManager] throwExecutor가 없습니다.");
            return;
        }

        if (startPos == null)
        {
            Debug.LogWarning("[SubPlantManager] startPos가 없습니다.");
            return;
        }

        if (itemPosis == null || itemPosis.Length == 0)
        {
            Debug.LogWarning("[SubPlantManager] itemPosis가 비어있습니다.");
            return;
        }

        for (int i = 0; i < currentMaterials.Count; i++)
        {
            if (currentMaterials[i] == null || currentMaterials[i].itemData == null)
                continue;

            ItemData item = currentMaterials[i].itemData;

            Vector3 throwStartPos = startPos.position;
            throwStartPos.z = 0f;

            Transform targetTransform = itemPosis[i % itemPosis.Length];

            if (targetTransform == null)
                continue;

            Vector3 throwTargetPos = targetTransform.position;
            throwTargetPos.z = 0f;

            Debug.Log($"[SubPlantManager] Throw Item: {item.dataId}");
            Debug.Log($"Start: {throwStartPos}, Target: {throwTargetPos}");

            bool result = throwExecutor.Throw(
                item,
                throwStartPos,
                throwTargetPos,
                gameObject,
                null,
                0
            );

            Debug.Log($"[SubPlantManager] Throw Result: {result}");
        }
    }
}