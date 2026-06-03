using UnityEngine;

public class BagUIInitializer : MonoBehaviour
{
    [Header("Bag UIs")]
    public BagUIBase[] bagUIs;
    public BagInitUI bagInitUI;

    public void InitAll()
    {
        for (int i = 0; i < bagUIs.Length; i++)
        {
            if (bagUIs[i] == null)
                continue;

            bagUIs[i].Init();
        }
        bagInitUI.Init();
    }
}