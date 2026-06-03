using UnityEngine;

public class BagInitUI : MonoBehaviour
{
    [SerializeField] BagUIBase[] bindings;
    
    public void Init()
    {
        for(int i = 0; i < bindings.Length; i++)
        {
            bindings[i].gameObject.SetActive(UnlockCheckUtility.CanUse(bindings[i].BagData));    
        }
    }
     
}
