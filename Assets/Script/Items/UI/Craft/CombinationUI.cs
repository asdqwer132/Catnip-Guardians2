using UnityEngine;

public class CombinationUI : MonoBehaviour
{
    public void CombineButton()
    {
        ItemCombinationManager.instance.Combine();
    }

    public void ClearButton()
    {
        ItemCombinationManager.instance.ReturnMaterials();
    }
}