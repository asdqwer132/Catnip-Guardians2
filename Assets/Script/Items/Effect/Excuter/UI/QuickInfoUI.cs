using System;
using UnityEngine;

[Serializable]
public class ObjectUIBinding
{
    public int index;
    public GameObject pannels;
}
public class QuickInfoUI : MonoBehaviour
{

    [SerializeField] private int selectedIndex;

    [Header("Pannels")]
    public ObjectUIBinding[] pannels;

    [Header("Default")]
    public bool initClose = true;

    private bool isOpened;

    private void Start()
    {
        isOpened = !initClose;
        RefreshPannels();
    }

    public void SetPannels()
    {
        isOpened = !isOpened;
        RefreshPannels();
    }

    public void SetIndex(int index) { selectedIndex = index; RefreshPannels(); }

    private void RefreshPannels()
    {
        if (pannels == null)
            return;



        for (int i = 0; i < pannels.Length; i++)
        {
            ObjectUIBinding binding = pannels[i];

            if (binding == null || binding.pannels == null)
                continue;

            binding.pannels.SetActive(isOpened && binding.index == selectedIndex);
        }
    }

}
