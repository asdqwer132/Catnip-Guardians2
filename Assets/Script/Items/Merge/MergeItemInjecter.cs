using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MergeItemInjecter : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] MergegameManager mergegameManager;

    [Header("Datas")]
    [SerializeField] MergeItem[] mergeItems;
    [SerializeField] GameObject mergeUIRoot;
    [SerializeField] MergeUI[] mergeUIs;

    [Header("Settings")]
    [SerializeField] int count = 10;
    [SerializeField] int defaultCount = 10;
    
    [ContextMenu("Inject")]
    public void Inject()
    {
        mergeUIs = mergeUIRoot.GetComponentsInChildren<MergeUI>();

        List<MergeItemMaterial> materials = new List<MergeItemMaterial>();
        for(int i=0;i<mergeItems.Length && i< mergeUIs.Length; i++)
        {
            materials.Add(new MergeItemMaterial(
                mergeItems[i],
                mergeUIs[i],
                count,
                defaultCount
                ));
        }
        mergegameManager.mergeItems = mergeItems.ToList();
        mergegameManager.mergeMaterials = materials;
    }
}
