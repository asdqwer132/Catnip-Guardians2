using UnityEngine;
using static UnityEditor.Progress;

public class DefaultData : ScriptableObject, IUnlockable
{
    [Header("Basic Info")]
    public string dataName;
    public Sprite icon;
    [TextArea] public string description;


    [Header("Id Info")]
    public string dataId;
    public DataType dataType;
    public bool requireUnlock = false;

    public bool RequireUnlock => requireUnlock;
    public DataType UnlockType => dataType;
    public string UnlockId => dataId;
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(dataId))
            dataId = name;
    }
#endif
}
