using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CursorType
{
    Default,
    Attack
}

[Serializable]
public struct CursorSet
{
    public CursorType type;

    [Header("Cursor Texture")]
    public Texture2D defaultCursor;
    public Texture2D clickCursor;

    [Header("Hotspot")]
    public Vector2 hotspot;
}

public class CursorChanger : MonoBehaviour
{
    public static CursorChanger instance;

    [Header("Cursor Sets")]
    public CursorSet[] cursorSets;

    [Header("Cursor Scale")]
    [Range(0.5f, 3f)]
    public float cursorScale = 1f;

    private CursorType currentType = CursorType.Default;
    private CursorSet currentSet;
    private bool hasCurrentSet = false;

    private bool isClicking = false;

    private Texture2D cachedDefaultCursor;
    private Texture2D cachedClickCursor;

    private bool cachedDefaultIsRuntimeTexture = false;
    private bool cachedClickIsRuntimeTexture = false;

    private float cachedScale = -1f;
    private CursorType cachedType;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        SetCursor(CursorType.Default);

        instance = this;
    }

    private void Start()
    {
        if (SettingManager.instance != null)
            cursorScale = SettingManager.instance.setting.cursorScale;

    }

    private void Update()
    {
        UpdateClickCursor();
    }

    private void UpdateClickCursor()
    {
        if (!hasCurrentSet)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicking = true;
            ApplyCurrentCursor();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isClicking = false;
            ApplyCurrentCursor();
        }
    }

    public void SetCursor(CursorType type)
    {
        CursorSet set;
        if (!TryGetCursorSet(type, out set))
        {
            Debug.LogWarning("커서 타입을 찾을 수 없습니다: " + type);
            return;
        }

        currentType = type;
        currentSet = set;
        hasCurrentSet = true;
        isClicking = false;

        RefreshScaledTextures();
        ApplyCurrentCursor();
    }

    public void SetCursorScale(float scale)
    {
        cursorScale = Mathf.Clamp(scale, 0.5f, 3f);

        if (!hasCurrentSet)
            return;

        RefreshScaledTextures();
        ApplyCurrentCursor();
    }

    private void RefreshScaledTextures()
    {
        if (!hasCurrentSet)
            return;

        if (Mathf.Approximately(cachedScale, cursorScale) &&
            cachedType == currentType &&
            cachedDefaultCursor != null)
        {
            return;
        }

        ClearRuntimeTextures();

        cachedScale = cursorScale;
        cachedType = currentType;

        cachedDefaultCursor = CreateCursorTexture(
            currentSet.defaultCursor,
            cursorScale,
            out cachedDefaultIsRuntimeTexture
        );

        cachedClickCursor = CreateCursorTexture(
            currentSet.clickCursor,
            cursorScale,
            out cachedClickIsRuntimeTexture
        );
    }

    private Texture2D CreateCursorTexture(Texture2D source, float scale, out bool isRuntimeTexture)
    {
        isRuntimeTexture = false;

        if (source == null)
            return null;

        if (Mathf.Approximately(scale, 1f))
            return source;

        int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, renderTexture);

        RenderTexture.active = renderTexture;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        result.name = source.name + "_RuntimeScaled_" + scale;

        isRuntimeTexture = true;
        return result;
    }

    private void ApplyCurrentCursor()
    {
        if (!hasCurrentSet)
            return;

        Texture2D texture;

        if (isClicking)
        {
            if (cachedClickCursor != null)
                texture = cachedClickCursor;
            else
                texture = cachedDefaultCursor;
        }
        else
        {
            texture = cachedDefaultCursor;
        }

        if (texture == null)
        {
            Debug.LogWarning("적용할 커서 텍스처가 없습니다: " + currentType);
            return;
        }

        Vector2 hotspot = currentSet.hotspot * cursorScale;

        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    private bool TryGetCursorSet(CursorType type, out CursorSet result)
    {
        result = default;

        if (cursorSets == null)
            return false;

        for (int i = 0; i < cursorSets.Length; i++)
        {
            if (cursorSets[i].type == type)
            {
                result = cursorSets[i];
                return true;
            }
        }

        return false;
    }

    private void ClearRuntimeTextures()
    {
        if (cachedDefaultIsRuntimeTexture && cachedDefaultCursor != null)
            Destroy(cachedDefaultCursor);

        if (cachedClickIsRuntimeTexture && cachedClickCursor != null)
            Destroy(cachedClickCursor);

        cachedDefaultCursor = null;
        cachedClickCursor = null;

        cachedDefaultIsRuntimeTexture = false;
        cachedClickIsRuntimeTexture = false;
    }

    public CursorType GetCurrentCursorType()
    {
        return currentType;
    }

    private void OnDestroy()
    {
        ClearRuntimeTextures();

        if (instance == this)
            instance = null;
    }
}