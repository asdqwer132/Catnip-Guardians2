using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownObjectActiver : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] GameObject[] objects;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        dropdown.onValueChanged.AddListener(Activate);
    }
    private void Start()
    {
        Activate(dropdown.value);
    }

    public void Activate(int index)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects[i];
            if (go != null)
            {
                go.SetActive(i==index);
            }
        }

    }
}
