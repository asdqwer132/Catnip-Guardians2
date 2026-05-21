using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SetCursor(CursorType.Default);
    }

    private void Update()
    {
        UpdateClickCursor();
    }

    private void UpdateClickCursor()
    {
        if (currentSet == null)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ApplyCursor(currentSet.clickCursor, currentSet.hotspot);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
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

    private CursorSet GetCursorSet(CursorType type)
    {
        if (cursorSets == null)
            return null;

        foreach (CursorSet set in cursorSets)
        {
            if (set == null)
                continue;

            if (set.type == type)
                return set;
        }

        return null;
    }

    private void ApplyCursor(Texture2D texture, Vector2 hotspot)
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