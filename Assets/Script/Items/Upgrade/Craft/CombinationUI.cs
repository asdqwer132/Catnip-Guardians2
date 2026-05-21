using UnityEngine;

public class CombinationUI : MonoBehaviour
{
    //UI ¿¬°á
    public void CombineButton() => ItemCombinationManager.instance.Combine();
    public void ClearButton() => ItemCombinationManager.instance.ReturnMaterials();
}