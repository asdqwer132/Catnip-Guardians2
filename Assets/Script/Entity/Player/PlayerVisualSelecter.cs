using System;
using System.Xml.Linq;
using UnityEngine;

public class PlayerVisualSelecter : MonoBehaviour
{

    //[Serializable]
    //public class PlayerVisuals
    //{
    //    [SerializeField] private void 
    //}
    //[SerializeField] private PlayerVisuals[] playerVisuals;

    [SerializeField] private AnimationController[] playerVisuals;
    int index = 0;


    private void Start()
    {
        if (PlantManager.instance == null)
        {
            Debug.Log("PM is null");
            return;
        }
        index = PlantManager.instance.CurrentPlantIndex;
        SetActiveObject(index);
    }


    private void SetActiveObject(int index)
    {
        for (int i = 0; i < playerVisuals.Length; i++)
        {
            if (playerVisuals[i] == null) continue;
            playerVisuals[i].gameObject.SetActive(i == index);
            if (i == index) playerVisuals[i].PlayOnStart();
        }
    }


}
