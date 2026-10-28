using UnityEngine;

public class ObjectAutoPlacer : MonoBehaviour
{
    [SerializeField] private Transform[] objects = new Transform[8];

    [SerializeField] private Vector3 startPosition;
    public Vector3 offset = new Vector3(1f, 0f, 0f);

    private void Awake()
    {
        Arrange();
    }

    [ContextMenu("Arrange Objects")]
    public void Arrange()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
                continue;

            objects[i].localPosition = startPosition + offset * i;
        }
    }
}