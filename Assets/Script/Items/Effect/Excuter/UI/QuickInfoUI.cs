using System;
using UnityEngine;

public class QuickInfoUI : MonoBehaviour
{
    [Serializable]
    public class ObjectUIBinding
    {
        public int index;
        public GameObject pannels;
    }

    [Header("Manager")]
    public BagSelectManager bagSelectManager;

    [Header("Pannels")]
    public ObjectUIBinding[] pannels;

    [Header("Default")]
    public bool initClose = true;

    private bool isOpened;

    private void OnEnable()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected += HandleBagSelected;
    }

    private void OnDisable()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected -= HandleBagSelected;
    }

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

    private void HandleBagSelected(int bagIndex)
    {
        if (!isOpened)
            return;

        RefreshPannels();
    }

    private void RefreshPannels()
    {
        if (pannels == null)
            return;

        int currentBagIndex = GetCurrentBagIndex();

        for (int i = 0; i < pannels.Length; i++)
        {
            ObjectUIBinding binding = pannels[i];

            if (binding == null || binding.pannels == null)
                continue;

            binding.pannels.SetActive(isOpened && binding.index == currentBagIndex);
        }
    }

    private int GetCurrentBagIndex()
    {
        if (bagSelectManager == null)
            return -1;

        return bagSelectManager.CurrentBagIndex;
    }
}