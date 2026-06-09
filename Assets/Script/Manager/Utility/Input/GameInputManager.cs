using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager instance;

    [Header("Input Block")]
    [SerializeField] private GameInputBlockType blockType = GameInputBlockType.None;

    [Header("Keys")]
    public Key resetAllCooldownKey = Key.R;

    [Header("Debug")]
    public bool logInput;

    public event Action<Vector2> OnMousePositionChanged;

    public event Action OnUseItemPressed;
    public event Action OnUseItemReleased;

    public event Action OnMovePressed;
    public event Action OnMoveReleased;

    public event Action<int> OnNumberPressed;

    public event Action OnResetAllCooldownPressed;
    public event Action OnInventoryPressed;
    public event Action OnSkillTreePressed;
    public event Action OnCraftPressed;
    public event Action OnShopPressed;
    public event Action OnSettingPressed;
    public event Action OnNextRoundPressed;
    public event Action OnCancelPressed;
    public event Action OnPlayerRangePressed;

    public Vector2 MouseScreenPosition { get; private set; }
    public Vector3 MouseWorldPosition { get; private set; }

    private Camera mainCamera;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;
    }

    private void Update()
    {
        RefreshMousePosition();
        CheckGameplayInput();
        CheckUIInput();
    }

    private void RefreshMousePosition()
    {
        if (Mouse.current == null)
            return;

        MouseScreenPosition = Mouse.current.position.ReadValue();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            MouseWorldPosition = mainCamera.ScreenToWorldPoint(MouseScreenPosition);
            MouseWorldPosition = new Vector3(MouseWorldPosition.x, MouseWorldPosition.y, 0f);
        }

        OnMousePositionChanged?.Invoke(MouseScreenPosition);
    }
    
    private void CheckGameplayInput()
    {
        if (IsBlocked(GameInputBlockType.Gameplay))
            return;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                //Debug.Log("Use Item Pressed");
                OnUseItemPressed?.Invoke();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Log("Use Item Released");
                OnUseItemReleased?.Invoke();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Log("Move Pressed");
                OnMovePressed?.Invoke();
            }

            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                Log("Move Released");
                OnMoveReleased?.Invoke();
            }
        }

        if (Keyboard.current == null)
            return;

        CheckNumberKey(Key.Digit1, 0);
        CheckNumberKey(Key.Digit2, 1);
        CheckNumberKey(Key.Digit3, 2);
        CheckNumberKey(Key.Digit4, 3);
        CheckNumberKey(Key.Digit5, 4);
        CheckNumberKey(Key.Digit6, 5);
        CheckNumberKey(Key.Digit7, 6);
        CheckNumberKey(Key.Digit8, 7);
        CheckNumberKey(Key.Digit9, 8);

        if (Keyboard.current[resetAllCooldownKey].wasPressedThisFrame)
        {
            Log("Reset All Cooldown");
            OnResetAllCooldownPressed?.Invoke();
        }
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Log("Prev Bag");
            OnPlayerRangePressed?.Invoke();
        }
    }

    private void CheckUIInput()
    {
        if (IsBlocked(GameInputBlockType.UI))
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            Log("Inventory");
            OnInventoryPressed?.Invoke();
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Log("Skill Tree");
            OnSkillTreePressed?.Invoke();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Log("Craft");
            OnCraftPressed?.Invoke();
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            Log("Shop");
            OnShopPressed?.Invoke();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Log("Cancel / Setting");
            OnCancelPressed?.Invoke();
            OnSettingPressed?.Invoke();
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Log("Next Round");
            OnNextRoundPressed?.Invoke();
        }
    }

    private void CheckNumberKey(Key key, int index)
    {
        if (Keyboard.current[key].wasPressedThisFrame)
        {
            Log("Number " + (index + 1));
            OnNumberPressed?.Invoke(index);
        }
    }

    public bool IsBlocked(GameInputBlockType targetBlock)
    {
        return (blockType & targetBlock) != 0;
    }

    public void SetBlock(GameInputBlockType targetBlock, bool blocked)
    {
        if (blocked)
            blockType |= targetBlock;
        else
            blockType &= ~targetBlock;
    }

    public void BlockGameplayInput(bool blocked)
    {
        SetBlock(GameInputBlockType.Gameplay, blocked);
    }

    public void BlockUIInput(bool blocked)
    {
        SetBlock(GameInputBlockType.UI, blocked);
    }

    public void BlockAllInput(bool blocked)
    {
        SetBlock(GameInputBlockType.All, blocked);
    }

    public void ClearBlock()
    {
        blockType = GameInputBlockType.None;
    }

    private void Log(string message)
    {
        if (logInput)
            Debug.Log("[GameInputManager] " + message);
    }
}
