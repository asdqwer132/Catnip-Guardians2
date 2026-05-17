using System;
using UnityEngine;

public enum CursorType
{
    Default,
    Attack
}

[Serializable]
public class CursorSet
{
    public CursorType type;

    [Header("Cursor Texture")]
    public Texture2D defaultCursor;
    public Texture2D clickCursor;

    [Header("Hotspot")]
    public Vector2 hotspot = Vector2.zero;
}

public class CursorChanger : MonoBehaviour
{
    public static CursorChanger instance;

    [Header("Cursor Sets")]
    public CursorSet[] cursorSets;

    private CursorType currentType = CursorType.Default;
    private CursorSet currentSet;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetCursor(CursorType.Default);
    }

    void Update()
    {
        UpdateClickCursor();
    }

    void UpdateClickCursor()
    {
        if (currentSet == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            ApplyCursor(currentSet.clickCursor, currentSet.hotspot);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ApplyCursor(currentSet.defaultCursor, currentSet.hotspot);
        }
    }

    public void SetCursor(CursorType type)
    {
        CursorSet set = GetCursorSet(type);

        if (set == null)
        {
            Debug.LogWarning("커서 타입을 찾을 수 없습니다: " + type);
            return;
        }

        currentType = type;
        currentSet = set;

        ApplyCursor(currentSet.defaultCursor, currentSet.hotspot);
    }

    CursorSet GetCursorSet(CursorType type)
    {
        foreach (CursorSet set in cursorSets)
        {
            if (set.type == type)
                return set;
        }

        return null;
    }

    void ApplyCursor(Texture2D texture, Vector2 hotspot)
    {
        if (texture == null)
            return;

        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    public CursorType GetCurrentCursorType()
    {
        return currentType;
    }
}